using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Model.Readers.Frontend;
using LiteSpec.Model.Readers.JUnit;

namespace LiteSpec.Model.Readers;

/// <summary>What the backend read found: the code model and the files it could not read.</summary>
/// <param name="Code">What the backend source says.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per file the reader could not read or parse.</param>
sealed record BackendRead(CodeModel Code, IReadOnlyList<Finding> Problems);

/// <summary>What the scenario read found: the test read and the files it could not read.</summary>
/// <param name="Tests">The scenarios, elements and step facts the test roots declare.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per file the reader could not read or parse.</param>
sealed record ScenarioRead(TestRead Tests, IReadOnlyList<Finding> Problems);

/// <summary>Reads the backend source for the configured backend roots.</summary>
interface IBackendReader
{
    /// <summary>Reads handlers, reactions, jobs, events, failure-code literals, comments and endpoint operations.</summary>
    BackendRead Read(ConsumerConfig config);
}

/// <summary>Reads the routes of one frontend.</summary>
interface IFrontendReader
{
    /// <summary>Reads the frontend's route screens and file comments.</summary>
    ScreenRead Read(ConsumerConfig config, CodeModel code);
}

/// <summary>Reads the acceptance scenarios and what they claim.</summary>
interface IScenarioReader
{
    /// <summary>Reads scenarios with their claimed cases, steps and asserted codes, and file comments.</summary>
    ScenarioRead Read(ConsumerConfig config);
}

/// <summary>Reads test outcomes by test name and by fully qualified test name.</summary>
interface ITestRunReader
{
    /// <summary>Reads the run, or null when no result file exists.</summary>
    TestRun? Read(ConsumerConfig config);
}

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
    /// <summary>Reader selection problems, such as a <c>server</c> project with no backend reader.</summary>
    public IReadOnlyList<Finding> Problems { get; init; } = [];
}

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
        public BackendRead Read(ConsumerConfig config) => new(new CodeModel(), []);
    }

    /// <summary>Reads no scenarios: the project names no stack with .NET tests.</summary>
    sealed class EmptyScenarioReader : IScenarioReader
    {
        public ScenarioRead Read(ConsumerConfig config) =>
            new(
                new TestRead([], [], new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)),
                []);
    }
}
