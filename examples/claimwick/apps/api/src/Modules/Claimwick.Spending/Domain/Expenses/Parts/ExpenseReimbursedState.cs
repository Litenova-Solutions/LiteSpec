namespace Claimwick.Spending.Domain.Expenses.Parts;

/// <summary>The expense was reimbursed.</summary>
internal sealed record ExpenseReimbursedState(DateTimeOffset ReimbursedAt) : ExpenseState;
