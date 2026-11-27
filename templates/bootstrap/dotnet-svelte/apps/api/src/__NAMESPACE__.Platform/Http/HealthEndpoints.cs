using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Npgsql;

namespace __NAMESPACE__.Platform.Http;

/// <summary>
/// The liveness and readiness endpoints. Liveness answers the process state;
/// readiness answers the database state and carries the bound stand-in adapters,
/// empty when the product binds none.
/// </summary>
/// <follows>litespec/packs/dotnet/architecture#keep-technical-capabilities-in-the-platform</follows>
public static class HealthEndpoints
{
    /// <summary>Maps liveness and readiness.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static void MapSampleHealth(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
            .WithName("health-live")
            .WithSummary("Liveness")
            .ExcludeFromDescription()
            .AllowAnonymous();
        endpoints.MapGet("/health/ready", ReadyAsync)
            .WithName("health-ready")
            .WithSummary("Readiness")
            .ExcludeFromDescription()
            .AllowAnonymous();
    }

    /// <summary>Answers 200 when the database answers, else 503.</summary>
    /// <param name="dataSource">The shared data source.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The readiness answer.</returns>
    private static async Task<IResult> ReadyAsync(
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Results.Json(new { status = "ready", stubAdapters = Array.Empty<string>() });
        }
        catch (Exception)
        {
            return Results.Json(
                new { status = "not-ready", stubAdapters = Array.Empty<string>() },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
