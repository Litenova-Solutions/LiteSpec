using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was refused.</summary>
/// <implements>domain-event/expense.refused</implements>
internal sealed record ExpenseRefusedDomainEvent(Guid ExpenseId, Guid RefusedBy, DateTimeOffset DecidedAt) : IDomainEvent;
