using Claimwick.Spending.Domain.ReminderPreference;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.ChangeReminderPreference;

/// <implements>use-case/spending.change-reminder-preference</implements>
internal sealed class ChangeReminderPreferenceCommandHandler : ICommandHandler<ChangeReminderPreferenceCommand, ChangeReminderPreferenceCommandResult>
{
    private readonly IReminderPreferenceRepository _preferences;

    public ChangeReminderPreferenceCommandHandler(IReminderPreferenceRepository preferences)
    {
        _preferences = preferences;
    }

    public async Task<ChangeReminderPreferenceCommandResult> HandleAsync(
        ChangeReminderPreferenceCommand command,
        CancellationToken cancellationToken)
    {
        var preference = await _preferences.GetByManagerAsync(command.ManagerId, cancellationToken);

        if (command.ReceivesReminders)
        {
            if (preference is not null)
            {
                ReminderPreference.EnsureChangerAllowed(preference.ManagerId, command.ChangerId, command.ChangerIsFinance);
                _preferences.Delete(preference.ManagerId);
            }
        }
        else if (preference is null)
        {
            _preferences.Store(ReminderPreference.Suppress(command.ManagerId, command.ChangerId, command.ChangerIsFinance, command.Reason, command.HandledAt));
        }
        else
        {
            preference.Change(command.ChangerId, command.ChangerIsFinance, command.Reason, command.HandledAt);
            _preferences.Store(preference);
        }

        return new ChangeReminderPreferenceCommandResult(command.ManagerId, command.ReceivesReminders);
    }
}
