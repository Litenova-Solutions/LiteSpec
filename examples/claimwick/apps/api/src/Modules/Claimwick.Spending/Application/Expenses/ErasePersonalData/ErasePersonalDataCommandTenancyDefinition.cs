using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.Expenses.ErasePersonalData;

/// <summary>Declares the erase-personal-data command single-tenant.</summary>
internal sealed class ErasePersonalDataCommandTenancyDefinition : ITenancyDefinition<ErasePersonalDataCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
