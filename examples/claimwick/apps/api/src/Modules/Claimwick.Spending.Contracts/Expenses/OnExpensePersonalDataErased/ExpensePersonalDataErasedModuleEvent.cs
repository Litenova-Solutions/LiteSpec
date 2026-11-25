using Claimwick.Spending.Domain.Expenses.Events;

namespace Claimwick.Spending.Contracts.Expenses.OnExpensePersonalDataErased;

/// <follows>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</follows>
public sealed partial record ExpensePersonalDataErasedModuleEvent
{
    /// <summary>Builds the module event from the domain event the expense raised.</summary>
    internal static ExpensePersonalDataErasedModuleEvent From(ExpensePersonalDataErasedDomainEvent domainEvent) =>
        new(domainEvent.ExpenseId);
}
