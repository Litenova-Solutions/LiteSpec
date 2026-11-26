using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

/// <summary>Declares the attach-receipt command audited, with the attached receipt as its target.</summary>
internal sealed class AttachReceiptCommandAuditDefinition : IAuditDefinition<AttachReceiptCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Audited("use-case/spending.attach-receipt")
        .WithTargetFrom<AttachReceiptCommand>(command => command.ReceiptId.ToString());
}
