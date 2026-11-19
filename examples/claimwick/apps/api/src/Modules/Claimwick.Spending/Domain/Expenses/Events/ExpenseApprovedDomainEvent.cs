using Claimwick.Kernel;
using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was approved.</summary>
/// <implements>domain-event/expense.approved</implements>
internal sealed record ExpenseApprovedDomainEvent(ExpenseId ExpenseId, Guid ApprovedBy, DateTimeOffset DecidedAt) : IDomainEvent;
