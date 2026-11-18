using Claimwick.Spending.Domain.Expenses.Events;

namespace Claimwick.Spending.Contracts.Expenses;

/// <summary>The expense-reimbursed event announces a reimbursed expense and the employee it was paid to.</summary>
/// <implements>module-event/spending.expense-reimbursed</implements>
/// <follows>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</follows>
public sealed record ExpenseReimbursedModuleEvent(
    Guid ExpenseId,
    Guid EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt)
{
    /// <summary>Builds the module event from the domain event the expense raised.</summary>
    internal static ExpenseReimbursedModuleEvent From(ExpenseReimbursedDomainEvent domainEvent) =>
        new(domainEvent.ExpenseId, domainEvent.EmployeeId, domainEvent.Amount, domainEvent.Currency, domainEvent.ReimbursedAt);
}
