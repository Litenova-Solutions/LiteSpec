using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expense.Events;

/// <summary>The personal data of a decided expense was erased.</summary>
/// <implements>domain-event/expense.personal-data-erased</implements>
internal sealed record ExpensePersonalDataErasedEvent(Guid ExpenseId) : IDomainEvent;
