using ActDim.Practix.Abstractions.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ActDim.Practix.Json;

/// <summary>
/// Microsoft.Extensions.DependencyInjection registration helpers for the JSON serializer.
/// </summary>
public static class JsonSerializerServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IJsonSerializer"/> as a singleton using the default
    /// <see cref="CoreJsonSerializer"/> implementation.
    /// Uses TryAdd so a caller-supplied implementation takes precedence.
    /// </summary>
    public static IServiceCollection AddJsonSerializer(this IServiceCollection services)
    {
        services.TryAddSingleton<IJsonSerializer, CoreJsonSerializer>();
        return services;
    }
}
