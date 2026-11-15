namespace Claimwick.Spending.Expenses.Domain;

/// <summary>The expense was approved.</summary>
public sealed record ExpenseApprovedState(Guid ApprovedBy, DateTimeOffset DecidedAt) : ExpenseState;
