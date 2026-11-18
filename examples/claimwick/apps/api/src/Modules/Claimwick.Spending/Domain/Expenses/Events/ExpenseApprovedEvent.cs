using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was approved.</summary>
/// <implements>domain-event/expense.approved</implements>
internal sealed record ExpenseApprovedEvent(Guid ExpenseId, Guid ApprovedBy, DateTimeOffset DecidedAt) : IDomainEvent;
