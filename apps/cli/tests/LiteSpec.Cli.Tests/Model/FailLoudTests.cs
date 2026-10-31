using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the fail-loud reader side: Worker code is read like every backend layer, and the CI environment variable is visible to the severity decision.</summary>
public sealed class FailLoudTests
{
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Worker_Root_Defaults_To_The_Worker_Project()
    {
        using var temp = Cli.Temp("fail-loud");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), """{"stacks": ["dotnet"]}""");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Shop.Worker"));

        var config = ConsumerConfig.Load(temp.Path);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(temp.Path, "apps", "api", "src", "Shop.Worker")),
            config.WorkerRoot);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Worker_Root_Follows_The_Declared_Path()
    {
        using var temp = Cli.Temp("fail-loud");
        WriteProject(temp.Path);

        var config = ConsumerConfig.Load(temp.Path);

        Assert.Equal(Path.GetFullPath(Path.Combine(temp.Path, "src", "Worker")), config.WorkerRoot);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Job_In_Worker_Is_Discovered()
    {
        using var temp = Cli.Temp("fail-loud");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Worker", "RemindOverdueBorrowersWorker.cs", """
            namespace Library.Worker.Lending.Loans;

            /// <implements>job/lending.remind-overdue-borrowers</implements>
            public sealed class RemindOverdueBorrowersWorker : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);

        var job = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Reactions);

        Assert.Equal("RemindOverdueBorrowersWorker", job.Class);
        Assert.Equal("job", job.Kind);
        Assert.Equal("job/lending.remind-overdue-borrowers", job.RecordId);
    }

    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Continuous_Integration_Is_Set_When_The_CI_Variable_Is_Set()
    {
        using var temp = Cli.Temp("fail-loud");
        WriteProject(temp.Path);
        var previous = Environment.GetEnvironmentVariable("CI");

        try
        {
            Environment.SetEnvironmentVariable("CI", "true");

            Assert.True(ConsumerConfig.Load(temp.Path).IsContinuousIntegration);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CI", previous);
        }
    }

    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Continuous_Integration_Is_Unset_When_The_CI_Variable_Is_Absent()
    {
        using var temp = Cli.Temp("fail-loud");
        WriteProject(temp.Path);
        var previous = Environment.GetEnvironmentVariable("CI");

        try
        {
            Environment.SetEnvironmentVariable("CI", null);

            Assert.False(ConsumerConfig.Load(temp.Path).IsContinuousIntegration);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CI", previous);
        }
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "application": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra", "worker": "src/Worker"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
