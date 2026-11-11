namespace LiteSpec.Cli.Book;

/// <summary>
///     The closed set of proof states the book writes in <c>proof.state</c> for a
///     scenario or a rule: passing, failing, awaiting, unconfirmed, unproved,
///     indirect and norun. The client types them as one union.
/// </summary>
static class BookProofStates
{
    /// <summary>Gets the <c>passing</c> proof state: the proving test passed.</summary>
    public const string Passing = "passing";

    /// <summary>Gets the <c>failing</c> proof state: the proving test failed.</summary>
    public const string Failing = "failing";

    /// <summary>Gets the <c>awaiting</c> proof state: the proving test has not run yet.</summary>
    public const string Awaiting = "awaiting";

    /// <summary>Gets the <c>unconfirmed</c> proof state: proves links name the claim but no confirmed test proves it.</summary>
    public const string Unconfirmed = "unconfirmed";

    /// <summary>Gets the <c>unproved</c> proof state: no proves link names the claim.</summary>
    public const string Unproved = "unproved";

    /// <summary>Gets the <c>indirect</c> proof state: a confirmed test proves the claim indirectly.</summary>
    public const string Indirect = "indirect";

    /// <summary>Gets the <c>norun</c> proof state: the model holds no test run.</summary>
    public const string Norun = "norun";

    /// <summary>Every proof state the writer emits.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Passing, Failing, Awaiting, Unconfirmed, Unproved, Indirect, Norun,
    };
}
