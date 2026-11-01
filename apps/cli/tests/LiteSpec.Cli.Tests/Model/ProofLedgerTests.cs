using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves test hashes ignore checkout path and line endings, and a damaged ledger reports.</summary>
public sealed class ProofLedgerTests
{
    const string Scenario = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";
    const string TestTitle = "Borrowing an available copy succeeds";
    const string TestFile = "tests/Proof.cs";

    /// <summary>The same test file with CRLF and LF content hashes the same.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Same_Test_With_Crlf_And_Lf_Hashes_The_Same()
    {
        using var temp = Cli.Temp("proof");
        WriteTest(temp.Path, "first line\nsecond line\n");
        var model = Model(temp.Path);
        var lf = ProofLedger.TestHashOf(model, Scenario);

        WriteTest(temp.Path, "first line\r\nsecond line\r\n");

        Assert.Equal(lf, ProofLedger.TestHashOf(model, Scenario));
    }

    /// <summary>The same test file under another project root hashes the same.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Same_Test_Under_Another_Root_Hashes_The_Same()
    {
        using var first = Cli.Temp("proof");
        using var second = Cli.Temp("proof");
        WriteTest(first.Path, "first line\nsecond line\n");
        WriteTest(second.Path, "first line\nsecond line\n");

        Assert.Equal(
            ProofLedger.TestHashOf(Model(first.Path), Scenario),
            ProofLedger.TestHashOf(Model(second.Path), Scenario));
    }

    /// <summary>A ledger that does not parse reports a model problem.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Ledger_That_Does_Not_Parse_Reports_A_Problem()
    {
        using var temp = Cli.Temp("proof");
        var folder = Path.Combine(temp.Path, "docs", "proof");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "proofs.json"), "{ not json\n");

        var ledger = ProofLedger.Load(ConsumerConfig.Load(temp.Path));

        Assert.Contains(ledger.Problems, problem => problem.Code == "record-invalid");
    }

    static ProjectModel Model(string root)
    {
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };
        model.Proves.Add(new ProvesLink(TestTitle, Scenario, true, true, false));
        model.BrowserTests.Add(new BrowserTestInfo("web", TestTitle, TestFile, 1, [], null, [], [], []));

        return model;
    }

    static void WriteTest(string root, string text)
    {
        var path = Path.Combine(root, TestFile.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
