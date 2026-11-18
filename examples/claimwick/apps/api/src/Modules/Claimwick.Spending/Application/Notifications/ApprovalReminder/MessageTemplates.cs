using System.Collections.Generic;

namespace Claimwick.Spending.Application.Notifications.ApprovalReminder;

/// <summary>The notification templates, keyed by template key.</summary>
internal static class MessageTemplates
{
    /// <summary>The templates, keyed by template key.</summary>
    internal static readonly Dictionary<string, MessageTemplate> Templates = new()
    {
        ["approval-reminder-manager"] = new MessageTemplate(
            "Expenses from {{SubmitterName}} await approval",
            "Hello, {{SubmitterName}} has expenses waiting for a decision for five days."),
    };
}
