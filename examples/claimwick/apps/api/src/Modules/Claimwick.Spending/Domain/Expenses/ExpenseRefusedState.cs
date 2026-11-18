namespace Claimwick.Spending.Domain.Expenses;

/// <summary>The expense was refused.</summary>
internal sealed record ExpenseRefusedState(Guid RefusedBy, DateTimeOffset DecidedAt, string Reason) : ExpenseState;
