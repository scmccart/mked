using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

namespace Mked.Controls.Tests.DependencyRules;

public sealed class ControlsLayer_DependencyRules_Tests
{
    private static readonly Architecture Arch = new ArchLoader()
        .LoadAssemblies(typeof(MarkdownViewer).Assembly)
        .Build();

    [Fact]
    public void Controls_DoesNotReferenceApplication()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Application.*")
            .Check(Arch);
    }

    [Fact]
    public void Controls_DoesNotReferenceDomain()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Domain.*")
            .Check(Arch);
    }

    [Fact]
    public void Controls_DoesNotReferenceInfrastructure()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Infrastructure.*")
            .Check(Arch);
    }

    [Fact]
    public void Controls_DoesNotReferenceConsole()
    {
        ArchRuleDefinition
            .Types()
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching("Mked\\.Console.*")
            .Check(Arch);
    }

    [Fact]
    public void Controls_ReferencesSpectreConsole()
    {
        var assembly = typeof(MarkdownViewer).Assembly;
        var referenced = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referenced.Should().Contain(name =>
            name.StartsWith("Spectre.Console", StringComparison.Ordinal),
            "Mked.Controls must reference Spectre.Console");
    }

    [Fact]
    public void Controls_ReferencesMarkdig()
    {
        var assembly = typeof(MarkdownViewer).Assembly;
        var referenced = assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        referenced.Should().Contain(name =>
            name.StartsWith("Markdig", StringComparison.Ordinal),
            "Mked.Controls must reference Markdig");
    }
}
