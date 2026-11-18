using System.Security.Claims;
using Claimwick.Platform.Http;
using Claimwick.Spending.UseCases.ChangeReminderPreference.Application;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.UseCases.ChangeReminderPreference.WebApi;

/// <implements>use-case/spending.change-reminder-preference</implements>
internal sealed class ChangeReminderPreferenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/reminder-preferences/{managerId}", HandleAsync)
            .WithName("ChangeReminderPreference")
            .WithSummary("Change a manager's reminder preference.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<NoContent> HandleAsync(
        Guid managerId,
        bool receivesReminders,
        string? reason,
        ClaimsPrincipal user,
        ICommandMediator mediator,
        CancellationToken cancellationToken)
    {
        // The changer is the signed-in caller, read from verified claims, never from the request body.
        var changerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var changerIsFinance = user.IsInRole("finance");
        await mediator.SendAsync(new ChangeReminderPreferenceCommand(managerId, changerId, changerIsFinance, receivesReminders, reason, DateTimeOffset.UtcNow), cancellationToken);

        return TypedResults.NoContent();
    }
}
