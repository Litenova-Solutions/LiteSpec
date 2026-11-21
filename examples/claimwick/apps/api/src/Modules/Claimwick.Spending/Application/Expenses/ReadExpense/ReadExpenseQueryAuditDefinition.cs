using Claimwick.Spending.Contracts.Expenses.ReadExpense;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ReadExpense;

/// <summary>Declares the read-expense query exempt from auditing.</summary>
internal sealed class ReadExpenseQueryAuditDefinition : IAuditDefinition<ReadExpenseQuery>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Reads trail nothing.");
}
