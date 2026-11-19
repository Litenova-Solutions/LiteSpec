using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ExportExpenses;

/// <summary>Declares the export-expenses query exempt from auditing.</summary>
internal sealed class ExportExpensesQueryAuditDefinition : IAuditDefinition<ExportExpensesQuery>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Reads trail nothing.");
}
