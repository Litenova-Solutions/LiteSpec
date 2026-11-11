using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves the .NET reader roots: every declared web and worker host, and a missing root that reports reader-missing.</summary>
public sealed class DotnetRootsTests
{
    /// <summary>WebApi Roots Follow A List Of Hosts.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void WebApiRootsFollowAListOfHosts()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "First.WebApi"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "Second.WebApi"));
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": ["src/First.WebApi", "src/Second.WebApi"]}}}
            """);

        var config = ConsumerConfig.Load(temp.Path);

        Assert.Equal(
            [
                Path.GetFullPath(Path.Combine(temp.Path, "src", "First.WebApi")),
                Path.GetFullPath(Path.Combine(temp.Path, "src", "Second.WebApi")),
            ],
            config.WebApiRoots);
        Assert.Contains(config.WebApiRoots[0], SourceRoots.BackendRoots(config));
        Assert.Contains(config.WebApiRoots[1], SourceRoots.BackendRoots(config));
    }

    /// <summary>Worker Roots Follow A List Of Hosts.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void WorkerRootsFollowAListOfHosts()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "First.Worker"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "Second.Worker"));
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "worker": ["src/First.Worker", "src/Second.Worker"]}}}
            """);

        var config = ConsumerConfig.Load(temp.Path);

        Assert.Equal(
            [
                Path.GetFullPath(Path.Combine(temp.Path, "src", "First.Worker")),
                Path.GetFullPath(Path.Combine(temp.Path, "src", "Second.Worker")),
            ],
            config.WorkerRoots);
        Assert.Contains(config.WorkerRoots[0], SourceRoots.BackendRoots(config));
        Assert.Contains(config.WorkerRoots[1], SourceRoots.BackendRoots(config));
    }

    /// <summary>WebApi Root From One String Still Reads Its Host.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void WebApiRootFromOneStringStillReadsItsHost()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "Only.WebApi"));
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": "src/Only.WebApi"}}}
            """);

        Assert.Equal(
            [Path.GetFullPath(Path.Combine(temp.Path, "src", "Only.WebApi"))],
            ConsumerConfig.Load(temp.Path).WebApiRoots);
    }

    /// <summary>WebApi List Entry With A Glob Reads Every Matching Host.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void WebApiListEntryWithAGlobReadsEveryMatchingHost()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "First.Api"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "Second.Api"));
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": ["src/*.Api"]}}}
            """);

        Assert.Equal(
            [
                Path.GetFullPath(Path.Combine(temp.Path, "src", "First.Api")),
                Path.GetFullPath(Path.Combine(temp.Path, "src", "Second.Api")),
            ],
            ConsumerConfig.Load(temp.Path).WebApiRoots);
    }

    /// <summary>Empty WebApi List Reads No WebApi Host.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void EmptyWebApiListReadsNoWebApiHost()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": []}}}
            """);

        Assert.Empty(ConsumerConfig.Load(temp.Path).WebApiRoots);
    }

    /// <summary>Endpoints In Both WebApi Hosts Are Read.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void EndpointsInBothWebApiHostsAreRead()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": ["src/First.WebApi", "src/Second.WebApi"]}}}
            """);
        WriteSource(temp.Path, "First.WebApi", "BorrowCopyEndpoint.cs", """
            namespace Library.First.Lending.Loans.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyEndpoint
            {
                public void Map(object app) => app.ToString();
            }
            """);
        WriteSource(temp.Path, "Second.WebApi", "ReturnCopyEndpoint.cs", """
            namespace Library.Second.Lending.Loans.ReturnCopy;

            /// <implements>use-case/lending.return-copy</implements>
            public sealed class ReturnCopyEndpoint
            {
                public void Map(object app) => app.ToString();
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Contains(code.Elements, element => element.Class == "BorrowCopyEndpoint");
        Assert.Contains(code.Elements, element => element.Class == "ReturnCopyEndpoint");
        Assert.Equal("First.WebApi", SourceRoots.ProjectOf(ConsumerConfig.Load(temp.Path), Path.Combine(temp.Path, "src", "First.WebApi", "BorrowCopyEndpoint.cs")));
        Assert.Equal("Second.WebApi", SourceRoots.ProjectOf(ConsumerConfig.Load(temp.Path), Path.Combine(temp.Path, "src", "Second.WebApi", "ReturnCopyEndpoint.cs")));
    }

    /// <summary>Jobs In Both Worker Hosts Are Read.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void JobsInBothWorkerHostsAreRead()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "worker": ["src/First.Worker", "src/Second.Worker"]}}}
            """);
        WriteSource(temp.Path, "First.Worker", "NightlyScanJob.cs", """
            namespace Library.First.Lending;

            /// <implements>job/lending.nightly-scan</implements>
            public sealed class NightlyScanJob : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);
        WriteSource(temp.Path, "Second.Worker", "DueReminderJob.cs", """
            namespace Library.Second.Lending;

            /// <implements>job/lending.due-reminder</implements>
            public sealed class DueReminderJob : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);

        var jobs = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Jobs;

        Assert.Equal(2, jobs.Count);
        Assert.Contains(jobs, job => job.RecordId == "job/lending.nightly-scan");
        Assert.Contains(jobs, job => job.RecordId == "job/lending.due-reminder");
    }

    /// <summary>Configured WebApi Host That Does Not Exist Reports Reader Missing.</summary>
    /// <proves>litespec/backend/dotnet/stack#report-a-stack-root-that-matches-no-folder</proves>
    [Fact]
    public void ConfiguredWebApiHostThatDoesNotExistReportsReaderMissing()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "App"));
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": "src/Missing.WebApi"}}}
            """);

        var problem = Assert.Single(
            ConsumerConfig.Load(temp.Path).Problems,
            found => found.Code == "reader-missing" && found.Message.Contains("webapi", StringComparison.Ordinal));

        Assert.Equal("S", problem.Tier);
        Assert.Contains("Missing.WebApi", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>Configured Worker Host In A List That Does Not Exist Reports Reader Missing.</summary>
    /// <proves>litespec/backend/dotnet/stack#report-a-stack-root-that-matches-no-folder</proves>
    [Fact]
    public void ConfiguredWorkerHostInAListThatDoesNotExistReportsReaderMissing()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "App"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "Only.Worker"));
        WriteProject(temp.Path, """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "worker": ["src/Only.Worker", "src/Missing.Worker"]}}}
            """);

        var config = ConsumerConfig.Load(temp.Path);
        var problem = Assert.Single(
            config.Problems,
            found => found.Code == "reader-missing" && found.Message.Contains("worker", StringComparison.Ordinal));

        Assert.Contains("Missing.Worker", problem.Message, StringComparison.Ordinal);
        Assert.Equal(2, config.WorkerRoots.Count);
    }

    /// <summary>Default Modules Root That Matches No Folder Reports Reader Missing.</summary>
    /// <proves>litespec/backend/dotnet/stack#report-a-stack-root-that-matches-no-folder</proves>
    [Fact]
    public void DefaultModulesRootThatMatchesNoFolderReportsReaderMissing()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        WriteProject(temp.Path, """{"stacks": ["dotnet"]}""");

        var problem = Assert.Single(
            ConsumerConfig.Load(temp.Path).Problems,
            found => found.Code == "reader-missing" && found.Message.Contains("modules", StringComparison.Ordinal));

        Assert.Contains("*.Modules", problem.Message, StringComparison.Ordinal);
        Assert.Contains("defaulted to", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>Project Without The Dotnet Stack Reports No Default Root.</summary>
    /// <proves>litespec/backend/dotnet/stack#report-a-stack-root-that-matches-no-folder</proves>
    [Fact]
    public void ProjectWithoutTheDotnetStackReportsNoDefaultRoot()
    {
        using var temp = TestCli.Temp("dotnet-roots");
        WriteProject(temp.Path, """{"shape": "client-only", "stacks": ["svelte"]}""");

        Assert.DoesNotContain(
            ConsumerConfig.Load(temp.Path).Problems,
            found => found.Code == "reader-missing");
    }

    /// <summary>Fixture Project With Two Hosts Reads Both Hosts And Its Traced Aggregate.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void FixtureProjectWithTwoHostsReadsBothHostsAndItsTracedAggregate()
    {
        var fixture = TestCli.Fixture("dotnet-roots");
        var config = ConsumerConfig.Load(fixture);

        Assert.Equal(2, config.WebApiRoots.Count);
        Assert.Equal(2, config.WorkerRoots.Count);

        var code = new DotnetBackendReader().Read(config);

        Assert.Contains(code.Elements, element => element.Class == "BorrowCopyEndpoint");
        Assert.Contains(code.Elements, element => element.Class == "ReturnCopyEndpoint");
        Assert.Equal(2, code.Jobs.Count);
        var loan = Assert.Single(code.Elements, element => element.Kind == CodeKind.AggregateRoot);
        Assert.Equal("Loan", loan.Class);
    }

    static void WriteProject(string root, string json) =>
        File.WriteAllText(Path.Combine(root, ".litespec/project.yaml"), json);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
