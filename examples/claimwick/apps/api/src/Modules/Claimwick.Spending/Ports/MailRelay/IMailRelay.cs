namespace Claimwick.Spending.Ports.MailRelay;

/// <summary>The mail relay delivering the spending module's emails to managers.</summary>
/// <implements>external-system/mail-relay</implements>
internal interface IMailRelay
{
    /// <summary>Sends one templated email about one expense.</summary>
    Task SendAsync(string templateKey, Guid expenseId, CancellationToken cancellationToken);
}
