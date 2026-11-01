namespace LiteSpec.Model.Readers;

/// <summary>The readers one consumer build reads through.</summary>
/// <param name="Backend">The backend reader.</param>
/// <param name="Frontends">One reader per readable frontend.</param>
/// <param name="Scenarios">The scenario reader.</param>
/// <param name="TestRuns">The test-run reader, across every supported result format.</param>
sealed record StackSet(
    IBackendReader Backend,
    IReadOnlyList<IFrontendReader> Frontends,
    IScenarioReader Scenarios,
    ITestRunReader TestRuns)
{
    /// <summary>Gets reader selection problems, such as a <c>server</c> project with no backend reader.</summary>
    public IReadOnlyList<Finding> Problems { get; init; } = [];
}
