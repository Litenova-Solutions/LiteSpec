namespace LiteSpec.Model.Readers;

/// <summary>
///     Reads one run across several result formats. Absent when no reader finds a result file.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class CombinedTestRunReader(params ITestRunReader[] readers) : ITestRunReader
{
    /// <summary>Reads and merges every reader's run, in order.</summary>
    public TestRun? Read(ConsumerConfig config)
    {
        TestRun? merged = null;

        foreach (var reader in readers)
        {
            var run = reader.Read(config);

            if (run is null)
            {
                continue;
            }

            merged ??= new TestRun();
            merged.Files.AddRange(run.Files);

            foreach (var (name, outcome) in run.OutcomeByTest)
            {
                merged.OutcomeByTest[name] = outcome;
            }

            foreach (var (method, outcome) in run.OutcomeByMethod)
            {
                merged.MergeOutcome(merged.OutcomeByMethod, method, outcome);
            }

            foreach (var (title, outcome) in run.OutcomeByTitle)
            {
                merged.MergeOutcome(merged.OutcomeByTitle, title, outcome);
            }

            merged.Flaky.UnionWith(run.Flaky);

            merged.Total += run.Total;
            merged.Passed += run.Passed;
            merged.Failed += run.Failed;

            if (run.Date is not null
                && (merged.Date is null || string.CompareOrdinal(run.Date, merged.Date) > 0))
            {
                merged.Date = run.Date;
            }
        }

        return merged;
    }
}
