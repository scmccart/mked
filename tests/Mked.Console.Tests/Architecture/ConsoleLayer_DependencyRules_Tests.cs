using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

namespace Mked.Console.Tests.Architecture;

public sealed class ConsoleLayer_DependencyRules_Tests
{
    private static readonly ArchUnitNET.Domain.Architecture Arch = new ArchLoader()
        .LoadAssemblies(
            typeof(ViewCommand).Assembly,
            typeof(Mked.Infrastructure.FileSystemReader).Assembly,
            typeof(Mked.Application.OpenFileUseCase).Assembly,
            typeof(Mked.Domain.MkedError).Assembly)
        .Build();

    [Fact]
    public void Domain_DoesNotReferenceConsole()
    {
        ArchRuleDefinition
            .Types().That().ResideInNamespaceMatching("Mked\\.Domain.*")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Console.*")
            .Check(Arch);
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        ArchRuleDefinition
            .Types().That().ResideInNamespaceMatching("Mked\\.Application.*")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Infrastructure.*")
            .Check(Arch);
    }

    [Fact]
    public void Console_IsOnlyLayerAllowedToReferenceInfrastructure()
    {
        // Application and Domain must not reference Infrastructure.
        var assembly = typeof(Mked.Application.OpenFileUseCase).Assembly;
        var referenced = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referenced.Should().NotContain(
            name => name.StartsWith("Mked.Infrastructure", StringComparison.Ordinal),
            "only Mked.Console (the composition root) may reference Mked.Infrastructure");
    }
}
