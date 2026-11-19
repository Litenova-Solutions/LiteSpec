using Claimwick.Kernel;
using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was submitted.</summary>
/// <implements>domain-event/expense.submitted</implements>
internal sealed record ExpenseSubmittedDomainEvent(ExpenseId ExpenseId, decimal Amount, string Currency, Guid EmployeeId) : IDomainEvent;
