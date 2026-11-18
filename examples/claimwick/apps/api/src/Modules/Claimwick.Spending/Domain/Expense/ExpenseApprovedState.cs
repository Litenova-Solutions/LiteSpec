namespace Claimwick.Spending.Domain.Expense;

/// <summary>The expense was approved.</summary>
internal sealed record ExpenseApprovedState(Guid ApprovedBy, DateTimeOffset DecidedAt) : ExpenseState;
