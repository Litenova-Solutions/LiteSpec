using LiteSpec.Model;
using LiteSpec.Model.Diff;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Diff;

/// <summary>Proves a proving test method changed without its scenario reports proof-hash-drift as advisory, and only on the base comparison.</summary>
public sealed class ProofChangesTests
{
    const string Scenario = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";
    const string TestName = "Library.Acceptance.Tests.Proof.ProofScenarios.Borrowing_an_available_copy_succeeds";
    const string ScenarioPath = ".litespec/behavior/modules/lending/loans/borrow-copy-proof-scenario.yaml";
    const string ProofPath = "apps/api/tests/Library.Acceptance.Tests/Proof/ProofScenarios.cs";

    /// <summary>A proving test method edited while its scenario held still reports proof-hash-drift as an advisory finding on the scenario.</summary>
    /// <proves>litespec/core/agent#protect-proofs-from-implementation-edits</proves>
    [Fact]
    public void Test_Method_Changed_Without_Scenario_Reports_Advisory_Finding()
    {
        using var temp = ModelFixture.Prepare("small");
        var baseModel = Prove(temp.Path);

        Write(temp.Path, ProofPath, ProofFile.Replace("RegisterACopy(\"copy-1\")", "RegisterACopy(\"copy-2\")", StringComparison.Ordinal));

        var head = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var finding = Assert.Single(ProofChanges.Find(baseModel, head));

        Assert.Equal("proof-hash-drift", finding.Code);
        Assert.Equal("A", finding.Tier);
        Assert.Equal(Scenario, finding.Subject);
        Assert.Equal(Path.Combine(temp.Path, ScenarioPath.Replace('/', Path.DirectorySeparatorChar)), finding.Path);
    }

    /// <summary>A proving test method edited together with its scenario reports nothing, since the scenario is the change it proves.</summary>
    /// <proves>litespec/core/agent#protect-proofs-from-implementation-edits</proves>
    [Fact]
    public void Test_Method_Changed_With_Scenario_Reports_Nothing()
    {
        using var temp = ModelFixture.Prepare("small");
        var baseModel = Prove(temp.Path);

        Write(temp.Path, ProofPath, ProofFile.Replace("RegisterACopy(\"copy-1\")", "RegisterACopy(\"copy-2\")", StringComparison.Ordinal));
        Write(temp.Path, ScenarioPath, ScenarioJson("The loan is active for three weeks."));

        var head = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Empty(ProofChanges.Find(baseModel, head));
    }

    /// <summary>An edit elsewhere in the proving test's file, outside its method, reports nothing.</summary>
    /// <proves>litespec/core/agent#protect-proofs-from-implementation-edits</proves>
    [Fact]
    public void Edit_Elsewhere_In_The_Test_File_Reports_Nothing()
    {
        using var temp = ModelFixture.Prepare("small");
        var baseModel = Prove(temp.Path);

        Write(temp.Path, ProofPath, ProofFile.Replace("namespace Library.Acceptance.Tests.Proof;", "namespace Library.Acceptance.Tests.Proof;\n\n// An unrelated note.", StringComparison.Ordinal));

        var head = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.Empty(ProofChanges.Find(baseModel, head));
    }

    /// <summary>The check only runs against a base: a head build alone reports no proof-hash-drift.</summary>
    /// <proves>litespec/core/agent#protect-proofs-from-implementation-edits</proves>
    [Fact]
    public void Without_A_Base_Nothing_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Prove(temp.Path);

        Write(temp.Path, ProofPath, ProofFile.Replace("RegisterACopy(\"copy-1\")", "RegisterACopy(\"copy-2\")", StringComparison.Ordinal));

        var head = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.DoesNotContain(Checker.Check(head).Findings, finding => finding.Code == "proof-hash-drift");
    }

    /// <summary>A passing run records the proving test method hash in the ledger beside the scenario and test hashes.</summary>
    /// <proves>litespec/core/agent#protect-proofs-from-implementation-edits</proves>
    [Fact]
    public void Passing_Run_Records_The_Method_Hash()
    {
        using var temp = ModelFixture.Prepare("small");
        Prove(temp.Path);

        using var ledger = System.Text.Json.JsonDocument.Parse(File.ReadAllText(
            Path.Combine(temp.Path, ".litespec", "proof", "proofs.json")));
        var entry = ledger.RootElement.GetProperty(Scenario);

        Assert.Matches("^[0-9a-f]{16}$", entry.GetProperty("method").GetString()!);
    }

    /// <summary>Writes the scenario and proving test, reads one passing run, saves the ledger, and returns the built base model.</summary>
    /// <param name="root">The fixture folder to write into.</param>
    /// <returns>The base model whose ledger holds the recorded proof.</returns>
    static ProjectModel Prove(string root)
    {
        Write(root, ScenarioPath, ScenarioJson("The loan is active."));
        Write(root, ProofPath, ProofFile);
        WriteTrx(root, "run-1");

        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);
        model.Ledger.Save(config);

        return model;
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
