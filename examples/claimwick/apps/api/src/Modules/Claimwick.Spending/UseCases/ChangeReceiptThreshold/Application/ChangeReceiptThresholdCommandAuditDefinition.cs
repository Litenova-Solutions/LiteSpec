using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.ChangeReceiptThreshold.Application;

/// <summary>Declares the change-receipt-threshold command exempt from auditing.</summary>
internal sealed class ChangeReceiptThresholdCommandAuditDefinition : IAuditDefinition<ChangeReceiptThresholdCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("The policy holds its threshold; the change trails nothing further.");
}
