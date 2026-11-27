namespace Library.Lending.BorrowCopy;

internal sealed class BorrowCopyCommandTenancyDefinition : ITenancyDefinition<BorrowCopyCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
