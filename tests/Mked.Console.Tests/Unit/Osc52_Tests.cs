using System.Text;

namespace Mked.Console.Tests.Unit;

public class Osc52_Tests
{
    [Fact]
    public void EncodeCopy_ProducesCorrectEscape()
    {
        string result = Osc52.EncodeCopy("hello");

        string expected = "\x1b]52;c;" + Convert.ToBase64String(Encoding.UTF8.GetBytes("hello")) + "\x07";
        result.Should().Be(expected);
    }

    [Fact]
    public void EncodeCopy_StartsWithOscIntroducer()
    {
        string result = Osc52.EncodeCopy("test");

        result.Should().StartWith("\x1b]52;c;");
    }

    [Fact]
    public void EncodeCopy_EndsWithBellTerminator()
    {
        string result = Osc52.EncodeCopy("test");

        result.Should().EndWith("\x07");
    }

    [Fact]
    public void EncodeCopy_EmptyString_ProducesValidSequence()
    {
        string result = Osc52.EncodeCopy(string.Empty);

        result.Should().Be("\x1b]52;c;\x07"); // base64 of empty = ""
    }

    [Fact]
    public void EncodeCopy_UnicodeText_Utf8Encoded()
    {
        const string text = "héllo wörld";
        string result = Osc52.EncodeCopy(text);

        // Verify the base64 payload decodes back to the original string
        string payload = result["\x1b]52;c;".Length..^1]; // strip OSC intro + BEL
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        decoded.Should().Be(text);
    }

    [Fact]
    public void EncodeCopy_MultilineText_EncodedCorrectly()
    {
        const string text = "line1\nline2";
        string result = Osc52.EncodeCopy(text);

        string payload = result["\x1b]52;c;".Length..^1];
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        decoded.Should().Be(text);
    }
}
