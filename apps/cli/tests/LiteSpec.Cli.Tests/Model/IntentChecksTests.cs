using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the Intent checks on problems, actors, journeys and goals against the large fixture.</summary>
public sealed class IntentChecksTests
{
    /// <summary>The problem record file of the large fixture.</summary>
    const string ProblemFile = ".litespec/product/problems/overdue-loans-pile-up.yaml";

    /// <summary>The journey record file of the large fixture.</summary>
    const string JourneyFile = ".litespec/product/journeys/borrow-and-return.yaml";

    /// <summary>A problem whose primary sufferer belongs to the team blocks with problem-sufferer-invalid.</summary>
    /// <proves>litespec/core/intent#admit-only-outside-pains-as-problems</proves>
    [Fact]
    public void Problem_With_Team_Sufferer_Blocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Write(
            fixture.Path,
            ".litespec/behavior/actors/platform-staff.yaml",
            """
            {
              "type": "actor",
              "id": "actor/platform-staff",
              "title": "Platform Staff",
              "owner": "lending-team",
              "status": "draft",
              "decider": "lending-team",
              "decideBy": "2027-01-01",
              "summary": "The team building and running the product.",
              "kind": "person",
              "belongsToTeam": true,
              "holds": [{ "role": "role/librarian", "scope": "platform" }]
            }
            """);
        var problem = ReadRecord(fixture.Path, ProblemFile);
        problem["affects"] = new JsonArray("actor/platform-staff");
        Restamp(fixture.Path, ProblemFile, problem);

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("problem-sufferer-invalid", result.Output);
    }

    /// <summary>A problem stating unknown cost with no open question blocks with problem-cost-missing.</summary>
    /// <proves>litespec/core/intent#state-the-cost-and-workaround-of-every-problem</proves>
    [Fact]
    public void Problem_With_Unknown_Cost_And_No_Question_Blocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        var problem = ReadRecord(fixture.Path, ProblemFile);
        problem["costToday"] = "unknown";
        Restamp(fixture.Path, ProblemFile, problem);

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("problem-cost-missing", result.Output);
    }

    /// <summary>A problem stating unknown cost with an open question reports no problem-cost-missing.</summary>
    /// <proves>litespec/core/intent#state-the-cost-and-workaround-of-every-problem</proves>
    [Fact]
    public void Problem_With_Unknown_Cost_And_Open_Question_Passes()
    {
        using var fixture = ModelFixture.Prepare("large");
        var problem = ReadRecord(fixture.Path, ProblemFile);
        problem["status"] = "draft";
        problem["decider"] = "lending-team";
        problem["decideBy"] = "2027-01-01";
        problem.Remove("approvedBy");
        problem.Remove("approvedOn");
        problem.Remove("approvedHash");
        problem["costToday"] = "unknown";
        problem["questions"] = new JsonArray(
            JsonNode.Parse(
                """
                {
                  "question": "What does a late fee cost a reader each month?",
                  "decider": "lending-team",
                  "decideBy": "2027-01-01",
                  "ballot": "borrowing",
                  "recommendation": "The desk lead prices the late fees from the desk log."
                }
                """));
        File.WriteAllText(Path.Combine(fixture.Path, ProblemFile), RecordFile.Write(problem));

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("problem-cost-missing", result.Output);
    }

    /// <summary>An approved problem with only assumed evidence blocks with problem-evidence-missing.</summary>
    /// <proves>litespec/core/intent#grade-problem-evidence-by-kind</proves>
    [Fact]
    public void Approved_Problem_With_Assumed_Evidence_Blocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        var problem = ReadRecord(fixture.Path, ProblemFile);
        ((JsonArray)problem["evidence"]!)[0]!["kind"] = "assumed";
        Restamp(fixture.Path, ProblemFile, problem);

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("problem-evidence-missing", result.Output);
    }

    /// <summary>A problem statement naming a solution word advises with problem-statement-invalid and never blocks.</summary>
    /// <proves>litespec/core/intent#name-no-solution-in-a-problem-statement</proves>
    [Fact]
    public void Problem_Statement_With_Solution_Word_Advises()
    {
        using var fixture = ModelFixture.Prepare("large");
        var problem = ReadRecord(fixture.Path, ProblemFile);
        problem["statement"] = "Readers need a reminder dashboard for due dates.";
        Restamp(fixture.Path, ProblemFile, problem);

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("problem-statement-invalid", result.Output);
    }

    /// <summary>An actor named by no journey, problem or access position advises with actor-unused.</summary>
    /// <proves>litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position</proves>
    [Fact]
    public void Unnamed_Actor_Advises()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Write(
            fixture.Path,
            ".litespec/behavior/actors/visitor.yaml",
            """
            {
              "type": "actor",
              "id": "actor/visitor",
              "title": "Visitor",
              "owner": "lending-team",
              "status": "draft",
              "decider": "lending-team",
              "decideBy": "2027-01-01",
              "summary": "A person reading public pages without signing in.",
              "kind": "person",
              "belongsToTeam": false
            }
            """);

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("actor-unused", result.Output);
    }

    /// <summary>A goal moved by no journey and no quality requirement advises with goal-mover-missing.</summary>
    /// <proves>litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement</proves>
    [Fact]
    public void Unmoved_Goal_Advises()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Write(
            fixture.Path,
            ".litespec/product/goals/more-members.yaml",
            """
            {
              "type": "goal",
              "id": "goal/more-members",
              "title": "More Members",
              "owner": "lending-team",
              "status": "draft",
              "decider": "lending-team",
              "decideBy": "2027-01-01",
              "statement": "More readers hold a membership.",
              "whyItMatters": "Members borrow more often.",
              "measure": "Members holding a card",
              "target": "500 members",
              "dueBy": "2027-06-01"
            }
            """);

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("goal-mover-missing", result.Output);
    }

    /// <summary>A journey step naming both an actor and a system blocks with field-invalid.</summary>
    /// <proves>litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system</proves>
    [Fact]
    public void Journey_Step_With_Actor_And_System_Blocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        var journey = ReadRecord(fixture.Path, JourneyFile);
        ((JsonArray)journey["steps"]!)[0]!["system"] = "external-system/text-gateway";
        Restamp(fixture.Path, JourneyFile, journey);

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("field-invalid", result.Output);
    }

    /// <summary>A capitalized phrase named in three records with no glossary term advises with term-missing.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void Undefined_Capitalized_Phrase_Advises()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Explain(fixture.Path, ".litespec/behavior/actors/librarian.md", "# Librarian\n\nDesign Partners meet at the desk each month.\n");
        TestRecords.Explain(fixture.Path, ".litespec/behavior/actors/reader.md", "# Reader\n\nDesign Partners borrow first each season.\n");
        TestRecords.Explain(fixture.Path, ".litespec/product/problems/overdue-loans-pile-up.md", "# Overdue Loans Pile Up\n\nDesign Partners report lost due dates at the desk.\n");

        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("term-missing", result.Output);
    }

    /// <summary>The built model writes the computed evidenceStrength of the problem record.</summary>
    /// <proves>litespec/core/intent#grade-problem-evidence-by-kind</proves>
    [Fact]
    public void Model_Writes_Evidence_Strength()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = Cli.Run("model", "build", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        var model = File.ReadAllText(Path.Combine(fixture.Path, ".litespec/model", "model.json"));
        Assert.Contains("\"evidenceStrength\": \"reported\"", model);
    }

    /// <summary>Reads one fixture record file as a JSON object.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="relative">The record file path under the fixture folder.</param>
    /// <returns>The record fields as a JSON object.</returns>
    static JsonObject ReadRecord(string root, string relative) =>
        JsonNode.Parse(YamlText.Parse(File.ReadAllText(Path.Combine(root, relative))).ToJson())!.AsObject();

    /// <summary>Recomputes the approved hash of a mutated record and writes it back.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="relative">The record file path under the fixture folder.</param>
    /// <param name="record">The mutated record fields.</param>
    static void Restamp(string root, string relative, JsonObject record)
    {
        using var document = JsonDocument.Parse(record.ToJsonString());
        var written = new LiteSpec.Model.Record
        {
            Type = record["type"]!.GetValue<string>(),
            Id = record["id"]!.GetValue<string>(),
            Raw = document.RootElement.Clone(),
        };
        record["approvedHash"] = Checker.SemanticHash(written);
        File.WriteAllText(Path.Combine(root, relative), RecordFile.Write(record));
    }
}
