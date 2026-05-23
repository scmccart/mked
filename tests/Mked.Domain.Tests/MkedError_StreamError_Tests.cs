namespace Mked.Domain.Tests;

public class MkedError_StreamError_Tests
{
    [Fact]
    public void Construction_ExposesReason()
    {
        var error = new MkedError.StreamError("broken pipe");

        error.Reason.Should().Be("broken pipe");
    }
}
