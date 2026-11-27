using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

/// <summary>Declares the attach-receipt command single-tenant.</summary>
internal sealed class AttachReceiptCommandTenancyDefinition : ITenancyDefinition<AttachReceiptCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
