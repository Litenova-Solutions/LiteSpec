namespace Library.Lending.Application.Loans.BorrowCopy;

internal sealed class BorrowCopyCommandTenancyDefinition : ITenancyDefinition<BorrowCopyCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
