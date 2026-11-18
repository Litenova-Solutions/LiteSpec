using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was refused.</summary>
/// <implements>domain-event/expense.refused</implements>
internal sealed record ExpenseRefusedEvent(Guid ExpenseId, Guid RefusedBy, DateTimeOffset DecidedAt) : IDomainEvent;
