using System.Net;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Cli.Verify.Probes;

namespace LiteSpec.Cli.Tests.Verify.Probes;

/// <summary>Proves <c>litespec verify</c> runs scenario probes against the running product.</summary>
public sealed class VerifyCommandTests
{
    /// <summary>An anonymous probe passes when the host answers the expected status and fields.</summary>
    [Fact]
    public void ProbePassesWhenStatusAndFieldsMatch()
    {
        using var temp = ModelFixture.Prepare("small");
        AddAnonymousProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-succeeds", 200, "/state", "lent");

        using var server = TestProbeServer.Start((request, body) => (200, """{"state":"lent","loanId":"l1"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        var report = JsonNode.Parse(File.ReadAllText(ReportPath(temp.Path)))!.AsObject();
        Assert.Empty(report["findings"]!.AsArray());
    }

    /// <summary>A probe fails when the host answers another status than the probe expects.</summary>
    [Fact]
    public void ProbeFailsWhenStatusDiffers()
    {
        using var temp = ModelFixture.Prepare("small");
        AddAnonymousProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-succeeds", 200, "/state", "lent");

        using var server = TestProbeServer.Start((request, body) => (404, """{"state":"lent"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[probe-failed]", result.Stdout);
        var report = JsonNode.Parse(File.ReadAllText(ReportPath(temp.Path)))!.AsObject();
        Assert.Equal("probe-failed", report["findings"]![0]!["code"]!.GetValue<string>());
    }

    /// <summary>A probe fails when a field holds another value than the probe expects.</summary>
    [Fact]
    public void ProbeFailsWhenFieldDiffers()
    {
        using var temp = ModelFixture.Prepare("small");
        AddAnonymousProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-succeeds", 200, "/state", "lent");

        using var server = TestProbeServer.Start((request, body) => (200, """{"state":"lost"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[probe-failed]", result.Stdout);
    }

    /// <summary>A probe passes when the problem code equals the expected refusal.</summary>
    [Fact]
    public void ProbePassesWhenRefusalCodeMatches()
    {
        using var temp = ModelFixture.Prepare("small");
        AddRefusalProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-refused", 409, "lending.copy-not-available");

        using var server = TestProbeServer.Start((request, body) => (409, """{"code":"lending.copy-not-available","title":"Copy unavailable"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
    }

    /// <summary>A probe fails when the problem carries another code and type than the expected refusal.</summary>
    [Fact]
    public void ProbeFailsWhenRefusalDiffers()
    {
        using var temp = ModelFixture.Prepare("small");
        AddRefusalProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-refused", 409, "lending.copy-not-available");

        using var server = TestProbeServer.Start((request, body) => (409, """{"code":"lending.other","title":"Other"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[probe-failed]", result.Stdout);
    }

    /// <summary>A probe is unreachable when no host answers the base address.</summary>
    [Fact]
    public void ProbeReportsUnreachableWhenHostRefuses()
    {
        using var temp = ModelFixture.Prepare("small");
        AddAnonymousProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-succeeds", 200, "/state", "lent");

        var dead = $"http://127.0.0.1:{FreePort()}";
        var result = RunVerify(temp.Path, dead);

        Assert.Equal(1, result.Status);
        Assert.Contains("[probe-unreachable]", result.Stdout);
    }

    /// <summary>A probe naming no cast person is a usage error reported before anything is sent.</summary>
    [Fact]
    public void ProbeReportsUsageErrorWhenAsNamesNoPerson()
    {
        using var temp = ModelFixture.Prepare("small");
        AddNamedProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-succeeds", "person/unknown", 200);

        var hits = 0;
        using var server = TestProbeServer.Start((request, body) => { hits++; return (200, """{"state":"lent"}"""); });
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(2, result.Status);
        Assert.Equal(0, hits);
        Assert.Contains("person/unknown", result.Stderr);
    }

    /// <summary>With a use-case filter only the probes of that use case run.</summary>
    [Fact]
    public void ProbeRunsOnlySelectedUseCase()
    {
        using var temp = ModelFixture.Prepare("small");
        AddAnonymousProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.borrowing-succeeds", 200, "/state", "lent");
        AddAnonymousProbe(temp.Path, "extend-loan", "scenario/extend-loan.extending-succeeds", 200, "/state", "lent");

        using var server = TestProbeServer.Start((request, body) => (200, """{"state":"lent"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl, "use-case/lending.borrow-copy");

        Assert.Equal(0, result.Status);
        var probes = JsonNode.Parse(File.ReadAllText(ReportPath(temp.Path)))!["probes"]!.AsArray();
        var probe = Assert.Single(probes);
        Assert.Equal("use-case/lending.borrow-copy", probe!["useCase"]!.GetValue<string>());
    }

    /// <summary>A probe resolves path and body placeholders from the verify values.</summary>
    [Fact]
    public void ProbeResolvesValuesInPathAndBody()
    {
        using var temp = ModelFixture.Prepare("small");
        AddValues(temp.Path, "loan", "l1");
        AddValuesProbe(temp.Path);

        string seenPath = string.Empty;
        string seenBody = string.Empty;
        using var server = TestProbeServer.Start((request, body) =>
        {
            seenPath = request.Url!.AbsolutePath;
            seenBody = body ?? string.Empty;
            return (200, """{"state":"lent"}""");
        });
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.Equal("/api/loans/l1", seenPath);
        Assert.Contains("l1", seenBody);
    }

    /// <summary>The values command output fills a probe path and overrides a fixed value of the same name.</summary>
    [Fact]
    public void ValuesFromOverlaysTheFixedValues()
    {
        using var temp = ModelFixture.Prepare("small");
        AddValuesBlock(temp.Path, "    loan: l1", ValuesCommand("process.stdout.write(JSON.stringify({loan:'l2'}))"));
        AddValuesProbe(temp.Path);

        string seenPath = string.Empty;
        using var server = TestProbeServer.Start((request, body) =>
        {
            seenPath = request.Url!.AbsolutePath;
            return (200, """{"state":"lent"}""");
        });
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.Equal("/api/loans/l2", seenPath);
    }

    /// <summary>A values command that fails reports the probe as unreachable and sends nothing.</summary>
    [Fact]
    public void FailingValuesCommandReportsUnreachable()
    {
        using var temp = ModelFixture.Prepare("small");
        AddValuesBlock(temp.Path, "    loan: l1", ValuesCommand("process.exit(3)"));
        AddValuesProbe(temp.Path);

        using var server = TestProbeServer.Start((request, body) => (200, """{"state":"lent"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[probe-unreachable]", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(server.Paths);
    }

    /// <summary>A values command that prints no JSON object reports the probe as unreachable.</summary>
    [Fact]
    public void ValuesCommandWithoutJsonObjectReportsUnreachable()
    {
        using var temp = ModelFixture.Prepare("small");
        AddValuesBlock(temp.Path, "    loan: l1", ValuesCommand("process.stdout.write('[1]')"));
        AddValuesProbe(temp.Path);

        using var server = TestProbeServer.Start((request, body) => (200, """{"state":"lent"}"""));
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[probe-unreachable]", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A node command line that runs the script, quoted for both the Windows shell and sh.</summary>
    /// <param name="script">The JavaScript to run.</param>
    /// <returns>The command line.</returns>
    static string ValuesCommand(string script) => $"node -e \"{script}\"";

    /// <summary>Adds the verify block with fixed values and a values command.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="values">The fixed value lines, indented under values.</param>
    /// <param name="command">The values command.</param>
    static void AddValuesBlock(string root, string values, string command)
    {
        var project = Path.Combine(root, ".litespec", "project.yaml");
        var quoted = command.Replace("'", "''", StringComparison.Ordinal);
        File.AppendAllText(project, $"{Environment.NewLine}verify:{Environment.NewLine}  values:{Environment.NewLine}{values}{Environment.NewLine}  valuesFrom: '{quoted}'{Environment.NewLine}");
    }

    /// <summary>Two probes of one person sign that person in once.</summary>
    [Fact]
    public void ProbeSignsPersonInOnce()
    {
        using var temp = ModelFixture.Prepare("small");
        AddCast(temp.Path);
        AddSignIn(temp.Path);
        AddNamedProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.first-borrow", "person/ava", 200);
        AddNamedProbe(temp.Path, "extend-loan", "scenario/extend-loan.first-extend", "person/ava", 200);

        using var server = TestProbeServer.Start((request, body) =>
        {
            if (request.Url!.AbsolutePath.StartsWith("/auth/", StringComparison.Ordinal))
            {
                return (200, """{"signedIn":true}""");
            }

            return (200, """{"state":"lent"}""");
        });
        var result = RunVerify(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        var signIns = server.Paths.Count(path => path.StartsWith("/auth/", StringComparison.Ordinal));
        Assert.Equal(1, signIns);
    }

    /// <summary>Runs the verify command and captures its output without the command dispatch.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="baseUrl">The base address.</param>
    /// <param name="useCase">The use-case filter, or null.</param>
    /// <returns>The exit code and the two output streams.</returns>
    static CliResult RunVerify(string root, string baseUrl, string? useCase = null)
    {
        string[] args = useCase is null
            ? ["verify", "--root", root, "--base-url", baseUrl]
            : ["verify", "--root", root, "--base-url", baseUrl, "--use-case", useCase];

        return TestCli.Run(args);
    }

    /// <summary>Adds one anonymous probe scenario to the use case.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="folder">The use-case folder, such as borrow-copy.</param>
    /// <param name="scenario">The scenario id.</param>
    /// <param name="status">The expected status.</param>
    /// <param name="path">The field path.</param>
    /// <param name="equals">The expected value.</param>
    static void AddAnonymousProbe(string root, string folder, string scenario, int status, string path, string equals)
    {
        var page = UseCaseFile(root, folder);
        var result = folder == "borrow-copy" ? "result/borrow-copy.borrowed" : "result/extend-loan.extended";
        File.AppendAllText(page, $"""
            {Environment.NewLine}scenarios:
            - id: {scenario}
              title: Added Probe
              given: A reader holds a copy.
              when: The reader borrows the copy.
              then: The copy is lent.
              result: {result}
              probe:
                request:
                  method: GET
                  path: /api/loans
                expect:
                  status: {status}
                  fields:
                  - path: {path}
                    equals: {equals}
            """);
    }

    /// <summary>Adds one refusal probe scenario to the use case.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="folder">The use-case folder.</param>
    /// <param name="scenario">The scenario id.</param>
    /// <param name="status">The expected status.</param>
    /// <param name="refusal">The expected refusal.</param>
    static void AddRefusalProbe(string root, string folder, string scenario, int status, string refusal)
    {
        var page = UseCaseFile(root, folder);
        var result = folder == "borrow-copy" ? "result/borrow-copy.copy-unavailable" : "result/extend-loan.extension-refused";
        File.AppendAllText(page, $"""
            {Environment.NewLine}scenarios:
            - id: {scenario}
              title: Added Refusal Probe
              given: A reader holds a copy.
              when: The reader borrows the copy.
              then: The copy is unavailable.
              result: {result}
              probe:
                request:
                  method: GET
                  path: /api/loans
                expect:
                  status: {status}
                  refusal: {refusal}
            """);
    }

    /// <summary>Adds one named probe scenario to the use case.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="folder">The use-case folder.</param>
    /// <param name="scenario">The scenario id.</param>
    /// <param name="person">The cast person id.</param>
    /// <param name="status">The expected status.</param>
    static void AddNamedProbe(string root, string folder, string scenario, string person, int status)
    {
        var page = UseCaseFile(root, folder);
        var result = folder == "borrow-copy" ? "result/borrow-copy.borrowed" : "result/extend-loan.extended";
        File.AppendAllText(page, $"""
            {Environment.NewLine}scenarios:
            - id: {scenario}
              title: Added Named Probe
              given: A reader holds a copy.
              when: The reader borrows the copy.
              then: The copy is lent.
              result: {result}
              probe:
                as: {person}
                request:
                  method: GET
                  path: /api/loans
                expect:
                  status: {status}
            """);
    }

    /// <summary>Adds the verify values block naming one fixed value.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="name">The value name.</param>
    /// <param name="value">The value.</param>
    static void AddValues(string root, string name, string value)
    {
        var project = Path.Combine(root, ".litespec", "project.yaml");
        File.AppendAllText(project, $"{Environment.NewLine}verify:{Environment.NewLine}  values:{Environment.NewLine}    {name}: {value}{Environment.NewLine}");
    }

    /// <summary>Adds one values probe resolving the loan value in its path and body.</summary>
    /// <param name="root">The fixture root.</param>
    static void AddValuesProbe(string root)
    {
        var page = UseCaseFile(root, "borrow-copy");
        File.AppendAllText(page, $$"""
            {{Environment.NewLine}}scenarios:
            - id: scenario/borrow-copy.values-probe
              title: Added Values Probe
              given: A reader holds a copy.
              when: The reader borrows the copy.
              then: The copy is lent.
              result: result/borrow-copy.borrowed
              probe:
                request:
                  method: POST
                  path: /api/loans/{loan}
                  body:
                    loanId: "{loan}"
                expect:
                  status: 200
                  fields:
                  - path: /state
                    equals: lent
            """);
    }

    /// <summary>Adds one cast person the probes can name.</summary>
    /// <param name="root">The fixture root.</param>
    static void AddCast(string root)
    {
        var cast = Path.Combine(root, ".litespec", "cast.yaml");
        File.WriteAllText(cast, """
            type: cast
            id: cast/fixture-library
            title: Fixture Library Cast
            summary: A test library with one reader.
            parties:
            - type: party
              id: party/fixture-library
              name: Fixture Library
              uses: product
              description: A test library.
            people:
            - type: person
              id: person/ava
              name: Ava
              actor: actor/reader
              party: party/fixture-library
              email: ava@fixture-library.example
              does: Borrows copies.
            """);
    }

    /// <summary>Adds the verify sign-in block to the project file.</summary>
    /// <param name="root">The fixture root.</param>
    static void AddSignIn(string root)
    {
        var project = Path.Combine(root, ".litespec", "project.yaml");
        File.AppendAllText(project, $"{Environment.NewLine}verify:{Environment.NewLine}  signIn:{Environment.NewLine}  - method: GET{Environment.NewLine}    path: /auth/development/{{person}}{Environment.NewLine}");
    }

    /// <summary>The use-case record file of one folder.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="folder">The use-case folder.</param>
    /// <returns>The absolute record file path.</returns>
    static string UseCaseFile(string root, string folder) => folder switch
    {
        "borrow-copy" => Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml"),
        _ => Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "extend-loan", "extend-loan.yaml"),
    };

    /// <summary>The verify report path of one consumer.</summary>
    /// <param name="root">The fixture root.</param>
    /// <returns>The absolute report path.</returns>
    static string ReportPath(string root) => Path.Combine(root, ".litespec", "verify", "report.json");

    /// <summary>Finds a free local port for the unreachable case.</summary>
    /// <returns>The free port number.</returns>
    static int FreePort()
    {
        using var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        socket.Start();
        var port = ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }
}
