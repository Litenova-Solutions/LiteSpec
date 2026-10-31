using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the check decides severity from the record's own status, and that reader-missing blocks under CI.</summary>
public sealed class DecideTests
{
    /// <summary>An approved use case under a draft requirement blocks on its own status.</summary>
    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
    [Fact]
    public void Approved_Use_Case_Under_Draft_Requirement_Blocks()
    {
        using var fixture = ModelFixture.Prepare("small", "use-case-unimplemented");
        DraftMaxActiveLoans(fixture.Path);

        var model = ModelBuilder.Build(ConsumerConfig.Load(fixture.Path));

        Assert.Equal("reviewed", model.ById["use-case/lending.list-loans"].Status);
        Assert.Equal("draft", model.EffectiveStatus["use-case/lending.list-loans"]);

        var json = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "use-case-unimplemented");

        Assert.Equal(1, json.Status);
        Assert.Equal("blocking", outcome);
        Assert.Null(reason);
    }

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
        using var fixture = ModelFixture.Prepare("small");
        AddWebFrontend(fixture.Path);

        string[] args = ["model", "check", "--root", fixture.Path, "--format", "json"];
        var json = Cli.Run(args, environment: new Dictionary<string, string>
        {
            ["CI"] = "",
            ["PATH"] = PathWithoutNode(),
        });
        var (outcome, _) = OutcomeOf(json.Stdout, "reader-missing");

        Assert.Equal(0, json.Status);
        Assert.Equal("reported", outcome);
    }

    /// <summary>Drafts the requirement list-loans fulfills, with its decider and decide-by date.</summary>
    static void DraftMaxActiveLoans(string root)
    {
        var file = Path.Combine(root, "docs", "product", "lending", "borrowing.md");
        var lines = File.ReadAllLines(file).ToList();
        var id = lines.FindIndex(line => line.Contains("requirement/borrowing.max-active-loans", StringComparison.Ordinal));

        Assert.True(id >= 0, "The borrowing page names no max-active-loans requirement to draft.");

        var at = lines.FindIndex(id, line => line.Trim() == "\"status\": \"approved\",");

        Assert.True(at >= 0, "The max-active-loans requirement carries no approved status to draft.");

        lines[at] = "      \"status\": \"draft\",";
        lines.Insert(at + 1, "      \"decider\": \"lending-team\",");
        lines.Insert(at + 2, "      \"decideBy\": \"2027-06-01\",");

        File.WriteAllLines(file, lines);
    }

    /// <summary>Declares a Svelte frontend the reader cannot reach once Node is hidden from PATH.</summary>
    static void AddWebFrontend(string root)
    {
        var file = Path.Combine(root, "litespec.project.json");
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
