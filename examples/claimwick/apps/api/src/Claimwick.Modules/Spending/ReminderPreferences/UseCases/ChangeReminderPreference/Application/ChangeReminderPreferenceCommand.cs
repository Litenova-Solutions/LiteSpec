namespace Claimwick.Spending.ReminderPreferences.UseCases.ChangeReminderPreference.Application;

public sealed record ChangeReminderPreferenceCommand(Guid ManagerId, Guid ChangerId, bool ChangerIsFinance, bool ReceivesReminders, string? Reason);
