using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

namespace Mked.Domain.Tests;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(Result).Assembly)
        .Build();

    [Fact]
    public void Domain_TypesDoNotDependOnSystemIo()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("System\\.IO.*")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_TypesDoNotDependOnSystemConsole()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("System\\.Console.*")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_DoesNotReferenceUpperLayers()
    {
        var domainAssembly = typeof(Result).Assembly;
        var referencedNames = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referencedNames.Should().NotContain(name =>
            name.StartsWith("Mked.Application", StringComparison.Ordinal) ||
            name.StartsWith("Mked.Infrastructure", StringComparison.Ordinal) ||
            name.StartsWith("Mked.Console", StringComparison.Ordinal),
            "Mked.Domain must not reference Application, Infrastructure, or Console assemblies");
    }
}
