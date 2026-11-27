using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Verify;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves a verify probe proves the Behavior scenario its report names.</summary>
public sealed class ProbeRunReaderTests
{
    const string Borrow = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";
    const string Return = "scenario/return-copy.returning-a-borrowed-copy-succeeds";

    /// <summary>A passing probe proves its scenario as passed.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingProbeProvesItsScenario()
    {
        using var temp = TestCli.Temp("reader-probe-proof");
        WriteProject(temp.Path);
        WriteReport(temp.Path, Probe(Borrow, "passed"), "");

        var run = new ProbeRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Passed", run.OutcomeByScenario[Borrow]);
        Assert.Equal(1, run.Passed);
        Assert.Equal(0, run.Failed);
    }

    /// <summary>A probe with a probe-failed finding proves as failed.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void FailedProbeFailsItsScenario()
    {
        using var temp = TestCli.Temp("reader-probe-proof");
        WriteProject(temp.Path);
        WriteReport(temp.Path, ProbeWithoutResult(Borrow), Failed(Borrow));

        var run = new ProbeRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.Equal("Failed", run.OutcomeByScenario[Borrow]);
        Assert.Equal(0, run.Passed);
        Assert.Equal(1, run.Failed);
    }

    /// <summary>An unreachable probe holds no outcome beside a probe that ran.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UnreachableProbeLeavesItsScenarioUnproved()
    {
        using var temp = TestCli.Temp("reader-probe-proof");
        WriteProject(temp.Path);
        WriteReport(temp.Path, Probe(Borrow, "unreachable") + "," + Probe(Return, "passed"), Unreachable(Borrow));

        var run = new ProbeRunReader().Read(ConsumerConfig.Load(temp.Path))!;

        Assert.DoesNotContain(Borrow, run.OutcomeByScenario.Keys);
        Assert.Equal("Passed", run.OutcomeByScenario[Return]);
    }

    /// <summary>A report with only unreachable probes reads no run.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ReportWithOnlyUnreachableProbesReadsNoRun()
    {
        using var temp = TestCli.Temp("reader-probe-proof");
        WriteProject(temp.Path);
        WriteReport(temp.Path, Probe(Borrow, "unreachable"), Unreachable(Borrow));

        Assert.Null(new ProbeRunReader().Read(ConsumerConfig.Load(temp.Path)));
    }

    /// <summary>A passing probe confirms the scenario it names.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingProbeConfirmsTheScenarioItNames()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson(Borrow, "Borrowing an available copy succeeds", "result/borrow-copy.borrowed"));
        WriteReport(temp.Path, Probe(Borrow, "passed"), "");

        using var document = Build(temp.Path);
        var link = LinkOf(document, Borrow);

        Assert.True(link.HasValue);

        var found = link!.Value;

