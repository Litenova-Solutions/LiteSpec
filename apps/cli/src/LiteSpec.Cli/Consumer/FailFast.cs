namespace LiteSpec.Consumer;

/// <summary>
///     Carries an exit code out of a check that ends the run immediately, such as
///     a malformed project file. Thrown after the message is already printed.
/// </summary>
sealed class FailFast : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FailFast"/> class.
    /// </summary>
    /// <param name="code">The exit code the command returns.</param>
    public FailFast(int code)
    {
        Code = code;
    }

    /// <summary>Gets the exit code the command returns.</summary>
    public int Code { get; }
}
