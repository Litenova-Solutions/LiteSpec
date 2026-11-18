using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.ListExpenses;

/// <summary>Declares the list-expenses query exempt from auditing.</summary>
internal sealed class ListExpensesQueryAuditDefinition : IAuditDefinition<ListExpensesQuery>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Reads trail nothing.");
}
