using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Events;

/// <summary>An expense was refused.</summary>
/// <implements>event/expense.refused</implements>
public sealed record ExpenseRefusedEvent(Guid ExpenseId, Guid RefusedBy, DateTimeOffset DecidedAt) : IDomainEvent;
