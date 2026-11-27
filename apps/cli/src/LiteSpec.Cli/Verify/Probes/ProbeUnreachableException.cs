namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     The host or the sign-in could not be reached: the failure behind one
///     probe-unreachable finding.
/// </summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal sealed class ProbeUnreachableException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ProbeUnreachableException"/> class.</summary>
    /// <param name="message">What could not be reached and why.</param>
    public ProbeUnreachableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ProbeUnreachableException"/> class with a cause.</summary>
    /// <param name="message">What could not be reached and why.</param>
    /// <param name="inner">The failure that stopped the request.</param>
    public ProbeUnreachableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
