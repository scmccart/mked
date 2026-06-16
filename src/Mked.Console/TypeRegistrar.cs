using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Mked.Console;

/// <summary>Bridges <see cref="IServiceCollection"/> with Spectre.Console.Cli's DI hook.</summary>
internal sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    /// <inheritdoc/>
    public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "Spectre.Console.Cli's ITypeRegistrar does not annotate its implementation parameter; " +
                        "callers are responsible for ensuring the type has public constructors.")]
    public void Register(Type service, Type implementation) =>
        services.AddSingleton(service, implementation);

    /// <inheritdoc/>
    public void RegisterInstance(Type service, object implementation) =>
        services.AddSingleton(service, implementation);

    /// <inheritdoc/>
    public void RegisterLazy(Type service, Func<object> factory) =>
        services.AddSingleton(service, _ => factory());
}
