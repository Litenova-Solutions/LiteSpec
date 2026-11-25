using Claimwick.Spending.Domain.Expenses.Events;

namespace Claimwick.Spending.Contracts.Expenses.OnExpenseReimbursed;

/// <follows>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</follows>
public sealed partial record ExpenseReimbursedModuleEvent
{
    /// <summary>Builds the module event from the domain event the expense raised.</summary>
    internal static ExpenseReimbursedModuleEvent From(ExpenseReimbursedDomainEvent domainEvent) =>
        new(domainEvent.ExpenseId, domainEvent.EmployeeId, domainEvent.Amount, domainEvent.Currency, domainEvent.ReimbursedAt);
}
