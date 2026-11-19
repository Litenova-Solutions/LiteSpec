using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the Intent checks on problems, actors, flows and goals against the large fixture.</summary>
public sealed class IntentChecksTests
{
    /// <summary>The problem record file of the large fixture.</summary>
    const string ProblemFile = ".litespec/intent/problems/overdue-loans-pile-up/overdue-loans-pile-up.yaml";

    /// <summary>The flow record file of the large fixture.</summary>
    const string FlowFile = ".litespec/intent/flows/borrow-and-return/borrow-and-return.yaml";

    /// <summary>A problem whose primary sufferer belongs to the team blocks with problem-sufferer-invalid.</summary>
    /// <proves>litespec/core/intent#admit-only-outside-pains-as-problems</proves>
    [Fact]
    public void ProblemWithTeamSuffererBlocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Write(
            fixture.Path,
            ".litespec/intent/actors/platform-staff.yaml",
            """
            {
              "type": "actor",
              "id": "actor/platform-staff",
              "title": "Platform Staff",
              "summary": "The team building and running the product.",
              "kind": "person",
              "belongsToTeam": true,
              "holds": ["role/librarian"]
            }
            """);
        var problem = ReadRecord(fixture.Path, ProblemFile);
        problem["affects"] = new JsonArray("actor/platform-staff");
        Restamp(fixture.Path, ProblemFile, problem);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("problem-sufferer-invalid", result.Output);
    }

    /// <summary>A problem with only assumed evidence blocks with problem-evidence-missing.</summary>
    /// <proves>litespec/core/intent#grade-problem-evidence-by-kind</proves>
    [Fact]
    public void ProblemWithAssumedEvidenceBlocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        var problem = ReadRecord(fixture.Path, ProblemFile);
        ((JsonArray)problem["evidence"]!)[0]!["kind"] = "assumed";
        Restamp(fixture.Path, ProblemFile, problem);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("problem-evidence-missing", result.Output);
    }

    /// <summary>An actor named by no flow, problem or access position advises with actor-unused.</summary>
    /// <proves>litespec/core/intent#name-every-actor-from-a-flow-a-problem-or-an-access-position</proves>
    [Fact]
    public void UnnamedActorAdvises()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Write(
            fixture.Path,
            ".litespec/intent/actors/visitor.yaml",
            """
            {
              "type": "actor",
              "id": "actor/visitor",
              "title": "Visitor",
              "summary": "A person reading public pages without signing in.",
              "kind": "person",
              "belongsToTeam": false
            }
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("actor-unused", result.Output);
    }

    /// <summary>A goal moved by no flow and no quality requirement blocks with goal-mover-missing.</summary>
    /// <proves>litespec/core/intent#move-every-goal-with-a-flow-or-a-quality-requirement</proves>
    [Fact]
    public void UnmovedGoalBlocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Write(
            fixture.Path,
            ".litespec/intent/goals/more-members.yaml",
            """
            {
              "type": "goal",
              "id": "goal/more-members",
              "title": "More Members",
              "summary": "More readers hold a membership.",
              "statement": "More readers hold a membership.",
              "measure": "Members holding a card",
              "target": "500 members",
              "dueBy": "2027-06-01"
            }
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("goal-mover-missing", result.Output);
    }

    /// <summary>A flow step naming a system actor no external system represents blocks with record-invalid.</summary>
    /// <proves>litespec/core/intent#perform-each-flow-step-with-an-actor-or-an-external-system</proves>
    [Fact]
    public void FlowStepWithUnrepresentedSystemActorBlocks()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Write(
            fixture.Path,
            ".litespec/intent/actors/mail-relay.yaml",
            """
            {
              "type": "actor",
              "id": "actor/mail-relay",
              "title": "Mail Relay",
              "summary": "The mail relay sends receipts to readers.",
              "kind": "system",
              "belongsToTeam": false,
              "responsibilities": [
                "The mail relay sends receipts to readers."
              ],
              "knowledge": [
                "The product assumes the mail relay accepts one receipt per call."
              ]
            }
            """);
        var flow = ReadRecord(fixture.Path, FlowFile);
        ((JsonArray)flow["steps"]!)[0]!["actor"] = "actor/mail-relay";
        Restamp(fixture.Path, FlowFile, flow);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Equal(1, result.Status);
        Assert.Contains("record-invalid", result.Output);
    }

    /// <summary>A capitalized phrase named in three records with no glossary term advises with term-missing.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void UndefinedCapitalizedPhraseAdvises()
    {
        using var fixture = ModelFixture.Prepare("large");
        TestRecords.Explain(fixture.Path, ".litespec/intent/actors/librarian/librarian.md", "# Librarian\n\nDesign Partners meet at the desk each month.\n");
        TestRecords.Explain(fixture.Path, ".litespec/intent/actors/reader/reader.md", "# Reader\n\nDesign Partners borrow first each season.\n");
        TestRecords.Explain(fixture.Path, ".litespec/intent/problems/overdue-loans-pile-up/overdue-loans-pile-up.md", "# Overdue Loans Pile Up\n\nDesign Partners report lost due dates at the desk.\n");

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("term-missing", result.Output);
    }

    /// <summary>The built model writes the computed evidenceStrength of the problem record.</summary>
    /// <proves>litespec/core/intent#grade-problem-evidence-by-kind</proves>
    [Fact]
    public void ModelWritesEvidenceStrength()
    {
        using var fixture = ModelFixture.Prepare("large");
        var model = ModelBuilder.Build(ConsumerConfig.Load(fixture.Path));

        Assert.Contains("\"evidenceStrength\": \"reported\"", model.ToJson());
    }

    /// <summary>Reads one fixture record file as a JSON object.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="relative">The record file path under the fixture folder.</param>
    /// <returns>The record fields as a JSON object.</returns>
    static JsonObject ReadRecord(string root, string relative) =>
        JsonNode.Parse(YamlText.Parse(File.ReadAllText(Path.Combine(root, relative))).ToJson())!.AsObject();

    /// <summary>Writes a mutated record back to its file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="relative">The record file path under the fixture folder.</param>
    /// <param name="record">The mutated record fields.</param>
    static void Restamp(string root, string relative, JsonObject record) =>
        File.WriteAllText(Path.Combine(root, relative), RecordFile.Write(record));
}
