using Claimwick.Kernel;
using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Expenses.Events;

/// <summary>The personal data of a decided expense was erased.</summary>
/// <implements>domain-event/expense.personal-data-erased</implements>
internal sealed record ExpensePersonalDataErasedDomainEvent(ExpenseId ExpenseId) : IDomainEvent;
