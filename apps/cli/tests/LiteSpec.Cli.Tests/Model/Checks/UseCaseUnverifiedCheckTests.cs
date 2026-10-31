using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the use-case-unverified check against hand-built models.</summary>
public sealed class UseCaseUnverifiedCheckTests
{
    /// <summary>Use Case With Unproved Scenarios Reports.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void Use_Case_With_Unproved_Scenarios_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow-copy.md"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));
        model.Status["use-case/lending.borrow-copy"] = new UseCaseStatus(Implemented: true, Verified: false);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "use-case-unverified");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' has scenarios with no passing proof.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Verified Use Case Passes.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void Verified_Use_Case_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow-copy.md"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));
        model.Status["use-case/lending.borrow-copy"] = new UseCaseStatus(Implemented: true, Verified: true);

        Assert.DoesNotContain("use-case-unverified", TestModel.Codes(model));
    }

    /// <summary>Use Case Without Scenarios Passes.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void Use_Case_Without_Scenarios_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.md"), """{"kind": "command"}"""));
        model.Status["use-case/lending.borrow-copy"] = new UseCaseStatus(Implemented: true, Verified: false);

        Assert.DoesNotContain("use-case-unverified", TestModel.Codes(model));
    }

    /// <summary>Use Case Without Status Passes.</summary>
    /// <proves>litespec/core/model#report-unverified-use-cases</proves>
    [Fact]
    public void Use_Case_Without_Status_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(temp.Path, "borrow-copy.md"), """{"given": "An available copy.", "when": "The reader borrows the copy.", "then": "The loan is active.", "result": "result/borrow-copy.borrowed"}"""));

        Assert.DoesNotContain("use-case-unverified", TestModel.Codes(model));
    }
}
