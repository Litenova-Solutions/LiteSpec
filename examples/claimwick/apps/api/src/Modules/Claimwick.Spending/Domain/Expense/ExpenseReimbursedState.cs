namespace Claimwick.Spending.Domain.Expense;

/// <summary>The expense was reimbursed.</summary>
internal sealed record ExpenseReimbursedState(DateTimeOffset ReimbursedAt) : ExpenseState;
