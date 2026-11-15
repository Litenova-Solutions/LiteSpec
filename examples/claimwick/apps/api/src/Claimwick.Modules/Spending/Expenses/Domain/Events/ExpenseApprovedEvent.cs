using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Events;

/// <summary>An expense was approved.</summary>
/// <implements>domain-event/expense.approved</implements>
public sealed record ExpenseApprovedEvent(Guid ExpenseId, Guid ApprovedBy, DateTimeOffset DecidedAt) : IDomainEvent;
