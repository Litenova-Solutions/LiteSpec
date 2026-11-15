namespace Claimwick.Spending.Application;

/// <summary>One notification template: its subject and its body.</summary>
public sealed record MessageTemplate(string Subject, string Body);
