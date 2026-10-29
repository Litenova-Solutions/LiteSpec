namespace LiteSpec.Model;

/// <summary>One step call inside a scenario.</summary>
sealed record ScenarioStep(string Phase, string Receiver, string? ReceiverType, string Method)
{
    /// <summary>The string literals the step passes, in order, such as the record kind a probe step asks for.</summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];
}

/// <summary>
///     What one steps method does through the helpers of its class: the request routes it names and the HTTP
///     statuses and refusal codes it asserts, so a shared then step such as <c>nothing_is_found</c> counts as the
///     assertion it makes.
/// </summary>
sealed record StepFacts(IReadOnlySet<string> Routes, IReadOnlySet<int> Statuses, IReadOnlySet<string> Codes);

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

/// <summary>What the test read found: scenarios and file comments.</summary>
sealed record TestRead(
    IReadOnlyList<ScenarioInfo> Scenarios,
    IReadOnlyList<CodeComment> Comments,
    IReadOnlyDictionary<string, IReadOnlySet<string>> StepCalls)
{
    /// <summary>Every test method the test roots declare, in file and line order.</summary>
    public IReadOnlyList<MarkedElement> Elements { get; init; } = [];

    /// <summary>Every marked method of no known kind the test roots declare, in file and line order.</summary>
    public IReadOnlyList<StrayMark> StrayMarks { get; init; } = [];

    /// <summary>
    ///     The probe tables of steps classes: per class, each key a step argument names mapped to the route the
    ///     step requests for it, such as a generic probe that asks for an unknown record of any kind.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ProbeRoutes { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    /// <summary>What each steps method requests and asserts, keyed by class and method.</summary>
    public IReadOnlyDictionary<string, StepFacts> StepFacts { get; init; } =
        new Dictionary<string, StepFacts>(StringComparer.Ordinal);
}
