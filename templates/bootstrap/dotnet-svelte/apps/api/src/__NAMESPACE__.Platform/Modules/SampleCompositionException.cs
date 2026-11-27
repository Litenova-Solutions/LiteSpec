namespace __NAMESPACE__.Platform.Modules;

/// <summary>
/// The platform composition is broken: a connection string is missing or a
/// required port has no registration. The message names the missing piece.
/// </summary>
public sealed class SampleCompositionException : Exception
{
    /// <summary>Creates the failure with a message for logs, never for clients.</summary>
    /// <param name="message">The missing piece.</param>
    public SampleCompositionException(string message)
        : base(message)
    {
    }
}
