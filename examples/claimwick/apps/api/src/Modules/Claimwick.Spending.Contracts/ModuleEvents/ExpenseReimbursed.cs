using Claimwick.Spending.Domain.Expenses.Events;

namespace Claimwick.Spending.Contracts.ModuleEvents;

/// <summary>The expense-reimbursed event announces a reimbursed expense and the employee it was paid to.</summary>
/// <implements>module-event/spending.expense-reimbursed</implements>
/// <follows>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</follows>
public sealed record ExpenseReimbursed(
    Guid ExpenseId,
    Guid EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt)
{
    /// <summary>Builds the module event from the domain event the expense raised.</summary>
    internal static ExpenseReimbursed From(ExpenseReimbursedEvent domainEvent) =>
        new(domainEvent.ExpenseId, domainEvent.EmployeeId, domainEvent.Amount, domainEvent.Currency, domainEvent.ReimbursedAt);
}
