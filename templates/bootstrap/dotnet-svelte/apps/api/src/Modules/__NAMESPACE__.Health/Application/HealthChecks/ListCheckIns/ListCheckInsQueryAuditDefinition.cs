using LiteBus.Auditing.Abstractions;
using LiteBus.Messaging.Abstractions;

namespace __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;

/// <summary>
/// Declares how the audit trail records a check-in list: the action with
/// no single target, so the target names the collection it read.
/// </summary>
internal sealed class ListCheckInsQueryAuditDefinition : IAuditDefinition<ListCheckInsQuery>
{
    /// <inheritdoc />
    public AuditDeclaration Audit =>
        AuditDeclaration.Audited("use-case/health.list-check-ins", "health-change", "health-check", reasonRequired: false)
            .WithTargetFrom<ListCheckInsQuery>(_ => "health-checks");
}
