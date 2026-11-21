using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Proof;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the ledger records the run commit and outcome per scenario, and a damaged ledger reports.</summary>
public sealed class ProofLedgerTests
{
    const string Scenario = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";

    /// <summary>A run passing the proof for the first time records its commit and reads fresh.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void NewRunRecordsCommitAndReadsFresh()
    {
        using var temp = TestCli.Temp("proof");
        var config = ConsumerConfig.Load(temp.Path);
        var ledger = ProofLedger.Empty();

        Assert.False(ledger.Stale(Scenario, "run-1", "commit-1", new ProofFreshness(config)));
        Assert.True(ledger.Changed);

        var entry = ledger.Entries[Scenario];

        Assert.Equal("commit-1", entry.Commit);
        Assert.Equal("Passed", entry.Outcome);
        Assert.Equal("run-1", entry.Run);
    }

    /// <summary>The same run read again without git reads stale, since no commit can be compared.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void SameRunWithoutGitReadsStale()
    {
        using var temp = TestCli.Temp("proof");
        var config = ConsumerConfig.Load(temp.Path);
        var ledger = ProofLedger.Empty();
        var freshness = new ProofFreshness(config);

        Assert.False(ledger.Stale(Scenario, "run-1", "", freshness));
        Assert.True(ledger.Stale(Scenario, "run-1", "", freshness));
    }

    /// <summary>The ledger saves the commit, the outcome and the run per scenario, and reads them back.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void LedgerSavesCommitOutcomeAndRun()
    {
        using var temp = TestCli.Temp("proof");
        var config = ConsumerConfig.Load(temp.Path);
        var ledger = ProofLedger.Empty();
        ledger.Stale(Scenario, "run-1", "commit-1", new ProofFreshness(config));
        ledger.Save(config);

        using var saved = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(temp.Path, ".litespec", "proof", "proofs.json")));
        var written = saved.RootElement.GetProperty(Scenario);

        Assert.Equal("commit-1", written.GetProperty("commit").GetString());
        Assert.Equal("Passed", written.GetProperty("outcome").GetString());
        Assert.Equal("run-1", written.GetProperty("run").GetString());
        Assert.False(written.TryGetProperty("scenario", out _));
        Assert.False(written.TryGetProperty("test", out _));
        Assert.False(written.TryGetProperty("method", out _));
        Assert.False(written.TryGetProperty("marks", out _));

        var loaded = ProofLedger.Load(config);

        Assert.Empty(loaded.Problems);

        var entry = loaded.Entries[Scenario];

        Assert.Equal("commit-1", entry.Commit);
        Assert.Equal("Passed", entry.Outcome);
        Assert.Equal("run-1", entry.Run);
    }

    /// <summary>A ledger entry without an outcome reads as passed, from its commit and run.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void LedgerEntryWithoutOutcomeReadsAsPassed()
    {
        using var temp = TestCli.Temp("proof");
        var folder = Path.Combine(temp.Path, ".litespec", "proof");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "proofs.json"),
            $$"""{ "{{Scenario}}": { "commit": "commit-1", "run": "run-1" } }""");

        var ledger = ProofLedger.Load(ConsumerConfig.Load(temp.Path));

        Assert.Empty(ledger.Problems);

        var entry = ledger.Entries[Scenario];

        Assert.Equal("commit-1", entry.Commit);
        Assert.Equal("Passed", entry.Outcome);
        Assert.Equal("run-1", entry.Run);
    }

    /// <summary>A missing ledger file reads as an empty ledger with no problems.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void MissingLedgerLoadsEmpty()
    {
        using var temp = TestCli.Temp("proof");

        var ledger = ProofLedger.Load(ConsumerConfig.Load(temp.Path));

        Assert.Empty(ledger.Entries);
        Assert.Empty(ledger.Problems);
        Assert.False(ledger.Changed);
    }

    /// <summary>A ledger that does not parse reports a model problem.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void LedgerThatDoesNotParseReportsAProblem()
    {
        using var temp = TestCli.Temp("proof");
        var folder = Path.Combine(temp.Path, ".litespec", "proof");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "proofs.json"), "{ not json\n");

        var ledger = ProofLedger.Load(ConsumerConfig.Load(temp.Path));

        Assert.Contains(ledger.Problems, problem => problem.Code == "record-invalid");
    }
}
