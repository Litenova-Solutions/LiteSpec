namespace Claimwick.Spending.Domain.ReminderPreference;

/// <summary>The preference applies: the manager skips approval reminders.</summary>
internal sealed record ReminderPreferenceActiveState(Guid ManagerId, string? Reason, DateTimeOffset UpdatedAt) : ReminderPreferenceState;
