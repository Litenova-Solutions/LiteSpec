using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Model.Readers.Frontend;
using LiteSpec.Model.Readers.JUnit;

namespace LiteSpec.Model.Readers;

/// <summary>
///     Picks the readers for a consumer: the .NET backend and scenario readers when the
///     project names a stack with .NET sources (dotnet, blazor, or android), one frontend
///     reader per declared frontend from its platform (the TypeScript reader through
///     Node for Svelte and React, the .NET screen reader for Blazor and Android), and a
///     test-run reader across every supported result format.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class StackReaders
{
    /// <summary>Picks the readers for the consumer.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The reader set chosen for the consumer, with any selection problems.</returns>
    public static StackSet For(ConsumerConfig config)
    {
        var dotnet = config.Stacks.Contains("dotnet", StringComparer.Ordinal)
            || config.Stacks.Contains("blazor", StringComparer.Ordinal)
            || config.Stacks.Contains("android", StringComparer.Ordinal);

        IBackendReader backend = dotnet ? new DotnetBackendReader() : new EmptyBackendReader();
        IScenarioReader scenarios = dotnet ? new DotnetScenarioReader() : new EmptyScenarioReader();

        var frontends = new List<IFrontendReader>();
        var webRun = new Lazy<IReadOnlyList<FrontendFacts>>(() => NodeFrontendReader.Read(config));

        foreach (var frontend in config.Frontends)
        {
            if (frontend.Platform is "svelte" or "react")
            {
                frontends.Add(new TypeScriptFrontendReader(frontend, webRun));
            }
            else if (frontend.Platform is "blazor" or "android")
            {
                frontends.Add(new DotnetFrontendReader(frontend));
            }
        }

        ITestRunReader runs = new CombinedTestRunReader(new TrxTestRunReader(), new JUnitRunReader());

        var problems = new List<Finding>();

        if (!dotnet
            && string.Equals(config.Shape, "server", StringComparison.Ordinal))
        {
            problems.Add(new Finding(
                "reader-missing",
                $"Project shape is 'server' but no backend reader is selected for stacks '{string.Join(", ", config.Stacks)}'; the backend source was not read.",
                Path.Combine(config.Root, "litespec.project.json"),
                1,
                null)
            {
                Reader = Finding.RoslynReader,
                ReaderIsQualified = true,
            });
        }

        return new StackSet(backend, frontends, scenarios, runs) { Problems = problems };
    }

    /// <summary>Reads no backend: the project names no stack with .NET sources.</summary>
    sealed class EmptyBackendReader : IBackendReader
    {
        /// <summary>
        ///     Returns an empty backend read: an empty code model and no problems, because the project names no
        ///     stack with .NET sources.
        /// </summary>
        /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
        /// <returns>An empty backend read: an empty code model and no problems.</returns>
        public BackendRead Read(ConsumerConfig config) => new(new CodeModel(), []);
    }

    /// <summary>Reads no scenarios: the project names no stack with .NET tests.</summary>
    sealed class EmptyScenarioReader : IScenarioReader
    {
        /// <summary>
        ///     Returns an empty scenario read: no tests and no problems, because the project names no stack with
        ///     .NET tests.
        /// </summary>
        /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
        /// <returns>An empty scenario read: no tests and no problems.</returns>
        public ScenarioRead Read(ConsumerConfig config) =>
            new(
                new TestRead([], [], new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)),
                []);
    }
}

/// <summary>Reads the backend source for the configured backend roots.</summary>
interface IBackendReader
{
    /// <summary>Reads handlers, reactions, jobs, events, failure-code literals, comments and endpoint operations.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The backend read: the code model and the files it could not read.</returns>
    BackendRead Read(ConsumerConfig config);
}

/// <summary>Reads the routes of one frontend.</summary>
interface IFrontendReader
{
    /// <summary>Reads the frontend's route screens and file comments.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="code">The code model the screens are checked against.</param>
    /// <returns>The screen read of the frontend.</returns>
    ScreenRead Read(ConsumerConfig config, CodeModel code);
}

/// <summary>Reads the acceptance scenarios and what they claim.</summary>
interface IScenarioReader
{
    /// <summary>Reads scenarios with their claimed cases, steps and asserted codes, and file comments.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The scenario read: the scenarios and the files it could not read.</returns>
    ScenarioRead Read(ConsumerConfig config);
}

/// <summary>Reads test outcomes by test name and by fully qualified test name.</summary>
interface ITestRunReader
{
    /// <summary>Reads the run, or null when no result file exists.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The test run, or null when no result file exists.</returns>
    TestRun? Read(ConsumerConfig config);
}
