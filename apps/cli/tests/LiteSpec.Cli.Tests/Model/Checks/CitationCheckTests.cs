using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves a requirement cites its reasons and an applying constraint is cited.</summary>
public sealed class CitationCheckTests
{
    /// <summary>Requirement Without Cites Reports At Tier A.</summary>
    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Requirement_Without_Cites_Reports_At_Tier_A()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "requirement-cites-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' cites no goal, problem, feature, constraint, research or decision.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <summary>Requirement With Cites Passes.</summary>
    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Requirement_With_Cites_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["constraint/library-keeps-records"]}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.yaml"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        Assert.DoesNotContain("requirement-cites-missing", TestModel.Codes(model));
    }

    /// <summary>Applying Constraint Without Cites Reports.</summary>
    /// <proves>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</proves>
    [Fact]
    public void Applying_Constraint_Without_Cites_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["problem/overdue-loans-pile-up"]}"""),
            TestModel.Record("problem", "problem/overdue-loans-pile-up", Path.Combine(temp.Path, "problems.yaml"), """{"affects": ["actor/reader"], "statement": "Overdue loans pile up on the shelves."}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.yaml"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "constraint-unreached");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Constraint 'constraint/library-keeps-records' applies and is cited by no requirement.", finding.Message);
        Assert.Equal("constraint/library-keeps-records", finding.Subject);
    }

    /// <summary>Cited Constraint Passes.</summary>
    /// <proves>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</proves>
    [Fact]
    public void Cited_Constraint_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["constraint/library-keeps-records"]}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.yaml"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""));

        Assert.DoesNotContain("constraint-unreached", TestModel.Codes(model));
    }

    /// <summary>Constraint Not Applying Passes.</summary>
    /// <proves>litespec/core/intent#cite-each-applying-constraint-from-a-requirement</proves>
    [Fact]
    public void Constraint_Not_Applying_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans.", "cites": ["problem/overdue-loans-pile-up"]}"""),
            TestModel.Record("problem", "problem/overdue-loans-pile-up", Path.Combine(temp.Path, "problems.yaml"), """{"affects": ["actor/reader"], "statement": "Overdue loans pile up on the shelves."}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(temp.Path, "charter.yaml"), """{"authority": "Library charter.", "kind": "organizational", "applies": false, "appliesBecause": "The library keeps no records yet."}"""));

        Assert.DoesNotContain("constraint-unreached", TestModel.Codes(model));
    }

    /// <summary>A page citing a removed record reports it.</summary>
    /// <proves>litespec/core/language#resolve-every-identifier-citation</proves>
    [Fact]
    public void Page_Citing_A_Removed_Record_Reports_It()
    {
        using var fixture = ModelFixture.Prepare("large");
        var guides = Path.Combine(fixture.Path, ".litespec", "guides");
        Directory.CreateDirectory(guides);
        File.WriteAllText(
            Path.Combine(guides, "lending.md"),
            "# Lending\n\nSee scenario/borrow-copy.gone-since-the-review for the refusal.\n");

        var result = Cli.Run("model", "check", "--root", fixture.Path);

        Assert.Contains("Citation 'scenario/borrow-copy.gone-since-the-review' resolves to no record.", result.Output);
    }
}
