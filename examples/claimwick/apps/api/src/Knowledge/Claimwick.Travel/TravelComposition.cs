using System;
using Microsoft.Extensions.DependencyInjection;

namespace Claimwick.Travel;

/// <summary>Registers the travel knowledge area in a host: the registry with its sets, and the rules reading them.</summary>
public static class TravelComposition
{
    /// <summary>Adds the travel sets the configure step registers, validates them, and serves ITravelRules.</summary>
    public static IServiceCollection AddTravel(this IServiceCollection services, Func<TravelRegistry, TravelRegistry> configure)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var registry = new TravelRegistry();
        configure(registry);
        registry.Validate();
        services.AddSingleton(registry);
        services.AddSingleton<ITravelRules, TravelRules>();
        return services;
    }
}
