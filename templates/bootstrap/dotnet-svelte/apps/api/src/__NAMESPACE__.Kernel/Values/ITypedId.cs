namespace __NAMESPACE__.Kernel.Values;

/// <summary>
/// A strongly typed identifier backed by one version 7 <see cref="Guid"/>.
/// Each aggregate declares its own identifier struct implementing this interface,
/// so one can never be passed where another is meant.
/// </summary>
public interface ITypedId
{
    /// <summary>The identifier value.</summary>
    Guid Value { get; }
}
