using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the check qualifies S findings by reader, that reader-missing blocks under CI, and that the release gate blocks unverified use cases.</summary>
public sealed class DecideTests
{
    /// <summary>Reader-unavailable blocks when CI is set, even though its reader is unqualified.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Reader_Unavailable_Blocks_When_CI_Is_Set()
    {
        using var fixture = ModelFixture.Prepare("small");
        AddWebFrontend(fixture.Path);

        string[] args = ["model", "check", "--root", fixture.Path, "--format", "json"];
        var json = Cli.Run(args, environment: new Dictionary<string, string>
        {
            ["CI"] = "true",
            ["PATH"] = PathWithoutNode(),
        });
        var (outcome, _) = OutcomeOf(json.Stdout, "reader-missing");

        Assert.Equal(1, json.Status);
        Assert.Equal("blocking", outcome);
    }

    /// <summary>Reader-unavailable reports without blocking when CI is unset.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Reader_Unavailable_Reports_When_CI_Is_Unset()
    {
        // The large fixture carries a controlled svelte frontend, whose screens the Node reader reads.
        using var fixture = ModelFixture.Prepare("large");

        string[] args = ["model", "check", "--root", fixture.Path, "--format", "json"];
        var json = Cli.Run(args, environment: new Dictionary<string, string>
        {
            ["CI"] = "",
            ["PATH"] = PathWithoutNode(),
        });
        var (outcome, _) = OutcomeOf(json.Stdout, "reader-missing");

        Assert.True(json.Status == 0, json.Output);
        Assert.Equal("reported", outcome);
    }

    /// <summary>At release an unverified use case blocks instead of reporting.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Release_Blocks_An_Unverified_Use_Case()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteScenario(fixture.Path);

        var loose = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (looseOutcome, _) = OutcomeOf(loose.Stdout, "use-case-unverified");

        Assert.True(loose.Status == 0, loose.Output);
        Assert.Equal("reported", looseOutcome);

        var strict = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json", "--release");
        var (strictOutcome, _) = OutcomeOf(strict.Stdout, "use-case-unverified");

        Assert.Equal(1, strict.Status);
        Assert.Equal("blocking", strictOutcome);
    }

    /// <summary>Each JSON finding carries the severity name of its level: error for G, warning for S and note for A.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Json_Findings_Carry_Severity_By_Level()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal("error", SeverityOf(result.Stdout, "paging-undeclared"));
        Assert.Equal("G", LevelOf(result.Stdout, "paging-undeclared"));

        using var advisoryFixture = ModelFixture.Prepare("small");
        WriteScenario(advisoryFixture.Path);
        var advisory = Cli.Run("model", "check", "--root", advisoryFixture.Path, "--format", "json");

        Assert.Equal("note", SeverityOf(advisory.Stdout, "use-case-unverified"));
        Assert.Equal("A", LevelOf(advisory.Stdout, "use-case-unverified"));

        using var readerFixture = ModelFixture.Prepare("small", "use-case-unimplemented");
        var reader = Cli.Run("model", "check", "--root", readerFixture.Path, "--format", "json");

        Assert.Equal("warning", SeverityOf(reader.Stdout, "use-case-unimplemented"));
        Assert.Equal("S", LevelOf(reader.Stdout, "use-case-unimplemented"));
    }

    /// <summary>Reads one finding's severity from the check JSON.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <param name="code">The finding code to read.</param>
    /// <returns>The finding severity: error, warning or note.</returns>
    static string? SeverityOf(string stdout, string code)
    {
        using var document = JsonDocument.Parse(stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            if (finding.GetProperty("code").GetString() == code)
            {
                return finding.GetProperty("severity").GetString();
            }
        }

        throw new InvalidOperationException($"No finding carries '{code}'.");
    }

    /// <summary>Reads one finding's level from the check JSON.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <param name="code">The finding code to read.</param>
    /// <returns>The finding level: G, S or A.</returns>
    static string? LevelOf(string stdout, string code)
    {
        using var document = JsonDocument.Parse(stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            if (finding.GetProperty("code").GetString() == code)
            {
                return finding.GetProperty("level").GetString();
            }
        }

        throw new InvalidOperationException($"No finding carries '{code}'.");
    }

    /// <summary>Appends a scenario entry to borrow-copy with no proving test.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteScenario(string root)
    {
        var page = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml");
        var record = JsonNode.Parse(YamlText.Parse(File.ReadAllText(page)).ToJson())!.AsObject();
        record["records"]!.AsArray().Add(JsonNode.Parse("""
            {
              "type": "scenario",
              "id": "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
              "title": "Borrowing an available copy succeeds",
              "given": "A copy sits available on its shelf.",
              "when": "The reader borrows the copy.",
              "then": "The loan is active.",
              "result": "result/borrow-copy.borrowed"
            }
            """));
        File.WriteAllText(page, RecordFile.Write(record));
    }

    /// <summary>Declares a Svelte frontend the reader cannot reach once Node is hidden from PATH.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void AddWebFrontend(string root)
    {
        var file = Path.Combine(root, ".litespec/project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        var paths = (JsonObject)project["paths"]!;
        paths["frontends"] = new JsonArray(
            new JsonObject
            {
                ["name"] = "web",
                ["path"] = "apps/web",
                ["platform"] = "svelte",
            });
        File.WriteAllText(file, project.ToJsonString());
        Directory.CreateDirectory(Path.Combine(root, "apps", "web"));
    }

    /// <summary>The process PATH without the folders holding Node, so the TypeScript reader is unavailable.</summary>
    /// <returns>The PATH value with every Node folder removed.</returns>
    static string PathWithoutNode()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";

        return string.Join(
            Path.PathSeparator,
            path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(folder => !File.Exists(Path.Combine(folder.Trim(), "node.exe"))
                    && !File.Exists(Path.Combine(folder.Trim(), "node"))));
    }

    /// <summary>Reads one finding's outcome and reason from the check JSON.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <param name="code">The finding code to read.</param>
    /// <returns>The finding outcome and reason, each null when absent.</returns>
    static (string? Outcome, string? Reason) OutcomeOf(string stdout, string code)
    {
        using var document = JsonDocument.Parse(stdout);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            if (finding.GetProperty("code").GetString() == code)
            {
                var reason = finding.GetProperty("reason");

                return (
                    finding.GetProperty("outcome").GetString(),
                    reason.ValueKind == JsonValueKind.Null ? null : reason.GetString());
            }
        }

        throw new InvalidOperationException($"No finding carries '{code}'.");
    }
}
