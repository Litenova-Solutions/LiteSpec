namespace Claimwick.Spending.UseCases.ChangeReminderPreference.Application;

internal sealed record ChangeReminderPreferenceCommand(Guid ManagerId, Guid ChangerId, bool ChangerIsFinance, bool ReceivesReminders, string? Reason, DateTimeOffset HandledAt);
