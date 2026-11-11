using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the use-case-unverified check against hand-built models.</summary>
public sealed class UseCaseUnverifiedCheckTests
{
    /// <summary>Use Case With Unproved Scenarios Reports.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void UseCaseWithUnprovedScenariosReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));
        model.Completion["use-case/lending.borrow-copy"] = new Completion("coded", "known", [], new Dictionary<string, CompletionCount>());

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "use-case-unverified");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' has scenarios with no passing proof.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Verified Use Case Passes.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void VerifiedUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));
        model.Completion["use-case/lending.borrow-copy"] = new Completion("verified", "known", [], new Dictionary<string, CompletionCount>());

        Assert.DoesNotContain("use-case-unverified", TestModel.Codes(model));
    }

    /// <summary>Use Case Without Scenarios Passes.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void UseCaseWithoutScenariosPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"kind": "command"}"""));
        model.Completion["use-case/lending.borrow-copy"] = new Completion("coded", "known", [], new Dictionary<string, CompletionCount>());

        Assert.DoesNotContain("use-case-unverified", TestModel.Codes(model));
    }

    /// <summary>Use Case Without Status Passes.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void UseCaseWithoutStatusPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));

        Assert.DoesNotContain("use-case-unverified", TestModel.Codes(model));
    }
}
