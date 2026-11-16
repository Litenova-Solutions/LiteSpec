using Claimwick.Application;
using Claimwick.Spending.ReminderPreferences.Domain;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.ReminderPreferences.UseCases.ChangeReminderPreference.Application;

/// <implements>use-case/spending.change-reminder-preference</implements>
internal sealed class ChangeReminderPreferenceCommandHandler(
    IReminderPreferenceRepository preferences,
    IClock clock) : ICommandHandler<ChangeReminderPreferenceCommand, ChangeReminderPreferenceCommandResult>
{
    public async Task<ChangeReminderPreferenceCommandResult> HandleAsync(
        ChangeReminderPreferenceCommand command,
        CancellationToken cancellationToken)
    {
        var preference = await preferences.GetByManagerAsync(command.ManagerId, cancellationToken);

        if (command.ReceivesReminders)
        {
            if (preference is not null)
            {
                ReminderPreference.EnsureChangerAllowed(preference.ManagerId, command.ChangerId, command.ChangerIsFinance);
                preferences.Delete(preference.ManagerId);
            }
        }
        else if (preference is null)
        {
            preferences.Store(ReminderPreference.Suppress(command.ManagerId, command.ChangerId, command.ChangerIsFinance, command.Reason, clock.UtcNow));
        }
        else
        {
            preference.Change(command.ChangerId, command.ChangerIsFinance, command.Reason, clock.UtcNow);
            preferences.Store(preference);
        }

        return new ChangeReminderPreferenceCommandResult(command.ManagerId, command.ReceivesReminders);
    }
}
