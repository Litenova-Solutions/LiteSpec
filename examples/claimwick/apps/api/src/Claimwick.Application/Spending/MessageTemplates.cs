using System.Collections.Generic;

namespace Claimwick.Application.Spending;

/// <summary>One notification template: its subject and its body.</summary>
public sealed record MessageTemplate(string Subject, string Body);

/// <summary>The notification templates, keyed by template key.</summary>
public static class MessageTemplates
{
    /// <summary>The templates, keyed by template key.</summary>
    public static readonly Dictionary<string, MessageTemplate> Templates = new()
    {
        ["approval-reminder-manager"] = new MessageTemplate(
            "Expenses from {{SubmitterName}} await approval",
            "Hello, {{SubmitterName}} has expenses waiting for a decision for five days."),
    };
}
