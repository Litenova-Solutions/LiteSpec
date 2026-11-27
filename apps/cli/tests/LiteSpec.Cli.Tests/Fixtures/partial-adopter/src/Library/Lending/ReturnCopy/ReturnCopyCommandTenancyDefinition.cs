namespace Library.Lending.ReturnCopy;

internal sealed class ReturnCopyCommandTenancyDefinition : ITenancyDefinition<ReturnCopyCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
