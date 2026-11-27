namespace __NAMESPACE__.Health.Domain.HealthChecks.Parts;

/// <summary>
/// The lifecycle of one check-in. A fresh check-in is recorded; later states
/// gain their own sealed records beside it.
/// </summary>
internal abstract record HealthCheckState
{
    /// <summary>The stable code the store carries for the state.</summary>
    public abstract string Code { get; }

    /// <summary>Reads a state back from its stored code.</summary>
    /// <param name="code">The stored code.</param>
    /// <exception cref="ArgumentException">The code names no check-in state.</exception>
    public static HealthCheckState FromCode(string code)
    {
        return code switch
        {
            RecordedState.RecordedCode => new RecordedState(),
            _ => throw new ArgumentException($"The check-in state '{code}' is unknown.", nameof(code)),
        };
    }
}
