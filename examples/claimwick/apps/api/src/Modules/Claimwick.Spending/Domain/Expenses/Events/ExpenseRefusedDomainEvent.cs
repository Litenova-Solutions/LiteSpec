using Claimwick.Kernel;
using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>An expense was refused.</summary>
/// <implements>domain-event/expense.refused</implements>
internal sealed record ExpenseRefusedDomainEvent(ExpenseId ExpenseId, Guid RefusedBy, DateTimeOffset DecidedAt) : IDomainEvent;
