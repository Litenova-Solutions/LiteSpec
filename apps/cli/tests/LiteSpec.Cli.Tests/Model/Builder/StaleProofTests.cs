using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves a scenario changed after its proof passed reads stale until a newer run passes it.</summary>
public sealed class StaleProofTests
{
    const string Scenario = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";
    const string UseCase = "use-case/lending.borrow-copy";
    const string TestName = "Library.Acceptance.Tests.Proof.ProofScenarios.Borrowing_an_available_copy_succeeds";

    /// <summary>A scenario unchanged since the run first passed its proof stays verified.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Unchanged_Scenario_Stays_Verified_Across_Builds()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");

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

    /// <summary>The ledger records the scenario hash and the test hash at the last passing run.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Ledger_Records_Scenario_And_Test_Hashes()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");

        Build(temp.Path).Dispose();

        using var ledger = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(temp.Path, ".litespec", "proof", "proofs.json")));
        var entry = ledger.RootElement.GetProperty(Scenario);

        // The builder always writes the three hash strings for a recorded proof.
        Assert.NotEmpty(entry.GetProperty("scenario").GetString()!);
        Assert.NotEmpty(entry.GetProperty("test").GetString()!);
        Assert.NotEmpty(entry.GetProperty("run").GetString()!);
    }

    /// <summary>A scenario whose semantic fields change after its proof passed reads stale and unverifies its use case.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Scenario_Changed_After_Its_Proof_Passed_Reads_Stale()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");

        using (var first = Build(temp.Path))
        {
            Assert.True(Verified(first.RootElement));
        }

        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active for three weeks."));

        using var changed = Build(temp.Path);

        Assert.False(Verified(changed.RootElement));
        Assert.Contains(Scenario, HeldBy(changed.RootElement));
    }

    /// <summary>A test changed after its proof passed reads stale and unverifies its use case.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Test_Changed_After_Its_Proof_Passed_Reads_Stale()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");

        using (var first = Build(temp.Path))
        {
            Assert.True(Verified(first.RootElement));
        }

        Write(temp.Path, ProofPath, ProofFile.Replace("copy-1", "copy-2", StringComparison.Ordinal));

        using var changed = Build(temp.Path);

        Assert.False(Verified(changed.RootElement));
        Assert.Contains(Scenario, HeldBy(changed.RootElement));
    }

    /// <summary>A newer passing run proves the changed scenario again and clears the stale proof.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Newer_Run_Clears_A_Stale_Proof()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active."));
        Write(temp.Path, ProofPath, ProofFile);
        WriteTrx(temp.Path, "run-1");

        Build(temp.Path).Dispose();
        TestRecords.Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active for three weeks."));
        Build(temp.Path).Dispose();
        WriteTrx(temp.Path, "run-2");

        using var rerun = Build(temp.Path);

        Assert.True(Verified(rerun.RootElement));
        Assert.Empty(HeldBy(rerun.RootElement));
    }

    const string ScenarioPath = ".litespec/behavior/modules/lending/loans/borrow-copy-proof-scenario.yaml";
    const string ProofPath = "apps/api/tests/Library.Acceptance.Tests/Proof/ProofScenarios.cs";

    static bool Verified(JsonElement root) =>
        CompletionOf(root, UseCase).GetProperty("stage").GetString() == "proven";

    static List<string?> HeldBy(JsonElement root) =>
        CompletionOf(root, UseCase).GetProperty("heldBy").EnumerateArray().Select(entry => entry.GetString()).ToList();

    static JsonElement CompletionOf(JsonElement root, string id) =>
        root.GetProperty("records").EnumerateArray()
            .Single(record => record.GetProperty("id").GetString() == id)
            .GetProperty("completion");

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
