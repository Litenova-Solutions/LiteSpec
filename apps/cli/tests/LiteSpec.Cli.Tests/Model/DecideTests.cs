using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the check decides severity from the record's own status, and that reader-missing blocks under CI.</summary>
public sealed class DecideTests
{
    /// <summary>An approved use case under a draft requirement blocks on its own status.</summary>
    /// <proves>litespec/core/model#hold-each-finding-to-its-own-status</proves>
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

    /// <summary>At release an unanswered question on a record carrying a risk class blocks instead of reporting.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Release_Blocks_An_Unanswered_Risk_Question()
    {
        using var fixture = ModelFixture.Prepare("small");
        DraftRiskQuestion(fixture.Path);

        var loose = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        var (looseOutcome, _) = OutcomeOf(loose.Stdout, "question-expired");

        Assert.Equal(0, loose.Status);
        Assert.Equal("reported", looseOutcome);

        var strict = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json", "--release");
        var (strictOutcome, _) = OutcomeOf(strict.Stdout, "question-expired");

        Assert.Equal(1, strict.Status);
        Assert.Equal("blocking", strictOutcome);
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

        Assert.Equal(0, loose.Status);
        Assert.Equal("reported", looseOutcome);

        var strict = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json", "--release");
        var (strictOutcome, _) = OutcomeOf(strict.Stdout, "use-case-unverified");

        Assert.Equal(1, strict.Status);
        Assert.Equal("blocking", strictOutcome);
    }

    /// <summary>Drafts borrow-copy with a risk value and an unanswered past-due question.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void DraftRiskQuestion(string root)
    {
        var file = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml");
        var record = JsonNode.Parse(YamlText.Parse(File.ReadAllText(file)).ToJson())!.AsObject();

        Assert.True(
            record["id"]?.GetValue<string>() == "use-case/lending.borrow-copy",
            "The borrow-copy page names no use case to draft.");
        Assert.True(
            record["status"]?.GetValue<string>() == "reviewed",
            "The borrow-copy use case carries no reviewed status to draft.");

        record["status"] = "draft";
        record["decider"] = "lending-team";
        record["decideBy"] = "2027-06-01";
        record["risk"] = new JsonArray(JsonValue.Create("money"));
        record["questions"] = new JsonArray(
            new JsonObject
            {
                ["question"] = "Who covers a lost copy?",
                ["decider"] = "lending-team",
                ["decideBy"] = "2020-01-01",
                ["ballot"] = "lost-copy",
                ["recommendation"] = "The library covers the copy.",
            });

        File.WriteAllText(file, RecordFile.Write(record));
    }

    /// <summary>Writes a draft scenario for borrow-copy with no proving test.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteScenario(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/lending/loans/borrow-copy-draft-scenario.yaml", """
            {
              "type": "scenario",
              "id": "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
              "title": "Borrowing an available copy succeeds",
              "owner": "lending-team",
              "status": "draft",
              "decider": "lending-team",
              "decideBy": "2027-06-01",
              "given": "A copy sits available on its shelf.",
              "when": "The reader borrows the copy.",
              "result": "result/borrow-copy.borrowed",
              "then": "The loan is active."
            }
            """);
        TestRecords.Explain(root, ".litespec/behavior/modules/lending/loans/borrow-copy-draft-scenario.md", """
            # Borrowing an available copy succeeds

            A reader borrows an available copy.
            """);
    }

    /// <summary>Drafts the requirement list-loans fulfills, with its decider and decide-by date.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void DraftMaxActiveLoans(string root)
    {
        var file = Path.Combine(root, ".litespec", "product", "lending", "borrowing.yaml");
        var page = JsonNode.Parse(YamlText.Parse(File.ReadAllText(file)).ToJson())!.AsObject();
        var entry = page["records"]!.AsArray()
            .OfType<JsonObject>()
            .FirstOrDefault(candidate => candidate["id"]?.GetValue<string>() == "requirement/borrowing.max-active-loans");

        Assert.True(entry is not null, "The borrowing page names no max-active-loans requirement to draft.");
        var requirement = entry!;
        Assert.True(
            requirement["status"]?.GetValue<string>() == "approved",
            "The max-active-loans requirement carries no approved status to draft.");

        requirement["status"] = "draft";
        requirement["decider"] = "lending-team";
        requirement["decideBy"] = "2027-06-01";

        File.WriteAllText(file, RecordFile.Write(page));
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
