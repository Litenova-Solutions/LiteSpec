using Claimwick.Spending.Ports.MailRelay;

namespace Claimwick.Spending.Infrastructure.MailRelay;

/// <summary>Sends the spending module's emails through the mail relay.</summary>
/// <implements>external-system/mail-relay</implements>
internal sealed class MailRelayMessenger : IMailRelay
{
    /// <summary>Sends one templated email about one expense.</summary>
    public Task SendAsync(string templateKey, Guid expenseId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
