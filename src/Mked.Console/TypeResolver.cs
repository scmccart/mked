using Microsoft.Extensions.DependencyInjection;

namespace Mked.Console;

/// <summary>Resolves types from the built <see cref="IServiceProvider"/>.</summary>
internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    /// <inheritdoc/>
    public object? Resolve(Type? type) =>
        type is null ? null : provider.GetService(type);

    /// <inheritdoc/>
    public void Dispose() => (provider as IDisposable)?.Dispose();
}
