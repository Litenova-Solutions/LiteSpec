namespace LiteSpec.Cli.Book;

/// <summary>
///     The closed set of finding check levels the book reads in <c>tier</c> and
///     <c>level</c>: G, S and A, in rank order. The client types them as one union.
/// </summary>
static class BookCheckLevels
{
    /// <summary>Gets the <c>G</c> check level.</summary>
    public const string G = "G";

    /// <summary>Gets the <c>S</c> check level.</summary>
    public const string S = "S";

    /// <summary>Gets the <c>A</c> check level.</summary>
    public const string A = "A";

    /// <summary>Every check level the writer reads, in rank order.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        G, S, A,
    };
}
