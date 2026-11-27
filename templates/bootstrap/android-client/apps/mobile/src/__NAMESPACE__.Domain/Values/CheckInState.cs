namespace __NAMESPACE__.Domain.Values;

/// <summary>The lifecycle of one recorded health check-in. A fresh check-in is recorded.</summary>
public abstract record CheckInState
{
    /// <summary>Gets the stable code the store carries for the state.</summary>
    public abstract string Code { get; }

    /// <summary>Reads a state back from its stored code.</summary>
    /// <param name="code">The stored code.</param>
    /// <returns>The check-in state the code names.</returns>
    /// <exception cref="ArgumentException">The code names no check-in state.</exception>
    public static CheckInState FromCode(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        return code switch
        {
            RecordedState.RecordedCode => new RecordedState(),
            _ => throw new ArgumentException("The check-in state '" + code + "' is unknown.", nameof(code)),
        };
    }
}
