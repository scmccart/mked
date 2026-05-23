namespace Mked.Domain.Tests;

public class MkedError_PatternMatch_Tests
{
    [Theory]
    [InlineData("io")]
    [InlineData("parse")]
    [InlineData("validation")]
    [InlineData("stream")]
    public void SwitchExpression_MatchesAllCasesExhaustively(string kind)
    {
        MkedError error = kind switch
        {
            "io"         => new MkedError.IoError("/f", "r"),
            "parse"      => new MkedError.ParseError(1, 1, "m"),
            "validation" => new MkedError.ValidationError("f", "m"),
            _            => new MkedError.StreamError("r"),
        };

        var matched = error switch
        {
            MkedError.IoError(var path, _)          => $"io:{path}",
            MkedError.ParseError(var l, var c, _)   => $"parse:{l},{c}",
            MkedError.ValidationError(var f, _)     => $"validation:{f}",
            MkedError.StreamError(var reason)       => $"stream:{reason}",
            _                                       => throw new System.Diagnostics.UnreachableException(),
        };

        matched.Should().NotBeNullOrEmpty();
    }
}
