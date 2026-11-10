using LiteSpec.Tests.Consumer;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves check findings block or report by their tier.</summary>
public sealed class SeverityCheckTests
{
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

    /// <summary>The planned-Intent set holds the five reachability codes, each running at records.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Planned_Intent_Holds_The_Five_Reachability_Codes()
    {
        Assert.Equal(
            new[] { "constraint-unreached", "goal-mover-missing", "intent-unreached", "requirement-fulfillment-missing", "step-use-case-missing" },
            Severity.PlannedIntent.OrderBy(code => code, StringComparer.Ordinal).ToArray());
        Assert.All(
            Severity.PlannedIntent,
            code => Assert.Equal(ConformanceLevel.Records, ConformanceLevels.LowestOf(code)));
    }

    /// <summary>A planned-Intent finding reports locally and blocks in the pull-request check at the records level.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Planned_Intent_Reports_Locally_And_Blocks_In_CI_At_Records()
    {
        using var fixture = ModelFixture.Prepare("large", "requirement-fulfillment-missing");
        SetConformance(fixture.Path, "records");

        var local = Cli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);
        var (localOutcome, _) = OutcomeOf(local.Stdout, "requirement-fulfillment-missing");

        Assert.Equal(0, local.Status);
        Assert.Equal("reported", localOutcome);

        var pullRequest = Cli.Run(
            ["model", "check", "--root", fixture.Path, "--format", "json"],
            environment: new Dictionary<string, string> { ["CI"] = "true" });
        var (pullOutcome, _) = OutcomeOf(pullRequest.Stdout, "requirement-fulfillment-missing");

        Assert.Equal(1, pullRequest.Status);
        Assert.Equal("blocking", pullOutcome);
    }

    /// <summary>An unknown marks value exits 2 with the file error.</summary>
    [Fact]
    public void Unknown_Marks_Exits_2()
    {
        using var fixture = ModelFixture.Prepare("small");
        BreakMarks(fixture.Path, "yaml");

        var result = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("project-invalid", result.Output);
        Assert.Contains("marks", result.Output);
    }

    /// <summary>Writes the claimed conformance level into the fixture project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="level">The level name: records, traced or full.</param>
    static void SetConformance(string root, string level)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(file)!.AsObject();
        project["layers"] = ConsumerFixture.Layers(level);
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Leaves the fixture with only a web stack, so no .NET reader runs.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void SetOnlyWebStacks(string root)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(file)!.AsObject();
        project["stacks"] = new JsonArray(JsonValue.Create("svelte"));
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Writes an unknown marks value into the fixture's project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="value">The marks value to write.</param>
    static void BreakMarks(string root, string value)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(file)!.AsObject();
        project["marks"] = JsonValue.Create(value);
        File.WriteAllText(file, project.ToJsonString());
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
