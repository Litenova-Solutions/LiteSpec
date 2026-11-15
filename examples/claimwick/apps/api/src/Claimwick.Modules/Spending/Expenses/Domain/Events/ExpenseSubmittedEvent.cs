using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Events;

/// <summary>An expense was submitted.</summary>
/// <implements>domain-event/expense.submitted</implements>
public sealed record ExpenseSubmittedEvent(Guid ExpenseId, decimal Amount, string Currency, Guid EmployeeId) : IDomainEvent;
