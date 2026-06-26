namespace Mked.Console.Tests.Unit;

public class TerminalInputParser_BracketedPaste_Tests
{
    private static List<byte> Seq(string s) => new(System.Text.Encoding.Latin1.GetBytes(s));
    private static List<byte> SeqUtf8(string s) => new(System.Text.Encoding.UTF8.GetBytes(s));

    private const string PasteStart = "\x1b[200~";
    private const string PasteEnd   = "\x1b[201~";

    // ─── Happy-path: full bracketed-paste sequence ────────────────────────────

    [Fact]
    public void FullPasteSequence_EmitsPasteEvent()
    {
        var parser = new TerminalInputParser();
        var buf = Seq(PasteStart + "hello" + PasteEnd);

        bool ok = parser.TryParse(buf, out var ev);

        ok.Should().BeTrue();
        ev.Kind.Should().Be(InputEventKind.Paste);
        ev.PasteText.Should().Be("hello");
    }

    [Fact]
    public void PasteText_IsEmpty_WhenNoBytesBeforeEndMarker()
    {
        var parser = new TerminalInputParser();
        var buf = Seq(PasteStart + PasteEnd);

        bool ok = parser.TryParse(buf, out var ev);

        ok.Should().BeTrue();
        ev.Kind.Should().Be(InputEventKind.Paste);
        ev.PasteText.Should().BeEmpty();
    }

    [Fact]
    public void PasteText_WithNewlines_PreservesNewlines()
    {
        var parser = new TerminalInputParser();
        var buf = Seq(PasteStart + "line1\nline2\nline3" + PasteEnd);

        bool ok = parser.TryParse(buf, out var ev);

        ok.Should().BeTrue();
        ev.PasteText.Should().Be("line1\nline2\nline3");
    }

    [Fact]
    public void PasteText_ContainingCsiLikeBytes_PreservedVerbatim()
    {
        // Paste content that looks like an escape sequence must not be interpreted
        var parser = new TerminalInputParser();
        var buf = Seq(PasteStart + "a\x1b[Db" + PasteEnd); // ESC [ D = LeftArrow in content

        bool ok = parser.TryParse(buf, out var ev);

        ok.Should().BeTrue();
        ev.PasteText.Should().Be("a\x1b[Db");
    }

    // ─── Split-read accumulation ──────────────────────────────────────────────

    [Fact]
    public void SplitBeforeEndMarker_AccumulatesAndEmitsOnSecondCall()
    {
        var parser = new TerminalInputParser();

        // First read: start marker + partial content (no end marker)
        var firstBuf = Seq(PasteStart + "hel");
        bool firstOk = parser.TryParse(firstBuf, out _);

        // Second read: rest of content + end marker
        var secondBuf = Seq("lo" + PasteEnd);
        bool secondOk = parser.TryParse(secondBuf, out var ev);

        firstOk.Should().BeFalse("end marker not yet received");
        secondOk.Should().BeTrue();
        ev.Kind.Should().Be(InputEventKind.Paste);
        ev.PasteText.Should().Be("hello");
    }

    [Fact]
    public void SplitAtStartMarker_WaitsAndThenParsesOnNextCall()
    {
        var parser = new TerminalInputParser();

        // First read: only the start marker
        var firstBuf = Seq(PasteStart);
        bool firstOk = parser.TryParse(firstBuf, out _);

        // Second read: content + end marker
        var secondBuf = Seq("world" + PasteEnd);
        bool secondOk = parser.TryParse(secondBuf, out var ev);

        firstOk.Should().BeFalse();
        secondOk.Should().BeTrue();
        ev.PasteText.Should().Be("world");
    }

    // ─── UTF-8 payload ────────────────────────────────────────────────────────

    [Fact]
    public void PasteText_Utf8Content_DecodedCorrectly()
    {
        var parser = new TerminalInputParser();
        // Build the buffer as raw bytes: start marker (Latin-1), UTF-8 payload, end marker
        var buf = new List<byte>();
        buf.AddRange(System.Text.Encoding.Latin1.GetBytes(PasteStart));
        buf.AddRange(System.Text.Encoding.UTF8.GetBytes("héllo"));
        buf.AddRange(System.Text.Encoding.Latin1.GetBytes(PasteEnd));

        bool ok = parser.TryParse(buf, out var ev);

        ok.Should().BeTrue();
        ev.PasteText.Should().Be("héllo");
    }

    // ─── Normal keys after paste ──────────────────────────────────────────────

    [Fact]
    public void AfterPaste_NormalKeysParseCorrectly()
    {
        var parser = new TerminalInputParser();
        var buf = Seq(PasteStart + "hi" + PasteEnd + "a");

        // First parse: the paste
        bool pasteOk = parser.TryParse(buf, out var pasteEv);
        pasteOk.Should().BeTrue();
        pasteEv.Kind.Should().Be(InputEventKind.Paste);

        // Second parse (remaining 'a' still in buf from the start marker + leftover)
        // The buf after paste should contain the 'a' keystroke
        bool keyOk = parser.TryParse(buf, out var keyEv);
        keyOk.Should().BeTrue();
        keyEv.Kind.Should().Be(InputEventKind.Key);
        keyEv.Key.KeyChar.Should().Be('a');
    }
}
