using Claimwick.Spending.Expenses.Domain.Events;

namespace Claimwick.Spending.Contracts.IntegrationEvents;

/// <summary>The expense-personal-data-erased event announces that the personal data of a decided expense was erased.</summary>
/// <implements>integration-event/expense-personal-data-erased</implements>
/// <follows>litespec/backend/application#emit-declared-integration-events-after-commit</follows>
public sealed record ExpensePersonalDataErased(Guid ExpenseId)
{
    /// <summary>Builds the integration event from the domain event the expense raised.</summary>
    public static ExpensePersonalDataErased From(ExpensePersonalDataErasedEvent domainEvent) =>
        new(domainEvent.ExpenseId);
}
