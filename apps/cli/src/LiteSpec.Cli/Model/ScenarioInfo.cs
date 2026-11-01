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
    IReadOnlySet<string> StaticCalls);
