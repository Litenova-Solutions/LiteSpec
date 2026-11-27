namespace __NAMESPACE__.Health.WebApi.CheckIns.ReadCheckIns;

/// <summary>Exposes the stored check-in list under the list operation for the phone list.</summary>
/// <implements>use-case/health.read-check-ins</implements>
internal sealed class ReadCheckInsEndpoint : IEndpoint
{
    /// <summary>Gets the operation name the list screen calls.</summary>
    /// <returns>The list operation name.</returns>
    public string Operation()
    {
        return WithName("List");
    }

    /// <summary>Names one operation the endpoint maps.</summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>The operation name.</returns>
    private static string WithName(string operation)
    {
        return operation;
    }
}
