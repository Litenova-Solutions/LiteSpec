using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves rule-statement-wide reports a statement past 600 characters and passes one within it.</summary>
public sealed class RuleStatementLengthTests
{
    /// <summary>A statement of exactly 600 characters passes.</summary>
    /// <proves>litespec/core/domain-model#keep-each-rule-statement-short</proves>
    [Fact]
    public void StatementAtTheLimitPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModelWith(temp.Path, Statement(600));

        Assert.DoesNotContain("rule-statement-wide", TestModel.Codes(model));
    }

    /// <summary>A statement of 601 characters reports, naming the rule and its length.</summary>
    /// <proves>litespec/core/domain-model#keep-each-rule-statement-short</proves>
    [Fact]
    public void StatementPastTheLimitReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModelWith(temp.Path, Statement(601));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "rule-statement-wide");

        Assert.Equal("Rule 'rule/copy.loaned-once' states 601 characters, more than 600; split it into one rule per condition.", finding.Message);
        Assert.Equal("rule/copy.loaned-once", finding.Subject);
        Assert.Equal("R", FindingCodes.Of(finding.Code).Level);
    }

    /// <summary>A model with one rule holding the statement and the use case it governs.</summary>
    /// <param name="root">The temporary consumer root.</param>
    /// <param name="statement">The rule statement.</param>
    /// <returns>The model.</returns>
    static ProjectModel ModelWith(string root, string statement) =>
        TestModel.Model(
            TestModel.Config(root),
            TestModel.Record(
                "rule",
                "rule/copy.loaned-once",
                Path.Combine(root, "copy.yaml"),
                $$"""{"kind": "invariant", "statement": "{{statement}}", "governs": ["use-case/lending.borrow-copy"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(root, "borrow.yaml"), """{"kind": "command"}"""));

    /// <summary>A statement of the given length: a copy is loaned once, padded with a repeated scope clause.</summary>
    /// <param name="length">The length in characters.</param>
    /// <returns>The statement.</returns>
    static string Statement(int length)
    {
        const string opening = "A copy is loaned to one member at a time.";
        return opening + new string('x', length - opening.Length);
    }
}
