using System.Net.Sockets;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Cli.Verify.Adversarial;

namespace LiteSpec.Cli.Tests.Verify.Adversarial;

/// <summary>Builds probes and runs the adversarial command against a local server.</summary>
static class AdversarialFixture
{
    /// <summary>Runs the adversarial command and captures its output without the command dispatch.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="baseUrl">The base address.</param>
    /// <param name="useCase">The use-case filter, or null.</param>
    /// <returns>The exit code and the two output streams.</returns>
    public static CliResult RunAdversarial(string root, string baseUrl, string? useCase = null)
    {
        string[] args = useCase is null
            ? ["verify", "--adversarial", "--root", root, "--base-url", baseUrl]
            : ["verify", "--adversarial", "--root", root, "--base-url", baseUrl, "--use-case", useCase];

        return TestCli.Run(args);
    }

    /// <summary>Adds one probe scenario to the use case.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="folder">The use-case folder, such as borrow-copy.</param>
    /// <param name="scenario">The scenario id.</param>
    /// <param name="asPerson">The cast person id, or null for anonymous.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The request path.</param>
    /// <param name="bodyYaml">The body value YAML at zero indent, or null for none. A value starting with [ writes inline, such as an array; anything else writes as a mapping.</param>
    /// <param name="status">The expected status.</param>
    public static void AddProbe(
        string root,
        string folder,
        string scenario,
        string? asPerson,
        string method,
        string path,
        string? bodyYaml,
        int status)
    {
        var result = folder == "borrow-copy" ? "result/borrow-copy.borrowed" : "result/extend-loan.extended";
        var asYaml = asPerson is null ? string.Empty : $"{Environment.NewLine}    as: {asPerson}";
        var bodyBlock = BodyBlock(bodyYaml);
        File.AppendAllText(UseCaseFile(root, folder), $"""
            {Environment.NewLine}scenarios:
            - id: {scenario}
              title: Added Adversarial Probe
              given: A reader holds a copy.
              when: The reader borrows the copy.
              then: The copy is lent.
              result: {result}
              probe:{asYaml}
                request:
                  method: {method}
                  path: {path}{bodyBlock}
                expect:
                  status: {status}
            """);
    }

    /// <summary>Gives the use case a key idempotency, so the race matrix replays its probes.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="folder">The use-case folder, such as borrow-copy.</param>
    public static void PatchIdempotencyKey(string root, string folder)
    {
        var page = UseCaseFile(root, folder);
        var text = File.ReadAllText(page);
        var none = "  mode: none\n  reason: Each call lends its own copy, and lending an unavailable copy is refused.\n";
        var key = "  mode: key\n  key: \"{copyId}\"\n  scope: tenant\n  replay: returns-first-result\n";

        if (!text.Contains(none, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The {folder} use case carries no none idempotency to replace.");
        }

        File.WriteAllText(page, text.Replace(none, key, StringComparison.Ordinal));
    }

    /// <summary>Adds one cast person the probes can name.</summary>
    /// <param name="root">The fixture root.</param>
    public static void AddCast(string root)
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

    /// <summary>The finding codes the adversarial report holds, in report order.</summary>
    /// <param name="root">The fixture root.</param>
    /// <returns>The codes of the report findings.</returns>
    public static IReadOnlyList<string> ReportCodes(string root)
    {
        var report = JsonNode.Parse(File.ReadAllText(ReportPath(root)))!.AsObject();
        return [.. report["findings"]!.AsArray().Select(finding => finding!["code"]!.GetValue<string>())];
    }

    /// <summary>The use-case record file of one folder.</summary>
    /// <param name="root">The fixture root.</param>
    /// <param name="folder">The use-case folder.</param>
    /// <returns>The absolute record file path.</returns>
    public static string UseCaseFile(string root, string folder) => folder switch
    {
        "borrow-copy" => Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml"),
        _ => Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "extend-loan", "extend-loan.yaml"),
    };

    /// <summary>The adversarial report path of one consumer.</summary>
    /// <param name="root">The fixture root.</param>
    /// <returns>The absolute report path.</returns>
    public static string ReportPath(string root) =>
        Path.Combine(root, ".litespec", "verify", "adversarial.json");

    /// <summary>Finds a free local port for the unreachable case.</summary>
    /// <returns>The free port number.</returns>
    public static int FreePort()
    {
        using var socket = new TcpListener(System.Net.IPAddress.Loopback, 0);
        socket.Start();
        var port = ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }

    /// <summary>The body block of one probe request, with the mapping entries indented under body.</summary>
    /// <param name="bodyYaml">The body value YAML at zero indent, or null for none.</param>
    /// <returns>The request lines holding the body, or empty when the probe sends none.</returns>
    private static string BodyBlock(string? bodyYaml)
    {
        if (bodyYaml is null)
        {
            return string.Empty;
        }

        if (bodyYaml.StartsWith('['))
        {
            return $"{Environment.NewLine}      body: {bodyYaml}";
        }

        var entries = string.Join(
            Environment.NewLine,
            bodyYaml.Split('\n').Select(line => "        " + line.Trim()));
        return $"{Environment.NewLine}      body:{Environment.NewLine}{entries}";
    }
}
