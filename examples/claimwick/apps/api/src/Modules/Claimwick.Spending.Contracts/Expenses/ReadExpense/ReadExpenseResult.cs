namespace Claimwick.Spending.Contracts.Expenses.ReadExpense;

/// <summary>One expense with its current lifecycle state.</summary>
public sealed record ReadExpenseResult(Guid ExpenseId, string State);
