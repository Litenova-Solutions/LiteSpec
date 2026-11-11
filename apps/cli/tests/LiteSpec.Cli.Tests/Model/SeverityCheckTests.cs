using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Consumer;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves check findings block or report by their tier.</summary>
public sealed class SeverityCheckTests
{
    /// <summary>An S finding no reader produced blocks under a project with only web stacks.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void RecordFindingUnderOnlyWebStacksBlocks()
    {
        using var fixture = ModelFixture.Prepare("small", "use-case-unimplemented");
        SetOnlyWebStacks(fixture.Path);

        var json = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (outcome, reason) = OutcomeOf(json.Stdout, "use-case-unimplemented");

        Assert.Equal(1, json.Status);
        Assert.Equal("blocking", outcome);
        Assert.Null(reason);
    }

    /// <summary>The planned-Intent set holds the five reachability codes, each applying under the intent-and-behavior claim.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void PlannedIntentHoldsTheFiveReachabilityCodes()
    {
        Assert.Equal(
            new[] { "constraint-unreached", "goal-mover-missing", "intent-unreached", "requirement-fulfillment-missing", "step-use-case-missing" },
            Severity.PlannedIntent.OrderBy(code => code, StringComparer.Ordinal).ToArray());
        var claim = new LayerClaims(Intent: true, Behavior: true, Implementation: null, Proof: null);
        Assert.All(
            Severity.PlannedIntent,
            code => Assert.True(claim.Covers(code, [])));
    }

    /// <summary>A planned-Intent finding reports locally and blocks in the pull-request check under the intent-and-behavior claim.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void PlannedIntentReportsLocallyAndBlocksInCIUnderIntentAndBehavior()
    {
        using var fixture = ModelFixture.Prepare("large", "requirement-fulfillment-missing");
        SetClaim(fixture.Path, ConsumerFixture.Layers(null, null));

        var local = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);
        var (localOutcome, _) = OutcomeOf(local.Stdout, "requirement-fulfillment-missing");

        Assert.Equal(0, local.Status);
        Assert.Equal("reported", localOutcome);

        var pullRequest = TestCli.Run(
            ["model", "check", "--root", fixture.Path, "--format", "json"],
            environment: new Dictionary<string, string> { ["CI"] = "true" });
        var (pullOutcome, _) = OutcomeOf(pullRequest.Stdout, "requirement-fulfillment-missing");

        Assert.Equal(1, pullRequest.Status);
        Assert.Equal("blocking", pullOutcome);
    }

    /// <summary>An unknown marks value fails the project schema and reports project-invalid.</summary>
    /// <proves>litespec/core/scope#write-every-hand-written-litespec-file-in-yaml</proves>
    [Fact]
    public void UnknownMarksReportsProjectInvalid()
    {
        using var fixture = ModelFixture.Prepare("small");
        BreakMarks(fixture.Path, "yaml");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("project-invalid", result.Output);
        Assert.Contains("marks", result.Output);
    }

    /// <summary>Writes the claimed layers into the fixture project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="layers">The layers object the project claims.</param>
    static void SetClaim(string root, JsonObject layers)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = RecordText.Read(file)!.AsObject();
        project["layers"] = layers;
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Leaves the fixture with only a web stack, so no .NET reader runs.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void SetOnlyWebStacks(string root)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = RecordText.Read(file)!.AsObject();
        project["stacks"] = new JsonArray(JsonValue.Create("svelte"));
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Writes an unknown marks value into the fixture's project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="value">The marks value to write.</param>
    static void BreakMarks(string root, string value)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = RecordText.Read(file)!.AsObject();
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
