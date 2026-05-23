namespace Mked.Domain.Tests;

public class Unit_Tests
{
    [Fact]
    public void Value_EqualsAnotherUnitInstance()
    {
        Unit.Value.Should().Be(new Unit());
    }

    [Fact]
    public void CanBeUsedAsResultSuccessType()
    {
        var result = Result.Ok<Unit, string>(Unit.Value);

        result.IsOk.Should().BeTrue();
    }
}
