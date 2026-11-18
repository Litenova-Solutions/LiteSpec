using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.ReminderPreferences.ChangeReminderPreference;

/// <summary>Declares the change-reminder-preference command idempotent through the preference state, which keeps the same choice in place.</summary>
internal sealed class ChangeReminderPreferenceCommandIdempotencyDefinition : IIdempotencyDefinition<ChangeReminderPreferenceCommand>
{
    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(
        "A repeat sets the same choice for the same manager, leaving the same preference in place.");
}
