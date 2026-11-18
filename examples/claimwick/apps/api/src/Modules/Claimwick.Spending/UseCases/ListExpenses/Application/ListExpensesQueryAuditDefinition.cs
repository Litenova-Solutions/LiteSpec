using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.ListExpenses.Application;

/// <summary>Declares the list-expenses query exempt from auditing.</summary>
internal sealed class ListExpensesQueryAuditDefinition : IAuditDefinition<ListExpensesQuery>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Reads trail nothing.");
}
