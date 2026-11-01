using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the proof-invalid check against hand-built models.</summary>
public sealed class ProofInvalidCheckTests
{
    const string TestName = "Library.Acceptance.Tests.Proof.ProofScenarios.Borrowing_an_available_copy_succeeds";
    const string Scenario = "scenario/borrow-copy.borrowing-an-available-copy-succeeds";

    /// <summary>Flaky Test Reports.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Flaky_Test_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "ProofScenarios.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("scenario", Scenario, Path.Combine(temp.Path, "borrow-copy.md"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));
        model.TestRun = new TestRun();
        model.TestRun.Flaky.Add(TestName);
        model.Tests.Add(new ScenarioInfo(
            TestName,
            "ProofScenarios",
            file,
            12,
            [Scenario],
            [],
            [],
            [],
            new HashSet<string>(StringComparer.Ordinal),
            [],
            [],
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal)));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "proof-invalid");
        Assert.Equal("A", finding.Tier);
        Assert.Equal($"Test '{TestName}' passed and failed within the runs read.", finding.Message);
        Assert.Equal(Scenario, finding.Subject);
        Assert.Equal(file, finding.Path);
        Assert.Equal(12, finding.Line);
    }

    /// <summary>Steady Test Passes.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Steady_Test_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = Path.Combine(temp.Path, "ProofScenarios.cs");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("scenario", Scenario, Path.Combine(temp.Path, "borrow-copy.md"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));
        model.TestRun = new TestRun();
        model.Tests.Add(new ScenarioInfo(
            TestName,
            "ProofScenarios",
            file,
            12,
            [Scenario],
            [],
            [],
            [],
            new HashSet<string>(StringComparer.Ordinal),
            [],
            [],
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal)));

        Assert.DoesNotContain("proof-invalid", TestModel.Codes(model));
    }

    /// <summary>Missing Run Passes.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Missing_Run_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("scenario", Scenario, Path.Combine(temp.Path, "borrow-copy.md"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));

        Assert.Null(model.TestRun);
        Assert.DoesNotContain("proof-invalid", TestModel.Codes(model));
    }
}
