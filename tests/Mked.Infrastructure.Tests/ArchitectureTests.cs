using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

namespace Mked.Infrastructure.Tests;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(FileSystemReader).Assembly)
        .Build();

    [Fact]
    public void Infrastructure_TypesDoNotReferenceApplication()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Application.*")
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_TypesDoNotReferenceConsole()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Console.*")
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_ReferencesOnlyDomainAndFramework()
    {
        var infraAssembly = typeof(FileSystemReader).Assembly;
        var referencedNames = infraAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referencedNames.Should().NotContain(name =>
            name.StartsWith("Mked.", StringComparison.Ordinal) &&
            !name.StartsWith("Mked.Domain", StringComparison.Ordinal),
            "Mked.Infrastructure must reference only Mked.Domain among Mked.* assemblies");
    }

    [Fact]
    public void Infrastructure_ReferencesDomain()
    {
        var infraAssembly = typeof(FileSystemReader).Assembly;
        var referencedNames = infraAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referencedNames.Should().Contain(
            name => name.StartsWith("Mked.Domain", StringComparison.Ordinal),
            "Mked.Infrastructure must reference Mked.Domain");
    }
}
