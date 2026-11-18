using Claimwick.Spending.Domain.Expense.Events;

namespace Claimwick.Spending.Contracts.ModuleEvents;

/// <summary>The expense-personal-data-erased event announces that the personal data of a decided expense was erased.</summary>
/// <implements>module-event/spending.expense-personal-data-erased</implements>
/// <follows>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</follows>
public sealed record ExpensePersonalDataErased(Guid ExpenseId)
{
    /// <summary>Builds the module event from the domain event the expense raised.</summary>
    internal static ExpensePersonalDataErased From(ExpensePersonalDataErasedEvent domainEvent) =>
        new(domainEvent.ExpenseId);
}
