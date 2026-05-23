namespace Mked.Domain.Tests;

public class ViewportAnchor_Tests
{
    [Fact]
    public void Construction_PreservesBlockIndex()
    {
        var anchor = new ViewportAnchor(3);

        anchor.BlockIndex.Should().Be(3);
    }

    [Fact]
    public void ValueEquality_HoldsForSameIndex()
    {
        new ViewportAnchor(0).Should().Be(new ViewportAnchor(0));
    }

    [Fact]
    public void ValueEquality_FailsForDifferentIndex()
    {
        new ViewportAnchor(1).Should().NotBe(new ViewportAnchor(2));
    }

    [Fact]
    public void Deconstruct_YieldsBlockIndex()
    {
        new ViewportAnchor(5).Deconstruct(out var blockIndex);

        blockIndex.Should().Be(5);
    }
}
