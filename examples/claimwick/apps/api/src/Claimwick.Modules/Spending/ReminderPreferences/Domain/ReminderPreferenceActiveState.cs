namespace Claimwick.Spending.ReminderPreferences.Domain;

/// <summary>The preference applies: the manager skips approval reminders.</summary>
public sealed record ReminderPreferenceActiveState(Guid ManagerId, string? Reason, DateTimeOffset UpdatedAt) : ReminderPreferenceState;
