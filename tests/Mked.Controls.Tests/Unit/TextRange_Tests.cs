namespace Mked.Controls.Tests;

public class TextRange_Tests
{
    [Fact]
    public void Construction_PreservesStartAndEnd()
    {
        var start = new CursorPosition(1, 1);
        var end   = new CursorPosition(1, 10);
        var range = new TextRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void ValueEquality_HoldsForSameStartAndEnd()
    {
        var a = new TextRange(new CursorPosition(1, 1), new CursorPosition(2, 5));
        var b = new TextRange(new CursorPosition(1, 1), new CursorPosition(2, 5));

        a.Should().Be(b);
    }

    [Fact]
    public void ValueEquality_FailsWhenEndDiffers()
    {
        var a = new TextRange(new CursorPosition(1, 1), new CursorPosition(2, 5));
        var b = new TextRange(new CursorPosition(1, 1), new CursorPosition(2, 6));

        a.Should().NotBe(b);
    }
}
