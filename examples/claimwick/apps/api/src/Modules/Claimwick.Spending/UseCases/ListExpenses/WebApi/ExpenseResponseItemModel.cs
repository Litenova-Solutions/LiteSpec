namespace Claimwick.Spending.UseCases.ListExpenses.WebApi;

/// <summary>One expense row: its identity, amount, currency, state and submission time.</summary>
internal sealed record ExpenseResponseItemModel(Guid ExpenseId, decimal Amount, string Currency, string State, DateTimeOffset SubmittedAt);
