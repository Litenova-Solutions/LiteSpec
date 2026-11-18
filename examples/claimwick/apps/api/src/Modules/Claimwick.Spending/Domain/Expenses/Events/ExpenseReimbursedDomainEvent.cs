using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was reimbursed to the employee who submitted it.</summary>
/// <implements>domain-event/expense.reimbursed</implements>
internal sealed record ExpenseReimbursedDomainEvent(
    Guid ExpenseId,
    Guid EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt) : IDomainEvent;
