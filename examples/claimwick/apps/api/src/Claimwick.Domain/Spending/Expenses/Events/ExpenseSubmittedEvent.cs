namespace Claimwick.Domain.Spending.Expenses.Events;

/// <summary>An expense was submitted.</summary>
/// <implements>event/expense.submitted</implements>
public sealed record ExpenseSubmittedEvent(Guid ExpenseId, decimal Amount, string Currency, Guid EmployeeId) : IDomainEvent;
