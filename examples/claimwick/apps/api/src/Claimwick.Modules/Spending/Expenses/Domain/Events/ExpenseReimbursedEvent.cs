using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Events;

/// <summary>An expense was reimbursed.</summary>
/// <implements>event/expense.reimbursed</implements>
public sealed record ExpenseReimbursedEvent(Guid ExpenseId, decimal Amount, string Currency, DateTimeOffset ReimbursedAt) : IDomainEvent;
