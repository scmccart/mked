namespace Mked.Domain.Tests;

public class CursorPosition_Tests
{
    [Fact]
    public void Construction_PreservesLineAndColumn()
    {
        var pos = new CursorPosition(Line: 4, Column: 12);

        pos.Line.Should().Be(4);
        pos.Column.Should().Be(12);
    }

    [Fact]
    public void ValueEquality_HoldsForSameCoordinates()
    {
        new CursorPosition(2, 5).Should().Be(new CursorPosition(2, 5));
    }

    [Fact]
    public void ValueEquality_FailsForDifferentCoordinates()
    {
        new CursorPosition(1, 1).Should().NotBe(new CursorPosition(1, 2));
    }

    [Fact]
    public void Deconstruction_YieldsLineAndColumn()
    {
        var (line, column) = new CursorPosition(7, 3);

        line.Should().Be(7);
        column.Should().Be(3);
    }
}
