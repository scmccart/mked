namespace Mked.Domain.Tests;

public class MkedError_ValidationError_Tests
{
    [Fact]
    public void Construction_ExposesFieldAndMessage()
    {
        var error = new MkedError.ValidationError("Title", "must not be empty");

        error.Field.Should().Be("Title");
        error.Message.Should().Be("must not be empty");
    }
}
