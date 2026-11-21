using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ErasePersonalData;

/// <summary>Authorizes the erase-personal-data command from its declared permission.</summary>
internal sealed class ErasePersonalDataCommandAuthorizationDefinition : IAuthorizationDefinition<ErasePersonalDataCommand>
{
    public static Permission Policy { get; } = new("permission/remove-personal-data");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
