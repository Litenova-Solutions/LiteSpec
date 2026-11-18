namespace Claimwick.Spending.Domain.Expenses;

/// <summary>The expense was approved.</summary>
internal sealed record ExpenseApprovedState(Guid ApprovedBy, DateTimeOffset DecidedAt) : ExpenseState;
