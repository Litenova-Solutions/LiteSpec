namespace Claimwick.Spending.Contracts.Expenses;

/// <summary>One line of an expense: the kind of cost with its amount in the currency of the expense. Spending commands share it, so it lives in Contracts.</summary>
public sealed record ExpenseLine(string Category, decimal Amount);
