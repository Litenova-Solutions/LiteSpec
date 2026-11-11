using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Model.Readers.Dotnet.Scenarios;
using LiteSpec.Cli.Model.Readers.Frontend;
using LiteSpec.Cli.Model.Readers.JUnit;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Readers;

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
        // The Node reader needs no code model, so it starts now and runs beside the .NET reads.
        var webRun = config.Frontends.Any(frontend => frontend.Platform is "svelte" or "react")
            ? Task.Run(() => NodeFrontendReader.Read(config))
            : Task.FromResult<IReadOnlyList<FrontendFacts>>([]);

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
                ConsumerRoot.ProjectFile(config.Root),
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
        public BackendRead Read(ConsumerConfig config) => new(new CodeFacts(), []);
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
