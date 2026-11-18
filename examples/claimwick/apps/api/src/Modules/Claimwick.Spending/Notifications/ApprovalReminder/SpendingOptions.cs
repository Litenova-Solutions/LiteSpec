namespace Claimwick.Spending.Notifications.ApprovalReminder;

/// <summary>The spending settings bound from configuration.</summary>
/// <implements>setting/spending.approval-reminder-delay</implements>
internal sealed class SpendingOptions
{
    /// <summary>The configuration section holding the spending settings.</summary>
    internal const string SectionName = "Spending";

    /// <summary>How long an expense waits before the product reminds its manager.</summary>
    public TimeSpan ApprovalReminderDelay { get; init; } = TimeSpan.FromHours(72);
}
