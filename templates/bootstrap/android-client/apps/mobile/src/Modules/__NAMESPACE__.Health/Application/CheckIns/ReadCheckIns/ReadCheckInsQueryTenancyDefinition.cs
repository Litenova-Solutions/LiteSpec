namespace __NAMESPACE__.Health.Application.CheckIns.ReadCheckIns;

/// <summary>Declares the tenancy of reading check-ins: the visitor reads their own check-ins.</summary>
/// <remarks>Implements ITenancyDefinition for the query.</remarks>
internal sealed class ReadCheckInsQueryTenancyDefinition
{
    /// <summary>Gets the tenancy: Required for the own scope.</summary>
    public string Tenancy => "Required own";
}
