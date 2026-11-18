using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was approved.</summary>
/// <implements>domain-event/expense.approved</implements>
internal sealed record ExpenseApprovedDomainEvent(Guid ExpenseId, Guid ApprovedBy, DateTimeOffset DecidedAt) : IDomainEvent;
