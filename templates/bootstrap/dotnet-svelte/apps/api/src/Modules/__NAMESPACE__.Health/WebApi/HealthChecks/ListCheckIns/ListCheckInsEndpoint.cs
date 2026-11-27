using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.WebApi.HealthChecks.ListCheckIns;

/// <summary>
/// The list-check-ins endpoint: lists the stored check-ins, newest first,
/// and answers 200 with them.
/// </summary>
/// <implements>use-case/health.list-check-ins</implements>
internal static class ListCheckInsEndpoint
{
    /// <summary>Maps the list-check-ins endpoint on the health group.</summary>
    /// <param name="group">The health route group.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/check-ins", GetAsync)
            .WithName("list-check-ins")
            .WithSummary("List check-ins")
            .Produces<ListCheckInsResponse>(StatusCodes.Status200OK);
    }

    /// <summary>Lists the stored check-ins.</summary>
    /// <param name="queries">The query mediator.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 with the listed check-ins.</returns>
    private static async Task<IResult> GetAsync(
        IQueryMediator queries,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queries);
        var result = await queries.QueryAsync(new ListCheckInsQuery(), cancellationToken).ConfigureAwait(false);
        return Results.Ok(new ListCheckInsResponse(
            result.CheckIns.Select(row => new CheckInRowResponse(row.CheckInId, row.RecordedAt, row.Note, row.State)).ToList()));
    }
}
