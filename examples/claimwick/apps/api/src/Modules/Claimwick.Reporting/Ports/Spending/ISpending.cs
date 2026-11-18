namespace Claimwick.Reporting.Ports.Spending;

/// <summary>Answers what Reporting needs from the spending module, in Reporting's own types.</summary>
internal interface ISpending
{
    /// <summary>Confirms one expense still exists in the spending module.</summary>
    Task<bool> ExpenseExistsAsync(Guid expenseId, CancellationToken cancellationToken);
}
