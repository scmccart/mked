namespace Mked.Console;

/// <summary>
/// Pure byte-stream state machine that decodes terminal input into <see cref="InputEvent"/>
/// instances. Handles printable UTF-8, control characters, VT escape sequences for
/// navigation keys, SGR 1006 mouse wheel sequences, and bracketed-paste blocks
/// (<c>ESC [ 2 0 0 ~</c> … <c>ESC [ 2 0 1 ~</c>). The parser is independent of any I/O
/// so it can be exercised in unit tests without a real terminal.
/// </summary>
internal sealed class TerminalInputParser
{
    // Unprocessed bytes from a previous TryParse call (partial escape sequence).
    private readonly List<byte> _pending = new(16);

    // Bracketed-paste accumulation. Non-null while we are between the start marker
    // (ESC [ 2 0 0 ~) and the end marker (ESC [ 2 0 1 ~).
    private List<byte>? _pasteBuffer;

    /// <summary>
    /// Attempts to parse one complete event from <paramref name="incoming"/> plus any bytes
    /// buffered from prior calls. On success, returns <see langword="true"/>, sets
    /// <paramref name="ev"/>, and removes the consumed bytes from <paramref name="incoming"/>.
    /// </summary>
    public bool TryParse(List<byte> incoming, out InputEvent ev)
    {
        // Merge pending remainder with new bytes.
        if (_pending.Count > 0)
        {
            _pending.AddRange(incoming);
            incoming.Clear();
            incoming.AddRange(_pending);
            _pending.Clear();
        }

        if (incoming.Count == 0) { ev = default; return false; }

        // ── Bracketed-paste accumulation ──────────────────────────────────────
        // While inside a paste block, route all bytes to the paste buffer until
        // we recognise the end marker ESC [ 2 0 1 ~.
        if (_pasteBuffer is not null)
            return TryAccumulatePaste(incoming, out ev);

        byte first = incoming[0];

        // ── ESC sequences ─────────────────────────────────────────────────────
        if (first == 0x1b)
        {
            if (incoming.Count == 1)
            {
                // Only ESC so far — might be the start of a multi-byte sequence arriving
                // in the next poll tick. Save it and wait.
                _pending.AddRange(incoming);
                incoming.Clear();
                ev = default;
                return false;
            }

            byte second = incoming[1];

            if (second == (byte)'[') // CSI
                return TryParseCsi(incoming, out ev);

            if (second == (byte)'O') // SS3 (used by some terminals for arrow keys)
                return TryParseSs3(incoming, out ev);

            // Unknown escape — discard the ESC byte and re-parse.
            incoming.RemoveAt(0);
            ev = default;
            return false;
        }

        // ── Plain byte ────────────────────────────────────────────────────────
        incoming.RemoveAt(0);
        ev = InputEvent.OfKey(PlainByteToKey(first));
        return true;
    }

    // Accumulate bytes into the paste buffer until ESC [ 2 0 1 ~ is seen, then emit a Paste event.
    private bool TryAccumulatePaste(List<byte> buf, out InputEvent ev)
    {
        // Search for the end marker: 0x1b 0x5b 0x32 0x30 0x31 0x7e  (ESC [ 2 0 1 ~)
        ReadOnlySpan<byte> endMarker = [0x1b, 0x5b, 0x32, 0x30, 0x31, 0x7e];
        int endIdx = IndexOf(buf, endMarker);

        if (endIdx < 0)
        {
            // End marker not yet received; buffer everything and wait.
            _pasteBuffer!.AddRange(buf);
            buf.Clear();
            ev = default;
            return false;
        }

        // Bytes before the end marker are paste content; the marker itself is consumed.
        _pasteBuffer!.AddRange(buf.Take(endIdx));
        RemoveConsumed(buf, endIdx + endMarker.Length);

        string text = System.Text.Encoding.UTF8.GetString(_pasteBuffer!.ToArray());
        _pasteBuffer = null;

        ev = InputEvent.OfPaste(text);
        return true;
    }

