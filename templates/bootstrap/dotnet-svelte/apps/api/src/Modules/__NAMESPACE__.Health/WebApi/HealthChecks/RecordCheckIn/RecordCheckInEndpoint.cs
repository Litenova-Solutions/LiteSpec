using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.WebApi.HealthChecks.RecordCheckIn;

/// <summary>
/// The record-check-in endpoint: posts a check-in and answers 201 with its
/// identifier and state. A refusal answers through the problem catalog:
/// invalid input as 400, a caller with no sign-in as 401.
/// </summary>
/// <implements>use-case/health.record-check-in</implements>
internal static class RecordCheckInEndpoint
{
    /// <summary>Maps the record-check-in endpoint on the health group.</summary>
    /// <param name="group">The health route group.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/check-ins", PostAsync)
            .WithName("record-check-in")
            .WithSummary("Record a check-in")
            .Produces<RecordCheckInResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    /// <summary>Records the posted check-in.</summary>
    /// <param name="request">The posted check-in.</param>
    /// <param name="commands">The command mediator.</param>
    /// <param name="clocks">The clock the endpoint stamps the record with.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>201 with the recorded check-in.</returns>
    private static async Task<IResult> PostAsync(
        RecordCheckInRequest request,
        ICommandMediator commands,
        TimeProvider clocks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(clocks);
        var result = await commands.SendAsync(
            new RecordCheckInCommand(CheckInId.From(Guid.CreateVersion7()), clocks.GetUtcNow().UtcDateTime, request.Note),
            cancellationToken).ConfigureAwait(false);
        return Results.Created(
            $"/api/health/check-ins/{result.CheckInId}",
            new RecordCheckInResponse(result.CheckInId, result.State));
    }
}
