using Claimwick.Spending.Domain.Expenses.Events;

namespace Claimwick.Spending.Contracts.Expenses.OnExpensePersonalDataErased;

/// <summary>The expense-personal-data-erased event announces that the personal data of a decided expense was erased.</summary>
/// <implements>module-event/spending.expense-personal-data-erased</implements>
/// <follows>litespec/core/backend-application#stage-declared-module-and-integration-events-in-the-commit</follows>
public sealed record ExpensePersonalDataErasedModuleEvent(ExpenseId ExpenseId)
{
    /// <summary>Builds the module event from the domain event the expense raised.</summary>
    internal static ExpensePersonalDataErasedModuleEvent From(ExpensePersonalDataErasedDomainEvent domainEvent) =>
        new(domainEvent.ExpenseId);
}
