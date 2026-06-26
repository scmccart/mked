namespace Mked.Domain.Tests;

public class ViewerState_Construction_Tests
{
    [Fact]
    public void Anchor_DefaultsToBlockZero()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);

        state.Anchor.Should().Be(new ViewportAnchor(0));
    }

    [Fact]
    public void Anchor_IsNone_ForEmptyDocument()
    {
        var doc = MarkdownDocument.Parse(string.Empty);
        var state = new ViewerState(doc);

        state.Anchor.Should().Be(ViewportAnchor.None);
        state.Anchor.IsNone.Should().BeTrue();
    }

    [Fact]
    public void IsFollowing_FalseByDefault()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);

        state.IsFollowing.Should().BeFalse();
    }

    [Fact]
    public void NullDocument_ThrowsArgumentNullException()
    {
        Action act = () => _ = new ViewerState(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
