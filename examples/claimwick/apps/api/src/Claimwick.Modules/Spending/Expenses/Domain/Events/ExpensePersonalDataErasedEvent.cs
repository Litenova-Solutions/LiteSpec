using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Events;

/// <summary>The personal data of a decided expense was erased.</summary>
/// <implements>event/expense.personal-data-erased</implements>
public sealed record ExpensePersonalDataErasedEvent(Guid ExpenseId) : IDomainEvent;
