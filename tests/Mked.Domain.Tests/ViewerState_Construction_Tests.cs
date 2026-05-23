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
    public void IsFollowing_FalseByDefault()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);

        state.IsFollowing.Should().BeFalse();
    }
}
