using LiteBus.Auditing.Abstractions;
using LiteBus.Messaging.Abstractions;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// Declares how the audit trail records a check-in: the action with the
/// check-in it wrote as its target.
/// </summary>
internal sealed class RecordCheckInCommandAuditDefinition : IAuditDefinition<RecordCheckInCommand>
{
    /// <inheritdoc />
    public AuditDeclaration Audit =>
        AuditDeclaration.Audited("use-case/health.record-check-in", "health-change", "health-check", reasonRequired: false)
            .WithTargetFrom<RecordCheckInCommand>(command => command.CheckInId.Value.ToString());
}
