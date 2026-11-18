namespace Library.Application.Lending.Copies.RegisterCopy;

internal sealed class RegisterCopyCommandAuthorizationDefinition : IAuthorizationDefinition<RegisterCopyCommand>
{
    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(AccessPolicies.Permission("manage-copies"));
}
