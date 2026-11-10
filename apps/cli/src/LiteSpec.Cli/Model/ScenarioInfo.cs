namespace LiteSpec.Model;

/// <summary>One acceptance scenario and what it claims.</summary>
sealed record ScenarioInfo(
    string Name,
    string Class,
    string File,
    int Line,
    IReadOnlyList<string> Claims,
    IReadOnlyList<ScenarioStep> Steps,
    IReadOnlyList<string> Codes,
    IReadOnlyList<int> Statuses,
    IReadOnlySet<string> Identifiers,
    IReadOnlyList<string> Routes,
    IReadOnlyList<string> AssertedCodes,
    bool HasAssertion,
    IReadOnlySet<string> CreatedTypes,
    IReadOnlySet<string> ResolvedServices,
    IReadOnlySet<string> StaticCalls)
{
    /// <summary>
    ///     Gets the hash of the test method's own text, its attributes and body, with line endings normalised,
    ///     so an edit elsewhere in its file leaves it unchanged. The proof ledger records it per scenario.
    /// </summary>
    public string MethodHash { get; init; } = "";
}
