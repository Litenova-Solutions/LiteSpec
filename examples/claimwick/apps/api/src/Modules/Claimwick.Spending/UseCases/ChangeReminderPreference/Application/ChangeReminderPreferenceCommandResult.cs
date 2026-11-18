namespace Claimwick.Spending.UseCases.ChangeReminderPreference.Application;

internal sealed record ChangeReminderPreferenceCommandResult(Guid ManagerId, bool ReceivesReminders);
