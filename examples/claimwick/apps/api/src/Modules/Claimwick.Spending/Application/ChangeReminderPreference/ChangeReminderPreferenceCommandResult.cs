namespace Claimwick.Spending.Application.ChangeReminderPreference;

internal sealed record ChangeReminderPreferenceCommandResult(Guid ManagerId, bool ReceivesReminders);
