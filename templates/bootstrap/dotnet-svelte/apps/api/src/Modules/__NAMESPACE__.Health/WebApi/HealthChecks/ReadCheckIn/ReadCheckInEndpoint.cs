using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using __NAMESPACE__.Health.Application.HealthChecks.ReadCheckIn;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.WebApi.HealthChecks.ReadCheckIn;

/// <summary>
/// The read-check-in endpoint: reads one check-in by its identifier and answers
/// 200 with it. A check-in the caller named answers 404 with its refusal code.
/// </summary>
/// <implements>use-case/health.read-check-in</implements>
internal static class ReadCheckInEndpoint
{
    /// <summary>Maps the read-check-in endpoint on the health group.</summary>
    /// <param name="group">The health route group.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/check-ins/{checkInId}", GetAsync)
            .WithName("read-check-in")
            .WithSummary("Read a check-in")
            .Produces<ReadCheckInResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    /// <summary>Reads the named check-in.</summary>
    /// <param name="checkInId">The check-in identifier.</param>
    /// <param name="queries">The query mediator.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 with the stored check-in, or 404 when the check-in is unknown.</returns>
    private static async Task<IResult> GetAsync(
        CheckInId checkInId,
        IQueryMediator queries,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queries);
        var result = await queries.QueryAsync(new ReadCheckInQuery(checkInId), cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            var extensions = new Dictionary<string, object?>();
            extensions["code"] = "health.check-in-unknown";
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "The check-in is unknown.",
                extensions: extensions);
        }

        return Results.Ok(new ReadCheckInResponse(result.CheckInId, result.RecordedAt, result.Note, result.State));
    }
}
