using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was submitted.</summary>
/// <implements>domain-event/expense.submitted</implements>
internal sealed record ExpenseSubmittedEvent(Guid ExpenseId, decimal Amount, string Currency, Guid EmployeeId) : IDomainEvent;
