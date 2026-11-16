namespace Claimwick.Spending.ReminderPreferences.UseCases.ChangeReminderPreference.Application;

public sealed record ChangeReminderPreferenceCommandResult(Guid ManagerId, bool ReceivesReminders);
