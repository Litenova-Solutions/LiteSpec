namespace Library.Lending.Application.Holds.PlaceHold;

internal sealed class PlaceHoldCommandTenancyDefinition : ITenancyDefinition<PlaceHoldCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
