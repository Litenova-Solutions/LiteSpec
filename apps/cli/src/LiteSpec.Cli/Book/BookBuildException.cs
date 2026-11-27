namespace LiteSpec.Cli.Book;

/// <summary>
///     A book build that failed: the message the author reads and the exit code a one-shot
///     run returns. A live rebuild shows the message and keeps the last good book.
/// </summary>
/// <param name="message">The failure message.</param>
/// <param name="code">The exit code of a one-shot run.</param>
sealed class BookBuildException(string message, int code) : Exception(message)
{
    /// <summary>Gets the exit code of a one-shot run.</summary>
    public int Code { get; } = code;
}
