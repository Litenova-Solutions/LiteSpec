namespace Claimwick.Spending.Application.Expenses.ListExpenses;

/// <summary>One row of an employee's expense list, projected by the read session.</summary>
internal sealed record ExpenseSummary(Guid ExpenseId, Guid EmployeeId, decimal Amount, string Currency, string State, DateTimeOffset SubmittedAt);
