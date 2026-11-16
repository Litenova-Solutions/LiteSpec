using System.Text.Json;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check</c> writes the proof ledger only with <c>--record-proof</c>, after the check, and keeps the exit code of the plain check.</summary>
public sealed class ModelCheckRecordProofTests
{
    const string Scenario = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";
    const string TestName = "Library.Acceptance.Tests.Proof.ProofScenarios.Borrowing_an_available_copy_succeeds";

    /// <summary>A check without the flag creates no ledger file, although the run passes a proof.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void CheckWithoutFlagCreatesNoLedger()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteProof(fixture.Path);
        var ledger = Path.Combine(fixture.Path, ".litespec", "proof", "proofs.json");

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.NotEqual(2, result.Status);
        Assert.False(File.Exists(ledger));
    }

    /// <summary>A check without the flag leaves a written ledger byte for byte as it was.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void CheckWithoutFlagLeavesWrittenLedgerUnchanged()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteProof(fixture.Path);
        var ledger = Path.Combine(fixture.Path, ".litespec", "proof", "proofs.json");
        Directory.CreateDirectory(Path.GetDirectoryName(ledger)!);
        File.WriteAllText(ledger, """{"scenario/library.quiet-close":{"commit":"abc","outcome":"Passed","run":"run-0"}}""" + "\n");
        var before = File.ReadAllBytes(ledger);

        var result = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.NotEqual(2, result.Status);
        Assert.Equal(before, File.ReadAllBytes(ledger));
    }

    /// <summary>A check with the flag writes the ledger and keeps the exit code of the plain check.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void CheckWithFlagWritesLedgerAndKeepsExitCode()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteProof(fixture.Path);
        var ledger = Path.Combine(fixture.Path, ".litespec", "proof", "proofs.json");

        var plain = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.NotEqual(2, plain.Status);
        Assert.False(File.Exists(ledger));

        var recorded = TestCli.Run("model", "check", "--root", fixture.Path, "--record-proof");

        Assert.Equal(plain.Status, recorded.Status);
        Assert.True(File.Exists(ledger), recorded.Output);

        using var saved = JsonDocument.Parse(File.ReadAllText(ledger));
        var entry = saved.RootElement.GetProperty(Scenario);

        Assert.Equal("Passed", entry.GetProperty("outcome").GetString());
        Assert.Matches("^[0-9a-f]{16}$", entry.GetProperty("run").GetString()!);
    }

    const string ScenarioPath = ".litespec/behavior/modules/lending/aggregates/loans/borrow-copy-proof-scenario.yaml";
    const string ProofPath = "apps/api/tests/Library.Acceptance.Tests/Proof/ProofScenarios.cs";

    static void WriteProof(string root)
    {
        TestRecords.Write(root, ScenarioPath, ScenarioJson("The loan is active."));
        Write(root, ProofPath, ProofFile);
        WriteTrx(root, "run-1");
    }

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
