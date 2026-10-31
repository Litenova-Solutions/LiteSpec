namespace LiteSpec.Model;

/// <summary>What the test read found: scenarios and file comments.</summary>
sealed record TestRead(
    IReadOnlyList<ScenarioInfo> Scenarios,
    IReadOnlyList<CodeComment> Comments,
    IReadOnlyDictionary<string, IReadOnlySet<string>> StepCalls)
{
    /// <summary>Gets every test method the test roots declare, in file and line order.</summary>
    public IReadOnlyList<MarkedElement> Elements { get; init; } = [];

    /// <summary>Gets every marked method of no known kind the test roots declare, in file and line order.</summary>
    public IReadOnlyList<StrayMark> StrayMarks { get; init; } = [];

    /// <summary>
    ///     Gets the probe tables of steps classes: per class, each key a step argument names mapped to the route the
    ///     step requests for it, such as a generic probe that asks for an unknown record of any kind.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ProbeRoutes { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    /// <summary>Gets what each steps method requests and asserts, keyed by class and method.</summary>
    public IReadOnlyDictionary<string, StepFacts> StepFacts { get; init; } =
        new Dictionary<string, StepFacts>(StringComparer.Ordinal);
}
