namespace Library.Lending.Application.Holds.CancelHold;

internal sealed class CancelHoldCommandTenancyDefinition : ITenancyDefinition<CancelHoldCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
