namespace Claimwick.Spending.Application.Notifications.ApprovalReminder;

/// <summary>One notification template: its subject and its body.</summary>
internal sealed record MessageTemplate(string Subject, string Body);
