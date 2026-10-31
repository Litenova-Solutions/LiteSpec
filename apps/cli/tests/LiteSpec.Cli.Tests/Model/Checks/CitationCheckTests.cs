using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves a requirement cites its reasons and an applying constraint is cited.</summary>
public sealed class CitationCheckTests
{
    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Requirement_Without_Cites_Reports_At_Tier_A()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "requirement-cites-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' cites no goal, problem, feature, constraint, research or decision.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Requirement_Without_Cites_And_Risk_Reports_At_Tier_G()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "risk": ["money"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "requirement-cites-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' cites no goal, problem, feature, constraint, research or decision.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Requirement_With_Cites_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["constraint/library-keeps-records"]}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.md"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        Assert.DoesNotContain("requirement-cites-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Retired_Requirement_Without_Cites_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"status": "retired", "kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.DoesNotContain("requirement-cites-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</proves>
    [Fact]
    public void Applying_Constraint_Without_Cites_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["problem/overdue-loans-pile-up"]}"""),
            TestModel.Record("problem", "problem/overdue-loans-pile-up", Path.Combine(temp.Path, "problems.md"), """{"affects": ["actor/reader"], "statement": "Overdue loans pile up on the shelves."}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.md"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "constraint-unreached");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Constraint 'constraint/library-keeps-records' applies and is cited by no requirement.", finding.Message);
        Assert.Equal("constraint/library-keeps-records", finding.Subject);
    }

    /// <proves>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</proves>
    [Fact]
    public void Cited_Constraint_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["constraint/library-keeps-records"]}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.md"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        Assert.DoesNotContain("constraint-unreached", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</proves>
    [Fact]
    public void Constraint_Not_Applying_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["problem/overdue-loans-pile-up"]}"""),
            TestModel.Record("problem", "problem/overdue-loans-pile-up", Path.Combine(temp.Path, "problems.md"), """{"affects": ["actor/reader"], "statement": "Overdue loans pile up on the shelves."}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.md"), """{"authority": "Library charter.", "kind": "organizational", "applies": false, "appliesBecause": "The library keeps no records yet."}"""));

        Assert.DoesNotContain("constraint-unreached", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</proves>
    [Fact]
    public void Retired_Applying_Constraint_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.md"), """{"status": "retired", "authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        Assert.DoesNotContain("constraint-unreached", TestModel.Codes(model));
    }
}
