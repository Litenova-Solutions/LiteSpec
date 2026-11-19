using Claimwick.Kernel.Domain;
using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was reimbursed to the employee who submitted it.</summary>
/// <implements>domain-event/expense.reimbursed</implements>
internal sealed record ExpenseReimbursedDomainEvent(
    ExpenseId ExpenseId,
    Guid EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt) : IDomainEvent;
