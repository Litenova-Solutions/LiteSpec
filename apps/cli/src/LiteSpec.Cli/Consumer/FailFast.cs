namespace LiteSpec.Consumer;

/// <summary>
///     Carries an exit code and its message out of a check that ends the consumer
///     pass immediately, such as a malformed project file. Thrown before anything
///     is printed; the caller prints the message and returns the code.
/// </summary>
sealed class FailFast : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FailFast"/> class.
    /// </summary>
    /// <param name="code">The exit code the command returns.</param>
    /// <param name="message">The message the command prints.</param>
    public FailFast(int code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Gets the exit code the command returns.</summary>
    public int Code { get; }
}
