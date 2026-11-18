namespace Claimwick.Spending.Domain.Expenses;

/// <summary>The expense was reimbursed.</summary>
internal sealed record ExpenseReimbursedState(DateTimeOffset ReimbursedAt) : ExpenseState;
