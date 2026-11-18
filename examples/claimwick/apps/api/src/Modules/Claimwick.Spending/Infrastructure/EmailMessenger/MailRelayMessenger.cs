using Claimwick.Spending.Ports.EmailMessenger;

namespace Claimwick.Spending.Infrastructure.EmailMessenger;

/// <summary>Sends the spending module's emails through the mail relay.</summary>
/// <implements>external-system/mail-relay</implements>
internal sealed class MailRelayMessenger : IEmailMessenger
{
    /// <summary>Sends one templated email about one expense.</summary>
    public Task SendAsync(string templateKey, Guid expenseId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
