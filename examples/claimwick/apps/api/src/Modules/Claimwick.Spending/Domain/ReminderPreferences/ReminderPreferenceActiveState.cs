namespace Claimwick.Spending.Domain.ReminderPreferences;

/// <summary>The preference applies: the manager skips approval reminders.</summary>
internal sealed record ReminderPreferenceActiveState(Guid ManagerId, string? Reason, DateTimeOffset UpdatedAt) : ReminderPreferenceState;
