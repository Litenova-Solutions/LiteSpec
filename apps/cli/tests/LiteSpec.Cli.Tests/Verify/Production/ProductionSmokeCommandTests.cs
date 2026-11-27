using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Consumer;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Cli.Verify.Production;

namespace LiteSpec.Cli.Tests.Verify.Production;

/// <summary>Proves the production smoke boots the containers, probes them, and always runs the down command.</summary>
public sealed class ProductionSmokeCommandTests
{
    static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(50);

    /// <summary>A healthy host with healthy paths exits 0, runs up then down, and writes an empty report.</summary>
    [Fact]
    public void HealthySmokeExitsZeroRunsDownAndWritesTheReport()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(5));

        Assert.Equal(0, status);
        Assert.DoesNotContain("[S]", stdout, StringComparison.Ordinal);
        Assert.Equal(
            new[]
            {
                "docker compose -f deploy/compose.yaml --env-file .env.example up -d --build",
                "docker compose -f deploy/compose.yaml --env-file .env.example down -v",
            },
            fake.Commands);
        var report = JsonNode.Parse(File.ReadAllText(ReportPath(consumer.Path)))!.AsObject();
        Assert.Equal(server.BaseUrl, report["baseUrl"]!.GetValue<string>());
        Assert.Empty(report["findings"]!.AsArray());
    }

    /// <summary>A verify block without production is a usage error.</summary>
    [Fact]
    public void MissingProductionBlockIsAUsageError()
    {
        using var consumer = CreateConsumer();
        var fake = new FakeProductionProcessRunner();

        var (status, _, stderr) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(1), Poll, TimeSpan.FromSeconds(1));

        Assert.Equal(2, status);
        Assert.Contains("production", stderr, StringComparison.Ordinal);
        Assert.Empty(fake.Commands);
    }

    /// <summary>A readiness path that never answers 2xx exits 1 with production-readiness-failed and still runs down.</summary>
    [Fact]
    public void FailingHealthReportsProductionUnhealthyAndStillRunsDown()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request =>
            request.Url!.AbsolutePath == "/health/ready" ? (500, "{}") : (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(1), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(1, status);
        Assert.Contains("[production-readiness-failed]", stdout, StringComparison.Ordinal);
        Assert.Equal(2, fake.Commands.Count);
        Assert.EndsWith("down -v", fake.Commands[1], StringComparison.Ordinal);
    }

    /// <summary>A readiness path that fails twice then answers 2xx exits 0: the wait polls.</summary>
    [Fact]
    public void HealthThatRecoversBeforeTheTimeoutReportsNothing()
    {
        using var consumer = CreateConsumer();
        var calls = 0;
        using var server = TestSmokeServer.Start(request =>
            request.Url!.AbsolutePath == "/health/ready"
                ? (Interlocked.Increment(ref calls) <= 2 ? (503, "{}") : (200, "{}"))
                : (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(10), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(0, status);
        Assert.DoesNotContain("[production-readiness-failed]", stdout, StringComparison.Ordinal);
        Assert.True(calls >= 3, "the wait polls until the host answers 2xx");
    }

    /// <summary>A host that gives no answer exits 1 with production-readiness-failed.</summary>
    [Fact]
    public void UnreachableHostReportsProductionUnhealthy()
    {
        using var consumer = CreateConsumer();
        WriteProductionBlock(consumer.Path, "http://127.0.0.1:" + ClosedPort());
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(2), Poll, TimeSpan.FromSeconds(1));

        Assert.Equal(1, status);
        Assert.Contains("[production-readiness-failed]", stdout, StringComparison.Ordinal);
        Assert.Equal(2, fake.Commands.Count);
    }

    /// <summary>A smoke path that answers 5xx exits 1 with path-failed.</summary>
    [Fact]
    public void FailingPathReportsPathFailed()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request =>
            request.Url!.AbsolutePath == "/bad" ? (500, "{}") : (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl, paths: ["/good", "/bad"]);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(1, status);
        Assert.Contains("[path-failed]", stdout, StringComparison.Ordinal);
        Assert.Contains("/bad", stdout, StringComparison.Ordinal);
    }

    /// <summary>A smoke path that never answers exits 1 with path-failed.</summary>
    [Fact]
    public void UnansweredPathReportsPathFailed()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request =>
        {
            if (request.Url!.AbsolutePath == "/slow")
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            return (200, "{}");
        });
        WriteProductionBlock(consumer.Path, server.BaseUrl, paths: ["/slow"]);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromMilliseconds(300));

        Assert.Equal(1, status);
        Assert.Contains("[path-failed]", stdout, StringComparison.Ordinal);
    }

    /// <summary>A smoke path that answers 404 reports nothing: only a 5xx or no answer fails.</summary>
    [Fact]
    public void ClientErrorPathReportsNothing()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request =>
            request.Url!.AbsolutePath == "/missing" ? (404, "{}") : (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl, paths: ["/missing"]);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(0, status);
        Assert.DoesNotContain("[path-failed]", stdout, StringComparison.Ordinal);
    }

    /// <summary>Each smoke path is requested with the forwarded https scheme.</summary>
    [Fact]
    public void SmokePathsCarryTheForwardedHttpsScheme()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl, paths: ["/api", "/auth"]);
        var fake = new FakeProductionProcessRunner();

        var (status, _, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(0, status);
        Assert.Equal("https", server.ForwardedProto["/api"]);
        Assert.Equal("https", server.ForwardedProto["/auth"]);
    }

    /// <summary>Each stub adapter in the readiness body exits 1 with its own stub-adapter-shown finding.</summary>
    [Fact]
    public void StubAdaptersEachReportStubActive()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, """{"stubAdapters":["mail","queue"]}"""));
        WriteProductionBlock(consumer.Path, server.BaseUrl, paths: []);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(1, status);
        Assert.Equal(2, CountOccurrences(stdout, "[stub-adapter-shown]"));
        Assert.Contains("mail", stdout, StringComparison.Ordinal);
        Assert.Contains("queue", stdout, StringComparison.Ordinal);
    }

    /// <summary>An empty stub adapter list reports nothing.</summary>
    [Fact]
    public void EmptyStubAdaptersReportNothing()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, """{"stubAdapters":[]}"""));
        WriteProductionBlock(consumer.Path, server.BaseUrl, paths: []);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(0, status);
        Assert.DoesNotContain("[stub-adapter-shown]", stdout, StringComparison.Ordinal);
    }

    /// <summary>A readiness body that is not JSON reports nothing.</summary>
    [Fact]
    public void NonJsonHealthBodyReportsNothing()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, "ok"));
        WriteProductionBlock(consumer.Path, server.BaseUrl, paths: []);
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(0, status);
        Assert.DoesNotContain("[stub-adapter-shown]", stdout, StringComparison.Ordinal);
    }

    /// <summary>The migrate command runs between up and down.</summary>
    [Fact]
    public void MigrateRunsBetweenUpAndDown()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl, migrate: "docker compose -f deploy/compose.yaml run --rm migrate");
        var fake = new FakeProductionProcessRunner();

        var (status, _, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(0, status);
        Assert.Equal(
            new[]
            {
                "docker compose -f deploy/compose.yaml --env-file .env.example up -d --build",
                "docker compose -f deploy/compose.yaml run --rm migrate",
                "docker compose -f deploy/compose.yaml --env-file .env.example down -v",
            },
            fake.Commands);
    }

    /// <summary>A failing migrate exits 1 with production-readiness-failed, never probes the host, and still runs down.</summary>
    [Fact]
    public void FailedMigrateReportsProductionUnhealthyWithoutProbing()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl, migrate: "docker compose -f deploy/compose.yaml run --rm migrate");
        var fake = new FakeProductionProcessRunner
        {
            Handler = line => line.Contains("run --rm migrate", StringComparison.Ordinal)
                ? new ProductionProcessResult(1, "the database is down")
                : new ProductionProcessResult(0, string.Empty),
        };

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(1, status);
        Assert.Contains("[production-readiness-failed]", stdout, StringComparison.Ordinal);
        Assert.Equal(0, server.Hits);
        Assert.Equal(3, fake.Commands.Count);
        Assert.EndsWith("down -v", fake.Commands[2], StringComparison.Ordinal);
    }

    /// <summary>A failing up exits 1 with production-readiness-failed, never probes the host, and still runs down.</summary>
    [Fact]
    public void FailedUpReportsProductionUnhealthyWithoutProbing()
    {
        using var consumer = CreateConsumer();
        using var server = TestSmokeServer.Start(request => (200, "{}"));
        WriteProductionBlock(consumer.Path, server.BaseUrl);
        var fake = new FakeProductionProcessRunner
        {
            Handler = line => line.Contains("up -d --build", StringComparison.Ordinal)
                ? new ProductionProcessResult(1, "the daemon is down")
                : new ProductionProcessResult(0, string.Empty),
        };

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--root", consumer.Path], TimeSpan.FromSeconds(5), Poll, TimeSpan.FromSeconds(2));

        Assert.Equal(1, status);
        Assert.Contains("[production-readiness-failed]", stdout, StringComparison.Ordinal);
        Assert.Equal(0, server.Hits);
        Assert.Equal(2, fake.Commands.Count);
        Assert.EndsWith("down -v", fake.Commands[1], StringComparison.Ordinal);
    }

    /// <summary>--help prints the usage text and exits 0.</summary>
    [Fact]
    public void HelpPrintsUsage()
    {
        using var consumer = CreateConsumer();
        var fake = new FakeProductionProcessRunner();

        var (status, stdout, _) = RunSmoke(consumer.Path, fake, ["--help"], TimeSpan.FromSeconds(1), Poll, TimeSpan.FromSeconds(1));

        Assert.Equal(0, status);
        Assert.Contains("production-smoke", stdout, StringComparison.Ordinal);
    }

    /// <summary>An unknown option is a usage error.</summary>
    [Fact]
    public void UnknownOptionIsAUsageError()
    {
        using var consumer = CreateConsumer();
        var fake = new FakeProductionProcessRunner();

        var (status, _, _) = RunSmoke(consumer.Path, fake, ["--bogus"], TimeSpan.FromSeconds(1), Poll, TimeSpan.FromSeconds(1));

        Assert.Equal(2, status);
        Assert.Empty(fake.Commands);
    }

    /// <summary>Builds a consumer root with only a project file: the smoke reads the verify block, never the records.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder CreateConsumer()
    {
        var folder = TestCli.Temp("production-smoke");
        var project = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["project"] = new JsonObject { ["name"] = "fixture" },
            ["size"] = "small",
            ["stacks"] = new JsonArray(["dotnet"]),
            ["layers"] = new JsonObject
            {
                ["intent"] = "litespec",
                ["behavior"] = "litespec",
                ["implementation"] = "litespec",
                ["proof"] = "litespec",
            },
            ["reviewedLiteSpecVersion"] = ConsumerFixture.ManifestVersion,
            ["paths"] = new JsonObject
            {
                ["docs"] = ".litespec",
                ["behaviorDocs"] = ".litespec/behavior",
            },
            ["frontends"] = new JsonArray(),
            ["selectedExtensions"] = new JsonArray(),
        };
        File.WriteAllText(
            Path.Combine(folder.Path, ".litespec", "project.yaml"),
            project.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        return folder;
    }

    /// <summary>Runs the command with the writers and timeouts the case names.</summary>
    /// <param name="root">The consumer root the command reads.</param>
    /// <param name="fake">The process runner that records the container commands.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="healthTimeout">How long the health wait polls.</param>
    /// <param name="pollInterval">How long the health wait rests between polls.</param>
    /// <param name="requestTimeout">How long one probe request may take.</param>
    /// <returns>The exit code with both output streams.</returns>
    static (int Status, string Stdout, string Stderr) RunSmoke(
        string root,
        FakeProductionProcessRunner fake,
        string[] args,
        TimeSpan healthTimeout,
        TimeSpan pollInterval,
        TimeSpan requestTimeout)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var status = ProductionSmokeCommand.Run(output, error, root, args, fake, healthTimeout, pollInterval, requestTimeout);
        return (status, output.ToString(), error.ToString());
    }

    /// <summary>Writes the verify production block into the fixture project file.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="baseUrl">The production address the smoke probes.</param>
    /// <param name="migrate">The migrate command, or null for none.</param>
    /// <param name="paths">The smoke paths, or null for the default pair.</param>
    static void WriteProductionBlock(string root, string baseUrl, string? migrate = null, string[]? paths = null)
    {
        var file = Path.Combine(root, ".litespec", "project.yaml");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        var production = new JsonObject
        {
            ["compose"] = "deploy/compose.yaml",
            ["envFile"] = ".env.example",
            ["baseUrl"] = baseUrl,
            ["health"] = "/health/ready",
        };

        if (migrate is not null)
        {
            production["migrate"] = migrate;
        }

        production["paths"] = new JsonArray([.. (paths ?? ["/api", "/auth"]).Select(path => (JsonNode?)path)]);
        project["verify"] = new JsonObject { ["production"] = production };
        File.WriteAllText(file, project.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    /// <summary>The report file the command writes for one consumer.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The report path.</returns>
    static string ReportPath(string root) =>
        Path.Combine(root, ".litespec", "verify", "production-smoke.json");

    /// <summary>Counts the occurrences of one value in a text.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="value">The value to count.</param>
    /// <returns>How many times the value occurs.</returns>
    static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    /// <summary>Finds a local port with nothing listening on it.</summary>
    /// <returns>The closed port number.</returns>
    static int ClosedPort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }
}
