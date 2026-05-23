namespace Mked.Domain.Tests;

public class ViewerState_FollowMode_Tests
{
    [Fact]
    public void SetFollowMode_True_IsFollowingBecomesTrue()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);

        state.SetFollowMode(true);

        state.IsFollowing.Should().BeTrue();
    }

    [Fact]
    public void SetFollowMode_False_IsFollowingBecomesFalse()
    {
        var doc = MarkdownDocument.Parse("# Heading");
        var state = new ViewerState(doc);
        state.SetFollowMode(true);

        state.SetFollowMode(false);

        state.IsFollowing.Should().BeFalse();
    }
}
