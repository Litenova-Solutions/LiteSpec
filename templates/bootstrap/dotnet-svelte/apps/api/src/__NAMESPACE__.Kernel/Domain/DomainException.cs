namespace __NAMESPACE__.Kernel.Domain;

/// <summary>
/// A refused domain operation. Each subclass carries exactly one refusal code,
/// and no two subclasses return one code.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Creates a refusal with its code and a message for logs, never for clients.</summary>
    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>The refusal code, such as health.check-in-unknown.</summary>
    public virtual string Code { get; }
}
