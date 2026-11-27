using LiteBus.Messaging.Abstractions.Tenancy;

namespace __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;

/// <summary>
/// Declares the tenancy of listing check-ins: the health sample runs with
/// no tenant split, so the query carries none.
/// </summary>
internal sealed class ListCheckInsQueryTenancyDefinition : ITenancyDefinition<ListCheckInsQuery>
{
    /// <inheritdoc />
    public TenancyDeclaration Tenancy => TenancyDeclaration.None("The Health sample is a single-tenant starter with no tenant split.");
}
