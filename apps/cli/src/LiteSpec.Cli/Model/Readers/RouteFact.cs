namespace LiteSpec.Model.Readers;

/// <summary>One route entry a frontend reader found, as the frontend facts protocol states it.</summary>
/// <param name="File">The route file, relative to the frontend root with forward slashes.</param>
/// <param name="Line">The 1-based line of the route entry.</param>
/// <param name="Address">The route template the entry serves.</param>
/// <param name="Marks">Every screen id the entry's mark names, in source order.</param>
/// <param name="Calls">Every generated-client call the route reaches.</param>
/// <param name="Styles">The style each mark was written in, one per mark, or null when the reader records none: the TypeScript reader reads only typed constants, so its routes carry none.</param>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed record RouteFact(
    string File,
    int Line,
    string Address,
    IReadOnlyList<string> Marks,
    IReadOnlyList<CallFact> Calls,
    IReadOnlyList<MarkStyle>? Styles = null);
