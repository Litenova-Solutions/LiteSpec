namespace Claimwick.Kernel;

/// <summary>The base of every business rejection in Claimwick.</summary>
public abstract class DomainException : Exception
{
    /// <summary>Creates a rejection with its message.</summary>
    protected DomainException(string message)
        : base(message)
    {
    }

    /// <summary>The stable failure code.</summary>
    public abstract string Code { get; }
}
