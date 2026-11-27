using Claimwick.Spending.Domain.Expenses.Events;

namespace Claimwick.Spending.Contracts.Expenses.OnExpenseSubmitted;

/// <summary>The expense-submitted event announces a newly submitted expense to the readers outside the product, through the event bus.</summary>
/// <implements>integration-event/expense-submitted</implements>
/// <follows>litespec/core/backend-application#stage-declared-module-and-integration-events-in-the-commit</follows>
public sealed record ExpenseSubmittedIntegrationEvent(ExpenseId ExpenseId)
{
    /// <summary>The CloudEvents type the event travels under.</summary>
    public const string EventType = "com.claimwick.expense.submitted";

    /// <summary>Builds the integration event from the domain event the expense raised; the payload carries only the expense.</summary>
    internal static ExpenseSubmittedIntegrationEvent From(ExpenseSubmittedDomainEvent domainEvent) =>
        new(domainEvent.ExpenseId);
}