    private static int IndexOf(List<byte> haystack, ReadOnlySpan<byte> needle)
    {
        for (int i = 0; i <= haystack.Count - needle.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { found = false; break; }
            }
            if (found) return i;
        }
        return -1;
    }

    // ─── CSI: ESC [ ... ──────────────────────────────────────────────────────

    private bool TryParseCsi(List<byte> buf, out InputEvent ev)
    {
        // Need at least ESC + '[' + one more byte.
        if (buf.Count < 3) { SavePending(buf); ev = default; return false; }

        byte third = buf[2];

        // ESC [ < n ; col ; row M/m  ←  SGR mouse (1006 protocol)
        if (third == (byte)'<')
            return TryParseSgrMouse(buf, out ev);

        // Try to read a parameter string followed by a final byte.
        // Format: ESC [ [params] finalByte  where finalByte is 0x40..0x7e
        int paramEnd = FindCsiFinalByte(buf, startAt: 2);
        if (paramEnd < 0) { SavePending(buf); ev = default; return false; }

        byte finalByte = buf[paramEnd];
        string paramStr = ByteRangeToString(buf, 2, paramEnd - 2);
        int consumed = paramEnd + 1;

        // ── Bracketed paste: ESC [ 2 0 0 ~  (start) ─────────────────────────
        // Check BEFORE the generic '~' key mapping below.
        if (finalByte == (byte)'~' && paramStr == "200")
        {
            RemoveConsumed(buf, consumed);
            _pasteBuffer = new List<byte>(256);
            // Immediately try to accumulate any bytes already in the buffer.
            return TryAccumulatePaste(buf, out ev);
        }

        ConsoleKeyInfo key = ParseCsiKey(paramStr, finalByte);
        RemoveConsumed(buf, consumed);
        ev = InputEvent.OfKey(key);
        return true;
    }

    // ESC [ < btn ; col ; row M|m
    private bool TryParseSgrMouse(List<byte> buf, out InputEvent ev)
    {
        // Find the terminating 'M' or 'm'
        int term = -1;
        for (int i = 3; i < buf.Count; i++)
        {
            if (buf[i] == (byte)'M' || buf[i] == (byte)'m') { term = i; break; }
        }

        if (term < 0) { SavePending(buf); ev = default; return false; }

        byte terminator = buf[term]; // 'M' = press/motion, 'm' = release
        string inner = ByteRangeToString(buf, 3, term - 3);
        RemoveConsumed(buf, term + 1);

        // Parse "btn;col;row"
        string[] parts = inner.Split(';');
        if (parts.Length < 1 || !int.TryParse(parts[0], out int btn))
        {
            ev = default;
            return false;
        }

        // SGR wheel: btn=64 = wheel up, btn=65 = wheel down.
        // Our convention: WheelDelta -1 = scroll toward start, +1 = toward end.
        if (btn == 64) { ev = InputEvent.OfWheel(-1); return true; }
        if (btn == 65) { ev = InputEvent.OfWheel(+1); return true; }

        // SGR left-button press: btn=0 (left), terminator='M' (press, not release),
        // bit 0x40 = wheel flag (not set), bit 0x20 = motion/drag (not set).
        // col and row are 1-based → subtract 1 for 0-based screen coordinates.
        if (terminator == (byte)'M'
            && (btn & 0x40) == 0   // not a wheel event
            && (btn & 0x20) == 0   // not a motion/drag event
            && (btn & 0x03) == 0   // left button (btn 0 = left, 1 = middle, 2 = right)
            && parts.Length >= 3
            && int.TryParse(parts[1], out int col)
            && int.TryParse(parts[2], out int row))
        {
            ev = InputEvent.OfClick(col - 1, row - 1);
            return true;
        }

        // Release, right/middle click, motion — discard.
        ev = default;
        return false;
    }

    private static ConsoleKeyInfo ParseCsiKey(string param, byte finalByte)
    {
        bool shift = false, alt = false, ctrl = false;

        // Modifier parameter: ESC [ 1 ; <mod> A  or  ESC [ <n> ; <mod> ~
        // Modifier encoding: 1=none,2=shift,3=alt,4=shift+alt,5=ctrl,6=shift+ctrl,7=alt+ctrl,8=all
        if (param.Contains(';'))
        {
            string[] parts = param.Split(';');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int mod))
            {
                mod--; // subtract 1 per VT spec
                shift = (mod & 1) != 0;
                alt   = (mod & 2) != 0;
                ctrl  = (mod & 4) != 0;
            }
            param = parts[0]; // take first segment for key id
        }

        return finalByte switch
        {
            (byte)'A' => MakeKey(ConsoleKey.UpArrow,    '\0', shift, alt, ctrl),
            (byte)'B' => MakeKey(ConsoleKey.DownArrow,  '\0', shift, alt, ctrl),
            (byte)'C' => MakeKey(ConsoleKey.RightArrow, '\0', shift, alt, ctrl),
            (byte)'D' => MakeKey(ConsoleKey.LeftArrow,  '\0', shift, alt, ctrl),
            (byte)'H' => MakeKey(ConsoleKey.Home,       '\0', shift, alt, ctrl),
            (byte)'F' => MakeKey(ConsoleKey.End,        '\0', shift, alt, ctrl),
            (byte)'~' => param switch
            {
                "1" or "7" => MakeKey(ConsoleKey.Home,     '\0', shift, alt, ctrl),
                "4" or "8" => MakeKey(ConsoleKey.End,      '\0', shift, alt, ctrl),
                "5"        => MakeKey(ConsoleKey.PageUp,   '\0', shift, alt, ctrl),
                "6"        => MakeKey(ConsoleKey.PageDown, '\0', shift, alt, ctrl),
                _          => MakeKey(ConsoleKey.None,     '\0', shift, alt, ctrl),
            },
            _ => MakeKey(ConsoleKey.None, '\0', shift, alt, ctrl),
        };
    }

    // ─── SS3: ESC O x ────────────────────────────────────────────────────────

    private static bool TryParseSs3(List<byte> buf, out InputEvent ev)
    {
        if (buf.Count < 3) { ev = default; return false; }
        byte ch = buf[2];
        RemoveConsumed(buf, 3);
        ev = InputEvent.OfKey(ch switch
        {
            (byte)'A' => MakeKey(ConsoleKey.UpArrow,    '\0'),
            (byte)'B' => MakeKey(ConsoleKey.DownArrow,  '\0'),
            (byte)'C' => MakeKey(ConsoleKey.RightArrow, '\0'),
            (byte)'D' => MakeKey(ConsoleKey.LeftArrow,  '\0'),
            (byte)'H' => MakeKey(ConsoleKey.Home,       '\0'),
            (byte)'F' => MakeKey(ConsoleKey.End,        '\0'),
            _         => MakeKey(ConsoleKey.None,       (char)ch),
        });
        return true;
    }

    // ─── Plain byte decoding ──────────────────────────────────────────────────

    private static ConsoleKeyInfo PlainByteToKey(byte b) => b switch
    {
        0x0d or 0x0a => MakeKey(ConsoleKey.Enter,     '\r'),
        0x7f or 0x08 => MakeKey(ConsoleKey.Backspace, '\b'),
        0x09         => MakeKey(ConsoleKey.Tab,        '\t'),
        0x1b         => MakeKey(ConsoleKey.Escape,     '\x1b'),
        _ when b < 0x20 => // Ctrl+letter: byte 1 = Ctrl+A, ..., 26 = Ctrl+Z
            MakeKey((ConsoleKey)(b + 64), (char)(b + 64), false, false, true),
        _ => // printable ASCII / UTF-8 leading byte (treat as char; surrogate handling omitted)
            MakeKey((ConsoleKey)char.ToUpperInvariant((char)b), (char)b),
    };

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static int FindCsiFinalByte(List<byte> buf, int startAt)
    {
        for (int i = startAt; i < buf.Count; i++)
        {
            byte b = buf[i];
            if (b >= 0x40 && b <= 0x7e) return i;
        }
        return -1;
    }

    private static string ByteRangeToString(List<byte> buf, int offset, int length)
    {
        if (length <= 0) return string.Empty;
        char[] chars = new char[length];
        for (int i = 0; i < length; i++) chars[i] = (char)buf[offset + i];
        return new string(chars);
    }

    private static void RemoveConsumed(List<byte> buf, int count) =>
        buf.RemoveRange(0, Math.Min(count, buf.Count));

    private void SavePending(List<byte> buf)
    {
        _pending.AddRange(buf);
        buf.Clear();
    }

    private static ConsoleKeyInfo MakeKey(
        ConsoleKey key, char ch,
        bool shift = false, bool alt = false, bool ctrl = false) =>
        new ConsoleKeyInfo(ch, key, shift, alt, ctrl);
}
