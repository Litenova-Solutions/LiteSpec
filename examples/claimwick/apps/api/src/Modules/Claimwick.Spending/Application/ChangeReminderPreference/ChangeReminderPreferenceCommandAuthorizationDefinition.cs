using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.ChangeReminderPreference;

/// <summary>Authorizes the change-reminder-preference command from its declared permission.</summary>
internal sealed class ChangeReminderPreferenceCommandAuthorizationDefinition : IAuthorizationDefinition<ChangeReminderPreferenceCommand>
{
    public static Permission Policy { get; } = new("permission/manage-reminder-preference");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
