namespace LiteSpec.Model;

/// <summary>
///     Phase timings for profiling the builder on a large consumer. They print to
///     standard error only when <c>LITESPEC_TIMING</c> is set, so a normal run
///     writes nothing beside its findings.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class Timing
{
    /// <summary>Whether timing output is on, read once from the LITESPEC_TIMING environment variable.</summary>
    static readonly bool enabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LITESPEC_TIMING"));

    /// <summary>Writes one timing line when timing is on.</summary>
    /// <param name="line">The formatted timing line to write to standard error.</param>
    public static void Report(string line)
    {
        if (enabled)
        {
            Console.Error.WriteLine(line);
        }
    }
}
