using LiteBus.Queries.Abstractions;

namespace Claimwick.Spending.Contracts.Expenses.ReadExpense;

/// <summary>Reads one expense with its current state.</summary>
public sealed record ReadExpenseQuery(ExpenseId ExpenseId) : IQuery<ReadExpenseResult?>;
