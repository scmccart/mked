namespace Mked.Domain.Tests;

public class ViewerState_SetAnchor_Tests
{
    [Fact]
    public void ValidIndex_UpdatesAnchor()
    {
        var doc = MarkdownDocument.Parse("# Title\n\nParagraph.");
        var state = new ViewerState(doc);

        state.SetAnchor(new ViewportAnchor(1));

        state.Anchor.Should().Be(new ViewportAnchor(1));
    }

    [Fact]
    public void NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);

        Action act = () => state.SetAnchor(new ViewportAnchor(-1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IndexEqualToBlockCount_ThrowsArgumentOutOfRangeException()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);
        int outOfRange = doc.Blocks.Count;

        Action act = () => state.SetAnchor(new ViewportAnchor(outOfRange));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FirstBlock_IsAlwaysValid()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);

        Action act = () => state.SetAnchor(new ViewportAnchor(0));

        act.Should().NotThrow();
    }
}
