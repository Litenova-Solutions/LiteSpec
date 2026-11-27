using LiteBus.Auditing.Abstractions;
using LiteBus.Messaging.Abstractions;

namespace __NAMESPACE__.Health.Application.HealthChecks.ReadCheckIn;

/// <summary>
/// Declares how the audit trail records a check-in read: the action with the
/// check-in it read as its target.
/// </summary>
internal sealed class ReadCheckInQueryAuditDefinition : IAuditDefinition<ReadCheckInQuery>
{
    /// <inheritdoc />
    public AuditDeclaration Audit =>
        AuditDeclaration.Audited("use-case/health.read-check-in", "health-change", "health-check", reasonRequired: false)
            .WithTargetFrom<ReadCheckInQuery>(query => query.CheckInId.Value.ToString());
}
