namespace Library.Lending.Application.Copies.RegisterCopy;

internal sealed class RegisterCopyCommandTenancyDefinition : ITenancyDefinition<RegisterCopyCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
