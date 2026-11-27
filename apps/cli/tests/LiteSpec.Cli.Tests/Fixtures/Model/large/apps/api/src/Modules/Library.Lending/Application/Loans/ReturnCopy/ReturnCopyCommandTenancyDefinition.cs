namespace Library.Lending.Application.Loans.ReturnCopy;

internal sealed class ReturnCopyCommandTenancyDefinition : ITenancyDefinition<ReturnCopyCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
