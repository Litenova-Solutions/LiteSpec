using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Policies.ChangeReceiptThreshold;

/// <summary>Declares the change-receipt-threshold command single-tenant.</summary>
internal sealed class ChangeReceiptThresholdCommandTenancyDefinition : ITenancyDefinition<ChangeReceiptThresholdCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
