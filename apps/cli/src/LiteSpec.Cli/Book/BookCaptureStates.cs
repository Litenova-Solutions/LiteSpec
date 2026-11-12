namespace LiteSpec.Cli.Book;

/// <summary>
///     The closed set of screen capture states the book carries in
///     <c>captureState</c> from the model: captured, route-not-captured and
///     no-route. The writer computes none of them; it carries the model's word
///     through, and a record that is no screen carries no capture state. The
///     client types them as one union.
/// </summary>
static class BookCaptureStates
{
    /// <summary>Gets the <c>captured</c> capture state: the galleries hold a capture for the screen's route.</summary>
    public const string Captured = "captured";

    /// <summary>Gets the <c>route-not-captured</c> capture state: the screen has a route the galleries do not capture.</summary>
    public const string RouteNotCaptured = "route-not-captured";

    /// <summary>Gets the <c>no-route</c> capture state: the screen serves no route.</summary>
    public const string NoRoute = "no-route";

    /// <summary>Every capture state the writer carries.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Captured, RouteNotCaptured, NoRoute,
    };
}
