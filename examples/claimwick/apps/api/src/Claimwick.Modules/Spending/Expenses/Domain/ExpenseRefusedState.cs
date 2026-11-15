namespace Claimwick.Spending.Expenses.Domain;

/// <summary>The expense was refused.</summary>
public sealed record ExpenseRefusedState(Guid RefusedBy, DateTimeOffset DecidedAt, string Reason) : ExpenseState;
