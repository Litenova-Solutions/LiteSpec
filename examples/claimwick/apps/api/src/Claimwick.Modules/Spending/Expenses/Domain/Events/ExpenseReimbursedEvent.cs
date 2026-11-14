using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Events;

/// <summary>An expense was reimbursed to the employee who submitted it.</summary>
/// <implements>event/expense.reimbursed</implements>
public sealed record ExpenseReimbursedEvent(
    Guid ExpenseId,
    Guid EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt) : IDomainEvent;
