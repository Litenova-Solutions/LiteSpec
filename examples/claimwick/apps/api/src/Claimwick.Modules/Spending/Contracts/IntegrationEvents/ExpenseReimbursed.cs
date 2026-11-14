using Claimwick.Spending.Expenses.Domain.Events;

namespace Claimwick.Spending.Contracts.IntegrationEvents;

/// <summary>The expense-reimbursed event announces a reimbursed expense and the employee it was paid to.</summary>
/// <implements>integration-event/expense-reimbursed</implements>
/// <follows>litespec/backend/application#emit-declared-integration-events-after-commit</follows>
public sealed record ExpenseReimbursed(
    Guid ExpenseId,
    Guid EmployeeId,
    decimal Amount,
    string Currency,
    DateTimeOffset ReimbursedAt)
{
    /// <summary>Builds the integration event from the domain event the expense raised.</summary>
    public static ExpenseReimbursed From(ExpenseReimbursedEvent domainEvent) =>
        new(domainEvent.ExpenseId, domainEvent.EmployeeId, domainEvent.Amount, domainEvent.Currency, domainEvent.ReimbursedAt);
}
