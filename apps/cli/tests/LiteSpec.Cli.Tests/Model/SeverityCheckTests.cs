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

    /// <summary>Leaves the fixture with only a web stack, so no .NET reader runs.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void SetOnlyWebStacks(string root)
    {
        var file = Path.Combine(root, ".litespec/project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        project["stacks"] = new JsonArray(JsonValue.Create("svelte"));
        File.WriteAllText(file, project.ToJsonString());
    }

    /// <summary>Writes an unknown marks value into the fixture's project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="value">The marks value to write.</param>
    static void BreakMarks(string root, string value)
    {
        var file = Path.Combine(root, ".litespec/project.json");
        var project = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
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
