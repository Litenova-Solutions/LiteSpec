namespace Claimwick.Spending.Application;

/// <summary>The mail relay delivering the spending module's emails to managers.</summary>
/// <implements>external-system/mail-relay</implements>
public interface IEmailMessenger
{
    /// <summary>Sends one templated email about one expense.</summary>
    Task SendAsync(string templateKey, Guid expenseId, CancellationToken cancellationToken);
}
