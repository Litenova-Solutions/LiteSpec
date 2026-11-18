namespace Claimwick.Spending.Application.ChangeReminderPreference;

internal sealed record ChangeReminderPreferenceCommand(Guid ManagerId, Guid ChangerId, bool ChangerIsFinance, bool ReceivesReminders, string? Reason, DateTimeOffset HandledAt);
