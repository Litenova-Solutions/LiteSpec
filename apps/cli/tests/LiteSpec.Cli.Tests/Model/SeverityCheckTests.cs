using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves check findings follow the record status and the project phase.</summary>
public sealed class SeverityCheckTests
{
    /// <summary>An S finding on a draft use case reports with draft and exits 0.</summary>
    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
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
    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
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

    /// <summary>
    ///     A frozen phase reports a proof finding with frozen instead of blocking. The overlay also
    ///     leaves a G finding that blocks in every phase, so the outcome is asserted, not the exit code.
    /// </summary>
    /// <proves>litespec/core/model#hold-proof-findings-while-tests-are-frozen</proves>
    [Fact]
    public void Frozen_Phase_Reports_Proof_Finding_Without_Blocking()
    {
        using var fixture = ModelFixture.Prepare("small", "proves-unconfirmed");
        FreezePhase(fixture.Path);

        var text = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Contains("[S frozen]", text.Output);
        Assert.Contains("proves-unconfirmed", text.Output);

        var json = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "proves-unconfirmed");

        Assert.Equal("reported", outcome);
        Assert.Equal("frozen", reason);
    }

    /// <summary>A working phase blocks a proof finding and exits 1.</summary>
    /// <proves>litespec/core/model#hold-proof-findings-while-tests-are-frozen</proves>
    [Fact]
    public void Working_Phase_Blocks_Proof_Finding()
    {
        using var fixture = ModelFixture.Prepare("small", "proves-unconfirmed");

        var text = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, text.Status);
        Assert.DoesNotContain("[S frozen]", text.Output);
        Assert.Contains("proves-unconfirmed", text.Output);
        var json = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "proves-unconfirmed");

        Assert.Equal("blocking", outcome);
        Assert.Null(reason);
    }

    /// <summary>An unknown marks or phase value exits 2 with the file error.</summary>
    /// <proves>litespec/core/model#hold-proof-findings-while-tests-are-frozen</proves>
    [Theory]
    [InlineData("marks", "yaml", "unknown marks \"yaml\"")]
    [InlineData("phase", "paused", "unknown check.phase \"paused\"")]
    public void Unknown_Setting_Exits_2(string field, string value, string message)
    {
        using var fixture = ModelFixture.Prepare("small");
        BreakSetting(fixture.Path, field, value);

        var result = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains(message, result.Output);
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

    /// <summary>Freezes the fixture's check phase.</summary>
    static void FreezePhase(string root)
    {
        var file = Path.Combine(root, "litespec.project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        project["check"] = new JsonObject { ["phase"] = JsonValue.Create("frozen") };
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Writes an unknown marks or phase value into the fixture's project file.</summary>
    static void BreakSetting(string root, string field, string value)
    {
        var file = Path.Combine(root, "litespec.project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();

        if (field == "marks")
        {
            project["marks"] = JsonValue.Create(value);
        }
        else
        {
            project["check"] = new JsonObject { ["phase"] = JsonValue.Create(value) };
        }

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
