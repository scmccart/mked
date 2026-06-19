namespace Mked.Domain.Tests;

public class MkedError_PatternMatch_Tests
{
    [Theory]
    [InlineData("io",         "io:/f")]
    [InlineData("parse",      "parse:1,1")]
    [InlineData("validation", "validation:f")]
    [InlineData("stream",     "stream:r")]
    public void SwitchExpression_MatchesAllCasesExhaustively(string kind, string expected)
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
            MkedError.IoError(var path, _, _)       => $"io:{path}",
            MkedError.ParseError(var l, var c, _)   => $"parse:{l},{c}",
            MkedError.ValidationError(var f, _)     => $"validation:{f}",
            MkedError.StreamError(var reason)       => $"stream:{reason}",
            _                                       => throw new System.Diagnostics.UnreachableException(),
        };

        matched.Should().Be(expected);
    }
}
