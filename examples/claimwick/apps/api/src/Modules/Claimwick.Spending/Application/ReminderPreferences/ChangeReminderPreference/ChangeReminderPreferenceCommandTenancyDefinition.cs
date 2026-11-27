using LiteBus.Messaging.Abstractions.Tenancy;

namespace Claimwick.Spending.Application.ReminderPreferences.ChangeReminderPreference;

/// <summary>Declares the change-reminder-preference command single-tenant.</summary>
internal sealed class ChangeReminderPreferenceCommandTenancyDefinition : ITenancyDefinition<ChangeReminderPreferenceCommand>
{
    public TenancyDeclaration Tenancy => TenancyDeclaration.Required();
}
