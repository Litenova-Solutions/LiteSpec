using LiteBus.Messaging.Abstractions;

namespace Claimwick.Reporting.Application.ListReimbursedExpenses;

/// <summary>Declares the list-reimbursed-expenses query exempt from auditing.</summary>
internal sealed class ListReimbursedExpensesQueryAuditDefinition : IAuditDefinition<ListReimbursedExpensesQuery>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Reads trail nothing.");
}
