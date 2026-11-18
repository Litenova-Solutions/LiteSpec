using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.AttachReceipt;

/// <summary>Declares the attach-receipt command exempt from auditing.</summary>
internal sealed class AttachReceiptCommandAuditDefinition : IAuditDefinition<AttachReceiptCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Attachments leave their trail in the expense and receipt states.");
}
