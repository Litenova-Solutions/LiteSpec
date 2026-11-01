using Claimwick.Domain.Shared;

namespace Claimwick.Domain.Spending.Expenses.Events;

/// <summary>An expense was refused.</summary>
/// <implements>event/expense.refused</implements>
public sealed record ExpenseRefusedEvent(Guid ExpenseId, Guid RefusedBy, DateTimeOffset DecidedAt) : IDomainEvent;
