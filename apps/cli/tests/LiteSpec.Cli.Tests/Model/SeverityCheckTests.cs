using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves check findings follow the record's own status.</summary>
public sealed class SeverityCheckTests
{
    /// <summary>An S finding on a draft use case reports with draft and exits 0.</summary>
    /// <proves>litespec/core/model#hold-each-finding-to-its-own-status</proves>
    [Fact]
    public void Draft_Use_Case_Reports_Without_Blocking()
    {
        using var fixture = ModelFixture.Prepare("small", "use-case-unimplemented");
        DraftListLoans(fixture.Path);

        var text = Cli.Run("model", "check", "--root", fixture.Path);
        var line = text.Output.Split('\n').Single(candidate => candidate.Contains("[use-case-unimplemented]"));

        Assert.Equal(0, text.Status);
        Assert.Contains("[S draft]", line);

        var json = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "use-case-unimplemented");

        Assert.Equal(0, json.Status);
        Assert.Equal("reported", outcome);
        Assert.Equal("draft", reason);
    }

    /// <summary>The same S finding on a reviewed use case blocks and exits 1.</summary>
    /// <proves>litespec/core/model#hold-each-finding-to-its-own-status</proves>
    [Fact]
    public void Reviewed_Use_Case_Blocks()
    {
        using var fixture = ModelFixture.Prepare("small", "use-case-unimplemented");
        EnsureReviewedListLoans(fixture.Path);

        var text = Cli.Run("model", "check", "--root", fixture.Path);
        var line = text.Output.Split('\n').Single(candidate => candidate.Contains("[use-case-unimplemented]"));

        Assert.Equal(1, text.Status);
        Assert.Contains("[S]", line);
        Assert.DoesNotContain("draft", line);

        var json = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "use-case-unimplemented");

        Assert.Equal(1, json.Status);
        Assert.Equal("blocking", outcome);
        Assert.Null(reason);
    }

    /// <summary>An S finding no reader produced blocks under a project with only web stacks.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Record_Finding_Under_Only_Web_Stacks_Blocks()
    {
        using var fixture = ModelFixture.Prepare("small", "use-case-unimplemented");
        SetOnlyWebStacks(fixture.Path);

        var json = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "use-case-unimplemented");

        Assert.Equal(1, json.Status);
        Assert.Equal("blocking", outcome);
        Assert.Null(reason);
    }

    /// <summary>Every code a known gap may name has a tier, so an unknown code fails the build.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Every_Checker_Code_Has_A_Tier()
    {
        var missing = Checker.Codes
            .Where(code => !Finding.Tiers.ContainsKey(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missing);
    }

    /// <summary>An unknown marks value exits 2 with the file error.</summary>
    [Fact]
    public void Unknown_Marks_Exits_2()
    {
        using var fixture = ModelFixture.Prepare("small");
        BreakMarks(fixture.Path, "yaml");

        var result = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("unknown marks \"yaml\"", result.Output);
    }

    /// <summary>Flips the list-loans page to a draft with its decider and decide-by date.</summary>
    static void DraftListLoans(string root)
    {
        var file = Path.Combine(root, "docs", "behavior", "modules", "lending", "loans", "list-loans.md");
        var lines = File.ReadAllLines(file).ToList();
        var at = lines.FindIndex(line => line.Trim() == "\"status\": \"reviewed\",");

        Assert.True(at >= 0, "The list-loans page carries no reviewed status to draft.");

        lines[at] = "  \"status\": \"draft\",";
        lines.Insert(at + 1, "  \"decider\": \"lending-team\",");
        lines.Insert(at + 2, "  \"decideBy\": \"2027-06-01\",");

        File.WriteAllLines(file, lines);
    }

    /// <summary>Leaves the list-loans page reviewed, restoring it when a draft is found.</summary>
    static void EnsureReviewedListLoans(string root)
    {
        var file = Path.Combine(root, "docs", "behavior", "modules", "lending", "loans", "list-loans.md");
        var lines = File.ReadAllLines(file).ToList();
        var at = lines.FindIndex(line => line.Trim() == "\"status\": \"draft\",");

        if (at >= 0)
        {
            lines[at] = "  \"status\": \"reviewed\",";
            lines.RemoveAll(line =>
                line.Trim().StartsWith("\"decider\":", StringComparison.Ordinal)
                || line.Trim().StartsWith("\"decideBy\":", StringComparison.Ordinal));
            File.WriteAllLines(file, lines);
        }

        Assert.Contains("\"status\": \"reviewed\",", File.ReadAllText(file));
    }

    /// <summary>Leaves the fixture with only a web stack, so no .NET reader runs.</summary>
    static void SetOnlyWebStacks(string root)
    {
        var file = Path.Combine(root, "litespec.project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        project["stacks"] = new JsonArray(JsonValue.Create("svelte"));
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Writes an unknown marks value into the fixture's project file.</summary>
    static void BreakMarks(string root, string value)
    {
        var file = Path.Combine(root, "litespec.project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        project["marks"] = JsonValue.Create(value);
        File.WriteAllText(file, project.ToJsonString());
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
