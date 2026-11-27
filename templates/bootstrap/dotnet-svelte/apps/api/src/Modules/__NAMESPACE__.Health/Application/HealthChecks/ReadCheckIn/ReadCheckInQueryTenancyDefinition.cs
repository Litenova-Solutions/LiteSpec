using LiteBus.Messaging.Abstractions.Tenancy;

namespace __NAMESPACE__.Health.Application.HealthChecks.ReadCheckIn;

/// <summary>
/// Declares the tenancy of reading a check-in: the health sample runs with
/// no tenant split, so the query carries none.
/// </summary>
internal sealed class ReadCheckInQueryTenancyDefinition : ITenancyDefinition<ReadCheckInQuery>
{
    /// <inheritdoc />
    public TenancyDeclaration Tenancy => TenancyDeclaration.None("The Health sample is a single-tenant starter with no tenant split.");
}
