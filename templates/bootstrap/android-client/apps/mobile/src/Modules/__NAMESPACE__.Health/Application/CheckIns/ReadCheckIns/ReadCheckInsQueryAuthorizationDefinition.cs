namespace __NAMESPACE__.Health.Application.CheckIns.ReadCheckIns;

/// <summary>Declares who may read check-ins: the visitor with read permission.</summary>
internal sealed class ReadCheckInsQueryAuthorizationDefinition
{
    /// <summary>Gets the authorization: Authorized for permission read-check-ins.</summary>
    public string Authorization => "Authorized read-check-ins";
}
