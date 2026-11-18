using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.ChangeReminderPreference.Application;

/// <summary>Declares the change-reminder-preference command exempt from auditing.</summary>
internal sealed class ChangeReminderPreferenceCommandAuditDefinition : IAuditDefinition<ChangeReminderPreferenceCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("The preference holds its choice; the change trails nothing further.");
}
