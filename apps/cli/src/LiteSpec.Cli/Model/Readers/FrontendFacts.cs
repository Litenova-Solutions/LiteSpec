namespace LiteSpec.Model.Readers;

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
    /// <param name="name">The frontend name the failed facts carry.</param>
    /// <param name="reader">The reader name recorded on the failed facts, such as typescript or roslyn.</param>
    /// <param name="failure">Why the reader could not run.</param>
    /// <returns>Empty facts that carry the failure for the named frontend.</returns>
    public static FrontendFacts Failed(string name, string reader, string failure) =>
        new(name, reader, false, failure, [], [], [], []);
}
