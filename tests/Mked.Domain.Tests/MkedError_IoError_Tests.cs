namespace Mked.Domain.Tests;

public class MkedError_IoError_Tests
{
    [Fact]
    public void Construction_ExposesPathAndReason()
    {
        var error = new MkedError.IoError("/tmp/file.md", "not found");

        error.Path.Should().Be("/tmp/file.md");
        error.Reason.Should().Be("not found");
    }

    [Fact]
    public void StructuralEquality_HoldsForSameValues()
    {
        var a = new MkedError.IoError("/a", "x");
        var b = new MkedError.IoError("/a", "x");

        a.Should().Be(b);
    }
}
