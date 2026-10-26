using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the term-lookalike check: joined and hyphenated pairs, spelling pairs, and the ignore list.</summary>
public sealed class TermLookalikeCheckTests
{
    [Fact]
    public void Joined_And_Hyphenated_Pair_Reported_When_Both_Occur()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(temp.Path, "soldout.md"), """{"template": "soldout"}"""),
            TestModel.Record("reaction", "reaction/test.send-sold-out-notice-on-sale-ended", Path.Combine(temp.Path, "sold-out.md"), """{"on": ["event/sale.sale-ended"]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-lookalike").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("soldout", finding.Message, StringComparison.Ordinal);
        Assert.Contains("sold-out", finding.Message, StringComparison.Ordinal);
        Assert.Contains("notification/test.send-soldout-notice", finding.Message, StringComparison.Ordinal);
        Assert.Contains("reaction/test.send-sold-out-notice-on-sale-ended", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void One_Form_Alone_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(temp.Path, "soldout.md"), """{"template": "soldout"}"""));

        Assert.DoesNotContain("term-lookalike", TestModel.Codes(model));
    }

    [Fact]
    public void Ignore_List_Honoured()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), """{"vocabulary": {"lookalikeIgnore": ["soldout|sold-out"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(temp.Path, "soldout.md"), """{"template": "soldout"}"""),
            TestModel.Record("reaction", "reaction/test.send-sold-out-notice-on-sale-ended", Path.Combine(temp.Path, "sold-out.md"), """{"on": ["event/sale.sale-ended"]}"""));

        Assert.DoesNotContain("term-lookalike", TestModel.Codes(model));
    }

    [Fact]
    public void British_And_American_Pair_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/test.colour-check", Path.Combine(temp.Path, "colour.md"), """{"kind": "invariant", "statement": "Each flag carries a colour.", "governs": []}"""),
            TestModel.Record("rule", "rule/test.color-check", Path.Combine(temp.Path, "color.md"), """{"kind": "invariant", "statement": "Each flag carries a color.", "governs": []}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-lookalike").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("colour", finding.Message, StringComparison.Ordinal);
        Assert.Contains("color", finding.Message, StringComparison.Ordinal);
        Assert.Contains("rule/test.colour-check", finding.Message, StringComparison.Ordinal);
        Assert.Contains("rule/test.color-check", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Singular_And_Plural_Names_In_One_Home_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/test.copy", Path.Combine(temp.Path, "copy.md"), """{"states": []}"""),
            TestModel.Record("aggregate", "aggregate/test.copies", Path.Combine(temp.Path, "copies.md"), """{"states": []}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-lookalike").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("copy", finding.Message, StringComparison.Ordinal);
        Assert.Contains("copies", finding.Message, StringComparison.Ordinal);
        Assert.Contains("aggregate/test.copy", finding.Message, StringComparison.Ordinal);
        Assert.Contains("aggregate/test.copies", finding.Message, StringComparison.Ordinal);
    }
}
