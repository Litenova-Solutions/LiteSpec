using Microsoft.AspNetCore.Routing;

namespace Claimwick.Platform.Http;

/// <summary>One HTTP operation: maps its route on the route builder.</summary>
public interface IEndpoint
{
    /// <summary>Maps the one route operation this endpoint serves.</summary>
    void MapEndpoint(IEndpointRouteBuilder endpoints);
}
