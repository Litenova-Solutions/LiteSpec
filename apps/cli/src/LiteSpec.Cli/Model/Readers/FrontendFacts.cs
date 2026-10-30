namespace LiteSpec.Model.Readers;

/// <summary>One generated-client call a route reaches: the operation name as the client exports it.</summary>
/// <param name="Operation">The operation name, as the client exports it.</param>
/// <param name="File">The file holding the call, relative to the frontend root with forward slashes.</param>
/// <param name="Line">The 1-based line of the call.</param>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed record CallFact(string Operation, string File, int Line);

/// <summary>One route entry a frontend reader found, as the frontend facts protocol states it.</summary>
/// <param name="File">The route file, relative to the frontend root with forward slashes.</param>
/// <param name="Line">The 1-based line of the route entry.</param>
/// <param name="Address">The route template the entry serves.</param>
/// <param name="Marks">Every screen id the entry's mark names, in source order.</param>
/// <param name="Calls">Every generated-client call the route reaches.</param>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed record RouteFact(
    string File,
    int Line,
    string Address,
    IReadOnlyList<string> Marks,
    IReadOnlyList<CallFact> Calls);

/// <summary>One use-case module folder a frontend reader found.</summary>
/// <param name="Folder">The folder, relative to the frontend root with forward slashes.</param>
/// <param name="File">The file holding its mark, relative to the frontend root.</param>
/// <param name="Line">The 1-based line of the mark.</param>
/// <param name="Marks">Every use-case id its <c>litespec</c> constants name.</param>
/// <follows>litespec/frontend/web/structure#mark-each-use-case-module-with-its-use-case</follows>
sealed record ModuleFact(
    string Folder,
    string File,
    int Line,
    IReadOnlyList<string> Marks);

/// <summary>One browser test a frontend reader found.</summary>
/// <param name="File">The test file, relative to the frontend root with forward slashes.</param>
/// <param name="Line">The 1-based line of the test.</param>
/// <param name="Title">The test title.</param>
/// <param name="Proves">Every proof target its <c>proves</c> annotations name.</param>
/// <param name="Journey">The journey its <c>journey</c> annotation names, or null.</param>
/// <param name="Gotos">Every <c>page.goto</c> target in the test body.</param>
/// <param name="Codes">Every refusal code its assertions hold.</param>
/// <param name="Texts">Every other asserted text.</param>
/// <follows>litespec/frontend/testing#mark-the-scenario-a-browser-test-proves</follows>
sealed record BrowserTestFact(
    string File,
    int Line,
    string Title,
    IReadOnlyList<string> Proves,
    string? Journey,
    IReadOnlyList<string> Gotos,
    IReadOnlyList<string> Codes,
    IReadOnlyList<string> Texts);

/// <summary>One file a frontend reader could not parse.</summary>
/// <param name="File">The file, relative to the frontend root.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Message">What the reader could not parse.</param>
sealed record FrontendDiagnostic(string File, int Line, string Message);

/// <summary>
///     The facts one frontend reader read for one frontend. When <see cref="Failure" />
///     is set the reader could not run and every fact list is empty.
/// </summary>
/// <param name="Name">The frontend name.</param>
/// <param name="Reader">The reader that produced these facts: <c>typescript</c> or <c>roslyn</c>.</param>
/// <param name="Qualified">Whether that reader is qualified for this frontend.</param>
/// <param name="Failure">Why the reader could not run, or null when it ran.</param>
/// <param name="Modules">The use-case module folders.</param>
/// <param name="Routes">The route entries.</param>
/// <param name="Tests">The browser tests.</param>
/// <param name="Diagnostics">The files the reader could not parse.</param>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed record FrontendFacts(
    string Name,
    string Reader,
    bool Qualified,
    string? Failure,
    IReadOnlyList<ModuleFact> Modules,
    IReadOnlyList<RouteFact> Routes,
    IReadOnlyList<BrowserTestFact> Tests,
    IReadOnlyList<FrontendDiagnostic> Diagnostics)
{
    /// <summary>Empty facts for a frontend whose reader could not run.</summary>
    public static FrontendFacts Failed(string name, string reader, string failure) =>
        new(name, reader, false, failure, [], [], [], []);
}

/// <summary>Address matching and call mapping shared by every frontend reader.</summary>
/// <follows>litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case</follows>
static class FrontendFactsRules
{
    /// <summary>
    ///     Whether a <c>page.goto</c> target reaches a screen at the address: the
    ///     segments are equal, and a <c>[param]</c> or <c>{}</c> segment of either
    ///     side matches any one segment of the other.
    /// </summary>
    public static bool AddressMatches(string address, string target)
    {
        var left = Segments(address);
        var right = Segments(target);

        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (IsParameter(left[index]) || IsParameter(right[index]))
            {
                continue;
            }

            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<string> Segments(string address) =>
        [.. address.Split('/', StringSplitOptions.RemoveEmptyEntries)];

    private static bool IsParameter(string segment) =>
        segment == "{}"
        || (segment.StartsWith("[", StringComparison.Ordinal) && segment.EndsWith("]", StringComparison.Ordinal))
        || (segment.StartsWith("{", StringComparison.Ordinal) && segment.EndsWith("}", StringComparison.Ordinal))
        || segment.StartsWith("$", StringComparison.Ordinal)
        || segment.StartsWith(":", StringComparison.Ordinal);
}
