namespace Mked.Domain.Tests;

public class MkedError_ParseError_Tests
{
    [Fact]
    public void Construction_ExposesLineColumnMessage()
    {
        var error = new MkedError.ParseError(3, 7, "unexpected token");

        error.Line.Should().Be(3);
        error.Column.Should().Be(7);
        error.Message.Should().Be("unexpected token");
    }
}
