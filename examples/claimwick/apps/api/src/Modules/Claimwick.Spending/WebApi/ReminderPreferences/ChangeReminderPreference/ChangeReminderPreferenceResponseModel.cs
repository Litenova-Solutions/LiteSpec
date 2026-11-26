namespace Claimwick.Spending.WebApi.ReminderPreferences.ChangeReminderPreference;

/// <summary>The answer to a change-preference request: the manager and the new choice.</summary>
internal sealed record ChangeReminderPreferenceResponseModel(Guid ManagerId, bool ReceivesReminders);
