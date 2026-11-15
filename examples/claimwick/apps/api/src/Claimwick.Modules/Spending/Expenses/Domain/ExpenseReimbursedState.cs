namespace Claimwick.Spending.Expenses.Domain;

/// <summary>The expense was reimbursed.</summary>
public sealed record ExpenseReimbursedState(DateTimeOffset ReimbursedAt) : ExpenseState;
