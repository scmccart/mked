using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

namespace Mked.Application.Tests.Architecture;

public sealed class ApplicationLayer_DependencyRules_Tests
{
    private static readonly ArchUnitNET.Domain.Architecture Arch = new ArchLoader()
        .LoadAssemblies(typeof(OpenFileUseCase).Assembly)
        .Build();

    [Fact]
    public void Application_TypesDoNotReferenceInfrastructure()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Infrastructure.*")
            .Check(Arch);
    }

    [Fact]
    public void Application_TypesDoNotReferenceConsole()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Console.*")
            .Check(Arch);
    }

    [Fact]
    public void Application_TypesDoNotReferenceSpectreConsole()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Spectre\\.Console.*")
            .Check(Arch);
    }

    [Fact]
    public void Application_TypesDoNotReferenceSystemIo()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("System\\.IO.*")
            .Check(Arch);
    }

    [Fact]
    public void Application_ReferencesOnlyDomainAmongMkedAssemblies()
    {
        var assembly = typeof(OpenFileUseCase).Assembly;
        var referencedNames = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referencedNames.Should().NotContain(name =>
            name.StartsWith("Mked.", StringComparison.Ordinal) &&
            !name.StartsWith("Mked.Domain", StringComparison.Ordinal),
            "Mked.Application must reference only Mked.Domain among Mked.* assemblies");
    }

    [Fact]
    public void Application_ReferencesDomain()
    {
        var assembly = typeof(OpenFileUseCase).Assembly;
        var referencedNames = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referencedNames.Should().Contain(
            name => name.StartsWith("Mked.Domain", StringComparison.Ordinal),
            "Mked.Application must reference Mked.Domain");
    }
}
