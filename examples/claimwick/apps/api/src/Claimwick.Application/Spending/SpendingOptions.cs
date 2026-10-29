namespace Claimwick.Application.Spending;

/// <summary>The spending settings bound from configuration.</summary>
/// <implements>setting/spending.approval-reminder-delay</implements>
public sealed class SpendingOptions
{
    /// <summary>The configuration section holding the spending settings.</summary>
    public const string SectionName = "Spending";

    /// <summary>How long an expense waits before the product reminds its manager.</summary>
    public TimeSpan ApprovalReminderDelay { get; init; } = TimeSpan.FromHours(48);
}
