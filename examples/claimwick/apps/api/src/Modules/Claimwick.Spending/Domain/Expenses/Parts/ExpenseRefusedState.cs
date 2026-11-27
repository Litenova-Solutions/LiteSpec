namespace Claimwick.Spending.Domain.Expenses.Parts;

/// <summary>The expense was refused.</summary>
internal sealed record ExpenseRefusedState(Guid RefusedBy, DateTimeOffset DecidedAt, string Reason) : ExpenseState;
