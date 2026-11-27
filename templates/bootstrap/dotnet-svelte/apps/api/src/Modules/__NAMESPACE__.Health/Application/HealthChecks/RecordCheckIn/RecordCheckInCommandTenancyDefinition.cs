using LiteBus.Messaging.Abstractions.Tenancy;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// Declares the tenancy of recording a check-in: the health sample runs with
/// no tenant split, so the command carries none.
/// </summary>
internal sealed class RecordCheckInCommandTenancyDefinition : ITenancyDefinition<RecordCheckInCommand>
{
    /// <inheritdoc />
    public TenancyDeclaration Tenancy => TenancyDeclaration.None("The Health sample is a single-tenant starter with no tenant split.");
}
