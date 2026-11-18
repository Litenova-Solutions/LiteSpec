using Claimwick.Platform.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Claimwick.Platform.Modules;

/// <summary>Composes the platform and the modules of one host.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-composition-in-hosts</follows>
public static class CompositionExtensions
{
    /// <summary>Adds the platform capabilities: persistence, messaging, access, auditing, HTTP and country packs.</summary>
    public static IServiceCollection AddPlatform(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(json => json.SerializerOptions.Converters.Add(new MoneyJsonConverter()));
        return services;
    }

    /// <summary>Adds the modules that run in this host, in the order given.</summary>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration, params IModule[] modules)
    {
        foreach (var module in modules)
        {
            services.AddSingleton(module);
            module.AddServices(services, configuration);
        }

        return services;
    }
}
