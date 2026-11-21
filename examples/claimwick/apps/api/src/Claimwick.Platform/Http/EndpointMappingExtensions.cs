using Claimwick.Platform.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Claimwick.Platform.Http;

/// <summary>Maps the endpoints the platform discovers in the modules of one host.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-composition-in-hosts</follows>
public static class EndpointMappingExtensions
{
    /// <summary>Maps every endpoint of every added module once, in type-name order.</summary>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpointTypes = app.Services.GetServices<IModule>()
            .Select(module => module.GetType().Assembly)
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEndpoint).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var endpointType in endpointTypes)
        {
            var endpoint = (IEndpoint)ActivatorUtilities.CreateInstance(app.Services, endpointType);
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
