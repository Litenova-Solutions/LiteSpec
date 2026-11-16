using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Builder;

/// <summary>Proves a proof stays fresh at its run commit and goes stale when code changes, until a newer run passes it.</summary>
public sealed class StaleProofTests
{
    const string Scenario = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";
    const string UseCase = "use-case/lending.borrow-copy";
    const string TestName = "Library.Acceptance.Tests.Proof.ProofScenarios.Borrowing_an_available_copy_succeeds";

    /// <summary>A scenario unchanged since the run first passed its proof stays verified.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UnchangedScenarioStaysVerifiedAcrossBuilds()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");
        CommitFixture(temp.Path);

        using (var first = Build(temp.Path))
        {
            Assert.True(Verified(first.RootElement));
        }

        Assert.True(File.Exists(Path.Combine(temp.Path, ".litespec", "proof", "proofs.json")));
        Assert.False(File.Exists(Path.Combine(temp.Path, ".litespec/model", "proofs.json")));

        using var second = Build(temp.Path);

        Assert.True(Verified(second.RootElement));
        Assert.Empty(HeldBy(second.RootElement));
    }

    /// <summary>The ledger records the run commit, the outcome and the run at the last passing run.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void LedgerRecordsCommitOutcomeAndRun()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");
        CommitFixture(temp.Path);

        Build(temp.Path).Dispose();

        using var ledger = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(temp.Path, ".litespec", "proof", "proofs.json")));
        var entry = ledger.RootElement.GetProperty(Scenario);

        // The builder writes the run commit, the outcome and the run for a recorded proof, and no hashes.
        Assert.Equal(HeadOf(temp.Path), entry.GetProperty("commit").GetString());
        Assert.Equal("Passed", entry.GetProperty("outcome").GetString());
        Assert.Matches("^[0-9a-f]{16}$", entry.GetProperty("run").GetString()!);
        Assert.False(entry.TryGetProperty("scenario", out _));
        Assert.False(entry.TryGetProperty("test", out _));
        Assert.False(entry.TryGetProperty("method", out _));
        Assert.False(entry.TryGetProperty("marks", out _));
    }

    /// <summary>A scenario changed without its code keeps its proof fresh, since only a record path changed.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ScenarioChangedWithoutItsCodeStaysVerified()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");
        CommitFixture(temp.Path);

        using (var first = Build(temp.Path))
        {
            Assert.True(Verified(first.RootElement));
        }

        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active for three weeks."));
        CommitAll(temp.Path, "Change the scenario text.");

        using var changed = Build(temp.Path);

        Assert.True(Verified(changed.RootElement));
        Assert.Empty(HeldBy(changed.RootElement));
    }

    /// <summary>A test changed after its proof passed reads stale and unverifies its use case.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void TestChangedAfterItsProofPassedReadsStale()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");
        CommitFixture(temp.Path);
        var passedAt = HeadOf(temp.Path);

        using (var first = Build(temp.Path))
        {
            Assert.True(Verified(first.RootElement));
        }

        Write(temp.Path, ProofPath, ProofFile.Replace("copy-1", "copy-2", StringComparison.Ordinal));
        CommitAll(temp.Path, "Change the proving test.");

        using var changed = Build(temp.Path);

        Assert.False(Verified(changed.RootElement));
        Assert.Contains(Scenario, HeldBy(changed.RootElement));

        var proof = ProofOf(changed.RootElement, Scenario);

        Assert.Equal("stale", proof.GetProperty("status").GetString());
        Assert.Equal(passedAt[..7], proof.GetProperty("lastPassedAt").GetString());
        Assert.Equal(1, proof.GetProperty("commitsBehind").GetInt32());
        Assert.Equal("files outside the record and documentation paths changed", proof.GetProperty("staleReason").GetString());
    }

    /// <summary>A newer passing run proves the changed code again and clears the stale proof.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void NewerRunClearsAStaleProof()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");
        CommitFixture(temp.Path);

        Build(temp.Path).Dispose();
        Write(temp.Path, ProofPath, ProofFile.Replace("copy-1", "copy-2", StringComparison.Ordinal));
        CommitAll(temp.Path, "Change the proving test.");

        using (var stale = Build(temp.Path))
        {
            Assert.False(Verified(stale.RootElement));
        }

        WriteTrx(temp.Path, "run-2");

        using var rerun = Build(temp.Path);

        Assert.True(Verified(rerun.RootElement));
        Assert.Empty(HeldBy(rerun.RootElement));
    }

    const string ScenarioPath = ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy-proof-scenario.yaml";
    const string ProofPath = "apps/api/tests/Library.Acceptance.Tests/Proof/ProofScenarios.cs";

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

    static void WriteTrx(string root, string runName)
    {
        var folder = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "results.trx"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + $"<TestRun name=\"{runName}\" xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">\n"
            + "  <Results>\n"
            + $"    <UnitTestResult testName=\"{TestName}\" outcome=\"Passed\" />\n"
            + "  </Results>\n"
            + "</TestRun>\n");
    }

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);
        model.Ledger.Save(config);

        return JsonDocument.Parse(model.ToJson());
    }

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

    static string HeadOf(string root)
    {
        var head = ProcessRunner.Git(root, "rev-parse", "HEAD");
        Assert.True(head.Status == 0, head.Error);

        return head.Stdout.Trim();
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
          "id": "{{Scenario}}",
          "title": "Borrowing an available copy succeeds",
          "given": "A copy sits available on its shelf.",
          "when": "The reader borrows the copy.",
          "result": "result/borrow-copy.borrowed",
          "then": "{{then}}"
        }
        """;

    const string ProofFile = """
        namespace Library.Acceptance.Tests.Proof;

        public sealed class ProofScenarios
        {
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            public Task Borrowing_an_available_copy_succeeds() => Runner.RunScenarioAsync(
                given => new RegisterCopySteps().RegisterACopy("copy-1"),
                when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                then => new BorrowCopySteps().TheLoanIsActive());
        }
        """;
}