        Assert.True(found.GetProperty("confirmed").GetBoolean());
        Assert.True(found.GetProperty("passing").GetBoolean());
    }

    /// <summary>A failing probe leaves the scenario not passing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void FailingProbeLeavesTheScenarioNotPassing()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson(Borrow, "Borrowing an available copy succeeds", "result/borrow-copy.borrowed"));
        WriteReport(temp.Path, Probe(Borrow, "failed"), Failed(Borrow));

        using var document = Build(temp.Path);
        var link = LinkOf(document, Borrow);

        Assert.True(link.HasValue);

        var found = link!.Value;

        Assert.True(found.GetProperty("confirmed").GetBoolean());
        Assert.False(found.GetProperty("passing").GetBoolean());
    }

    /// <summary>An unreachable probe proves nothing.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UnreachableProbeProvesNothing()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson(Borrow, "Borrowing an available copy succeeds", "result/borrow-copy.borrowed"));
        WriteReport(temp.Path, Probe(Borrow, "unreachable"), Unreachable(Borrow));

        using var document = Build(temp.Path);

        Assert.False(LinkOf(document, Borrow).HasValue);
    }

    /// <summary>A passing probe verifies its use case until a code change makes the report stale.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void OlderReportReadsStaleAfterCodeChanges()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        WriteReport(temp.Path, Probe(Borrow, "passed"), "");
        Write(temp.Path, "Touched.txt", "one");
        CommitFixture(temp.Path);

        using (var first = Build(temp.Path))
        {
            Assert.True(Verified(first.RootElement));
        }

        Write(temp.Path, "Touched.txt", "two");
        CommitAll(temp.Path, "Change the code.");

        using var changed = Build(temp.Path);

        Assert.False(Verified(changed.RootElement));
        Assert.Contains(Borrow, HeldBy(changed.RootElement));

        var proof = ProofOf(changed.RootElement, Borrow);

        Assert.Equal("stale", proof.GetProperty("status").GetString());
    }

    const string ScenarioPath = ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy-probe-proof-scenario.yaml";
    const string UseCase = "use-case/lending.borrow-copy";

    static void WriteProject(string root) =>
        File.WriteAllText(Path.Combine(root, ".litespec/project.yaml"), """{ "owners": ["team"] }""");

    static void WriteReport(string root, string probes, string findings)
    {
        var folder = Path.Combine(root, ".litespec", "verify");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "report.json"),
            "{\n"
            + "  \"baseUrl\": \"http://localhost:5000\",\n"
            + "  \"date\": \"2026-11-27\",\n"
            + $"  \"probes\": [{probes}],\n"
            + $"  \"findings\": [{findings}]\n"
            + "}\n");
    }

    static string Probe(string scenario, string result) =>
        "{"
        + $"\"scenario\": \"{scenario}\","
        + " \"useCase\": \"use-case/lending.borrow-copy\","
        + " \"as\": \"person/ava\","
        + " \"method\": \"POST\","
        + " \"path\": \"/api/copies\","
        + " \"status\": 201,"
        + $" \"result\": \"{result}\"}}";

    static string ProbeWithoutResult(string scenario) =>
        "{"
        + $"\"scenario\": \"{scenario}\","
        + " \"useCase\": \"use-case/lending.borrow-copy\","
        + " \"as\": \"person/ava\","
        + " \"method\": \"POST\","
        + " \"path\": \"/api/copies\","
        + " \"status\": 201}";

    static string Failed(string scenario) =>
        "{"
        + " \"code\": \"probe-failed\","
        + $" \"message\": \"Probe '{scenario}' did not pass.\","
        + " \"path\": \".litespec/behavior/modules/lending/aggregates/loans/borrow-copy-probe-proof-scenario.yaml\","
        + " \"line\": 1,"
        + $" \"subject\": \"{scenario}\"}}";

    static string Unreachable(string scenario) =>
        "{"
        + " \"code\": \"probe-unreachable\","
        + $" \"message\": \"Probe '{scenario}' could not reach the host.\","
        + " \"path\": \".litespec/behavior/modules/lending/aggregates/loans/borrow-copy-probe-proof-scenario.yaml\","
        + " \"line\": 1,"
        + $" \"subject\": \"{scenario}\"}}";

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);
        model.Ledger.Save(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static JsonElement? LinkOf(JsonDocument document, string claim)
    {
        foreach (var link in document.RootElement.GetProperty("links").GetProperty("proves").EnumerateArray())
        {
            if (link.GetProperty("claim").GetString() == claim)
            {
                return link;
            }
        }

        return null;
    }

    static bool Verified(JsonElement root) =>
        CompletionOf(root, UseCase).GetProperty("stage").GetString() == "verified";

    static List<string?> HeldBy(JsonElement root) =>
        CompletionOf(root, UseCase).GetProperty("heldBy").EnumerateArray().Select(entry => entry.GetString()).ToList();

    static JsonElement CompletionOf(JsonElement root, string id) =>
        root.GetProperty("records").EnumerateArray()
            .Single(record => record.GetProperty("id").GetString() == id)
            .GetProperty("completion");

    static JsonElement ProofOf(JsonElement root, string id) =>
        root.GetProperty("records").EnumerateArray()
            .Single(record => record.GetProperty("id").GetString() == id)
            .GetProperty("proof");

    static void CommitFixture(string root)
    {
        Assert.Equal(0, ProcessRunner.Git(root, "init").Status);
        Assert.Equal(0, ProcessRunner.Git(root, "config", "user.email", "litespec@example.com").Status);
        Assert.Equal(0, ProcessRunner.Git(root, "config", "user.name", "LiteSpec").Status);
        CommitAll(root, "Record the passing proof.");
    }

    static void CommitAll(string root, string message)
    {
        Assert.Equal(0, ProcessRunner.Git(root, "add", "-A").Status);
        var commit = ProcessRunner.Git(root, "-c", "commit.gpgsign=false", "commit", "-m", message);
        Assert.True(commit.Status == 0, commit.Error);
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    static string ScenarioJson(string then) => $$"""
        {
          "type": "scenario",
          "id": "{{Borrow}}",
          "title": "Borrowing an available copy succeeds",
          "given": "A copy sits available on its shelf.",
          "when": "The reader borrows the copy.",
          "result": "result/borrow-copy.borrowed",
          "then": "{{then}}"
        }
        """;

    static string ScenarioJson(string id, string title, string result) => $$"""
        {
          "type": "scenario",
          "id": "{{id}}",
          "title": "{{title}}",
          "given": "The library holds the copy.",
          "when": "The reader acts on it.",
          "result": "{{result}}",
          "then": "The library answers."
        }
        """;
}
