using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the term-clash check: joined and hyphenated pairs, spelling pairs, and the ignore list.</summary>
public sealed class TermLookalikeCheckTests
{
    /// <summary>Joined And Hyphenated Pair Reported When Both Occur.</summary>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Fact]
    public void Joined_And_Hyphenated_Pair_Reported_When_Both_Occur()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(temp.Path, "soldout.md"), """{"template": "soldout"}"""),
            TestModel.Record("reaction", "reaction/test.send-sold-out-notice-on-sale-ended", Path.Combine(temp.Path, "sold-out.md"), """{"triggeredBy": ["event/sale.sale-ended"]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-clash").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("soldout", finding.Message, StringComparison.Ordinal);
        Assert.Contains("sold-out", finding.Message, StringComparison.Ordinal);
        Assert.Contains("notification/test.send-soldout-notice", finding.Message, StringComparison.Ordinal);
        Assert.Contains("reaction/test.send-sold-out-notice-on-sale-ended", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>One Form Alone Not Reported.</summary>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Fact]
    public void One_Form_Alone_Not_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(temp.Path, "soldout.md"), """{"template": "soldout"}"""));

        Assert.DoesNotContain("term-clash", TestModel.Codes(model));
    }

    /// <summary>Ignore List Honoured.</summary>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Fact]
    public void Ignore_List_Honoured()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), """{"vocabulary": {"lookalikeIgnore": ["soldout|sold-out"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(temp.Path, "soldout.md"), """{"template": "soldout"}"""),
            TestModel.Record("reaction", "reaction/test.send-sold-out-notice-on-sale-ended", Path.Combine(temp.Path, "sold-out.md"), """{"triggeredBy": ["event/sale.sale-ended"]}"""));

        Assert.DoesNotContain("term-clash", TestModel.Codes(model));
    }

    /// <summary>British And American Pair Reported.</summary>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Fact]
    public void British_And_American_Pair_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/test.colour-check", Path.Combine(temp.Path, "colour.md"), """{"kind": "invariant", "statement": "Each flag carries a colour.", "governs": []}"""),
            TestModel.Record("rule", "rule/test.color-check", Path.Combine(temp.Path, "color.md"), """{"kind": "invariant", "statement": "Each flag carries a color.", "governs": []}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-clash").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("colour", finding.Message, StringComparison.Ordinal);
        Assert.Contains("color", finding.Message, StringComparison.Ordinal);
        Assert.Contains("rule/test.colour-check", finding.Message, StringComparison.Ordinal);
        Assert.Contains("rule/test.color-check", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Singular And Plural Names In One Home Reported.</summary>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Fact]
    public void Singular_And_Plural_Names_In_One_Home_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/test.copy", Path.Combine(temp.Path, "copy.md"), """{"states": []}"""),
            TestModel.Record("aggregate", "aggregate/test.copies", Path.Combine(temp.Path, "copies.md"), """{"states": []}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-clash").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("copy", finding.Message, StringComparison.Ordinal);
        Assert.Contains("copies", finding.Message, StringComparison.Ordinal);
        Assert.Contains("aggregate/test.copy", finding.Message, StringComparison.Ordinal);
        Assert.Contains("aggregate/test.copies", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Split Opening With A Function Word Not Reported.</summary>
    /// <param name="split">The hyphenated form the case opens with a function word.</param>
    /// <param name="joined">The joined term the hyphenated form resembles in the case.</param>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Theory]
    [InlineData("as-signed", "assigned")]
    [InlineData("on-line", "online")]
    [InlineData("as-set", "asset")]
    [InlineData("with-hold", "withhold")]
    [InlineData("with-holds", "withholds")]
    // The InlineData values pair a function-word split with its joined lookalike.
    // A split that opens with a function word reads as two words, not one compound.
    public void Split_Opening_With_A_Function_Word_Not_Reported(string split, string joined)
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", $"notification/test.send-{split}-notice", Path.Combine(temp.Path, "split.md"), """{"template": "notice"}"""),
            TestModel.Record("notification", $"notification/test.send-{joined}-notice", Path.Combine(temp.Path, "joined.md"), """{"template": "notice"}"""));

        Assert.DoesNotContain("term-clash", TestModel.Codes(model));
    }

    /// <summary>Real Compound Split Still Reported.</summary>
    /// <param name="split">The hyphenated spelling of the compound term in the case.</param>
    /// <param name="joined">The joined spelling of the compound term in the case.</param>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Theory]
    [InlineData("claw-back", "clawback")]
    [InlineData("sign-up", "signup")]
    [InlineData("take-down", "takedown")]
    [InlineData("line-up", "lineup")]
    // The InlineData values pair a real compound split with its joined form.
    // A real compound split still counts as a lookalike of the joined term.
    public void Real_Compound_Split_Still_Reported(string split, string joined)
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", $"notification/test.send-{split}-notice", Path.Combine(temp.Path, "split.md"), """{"template": "notice"}"""),
            TestModel.Record("notification", $"notification/test.send-{joined}-notice", Path.Combine(temp.Path, "joined.md"), """{"template": "notice"}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-clash").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains(joined, finding.Message, StringComparison.Ordinal);
        Assert.Contains(split, finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Behaviour And Behavior Still Reported.</summary>
    /// <proves>litespec/core/authoring#review-each-term-for-its-lookalikes</proves>
    [Fact]
    public void Behaviour_And_Behavior_Still_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/test.behaviour-check", Path.Combine(temp.Path, "behaviour.md"), """{"kind": "invariant", "statement": "Each flag carries a behaviour.", "governs": []}"""),
            TestModel.Record("rule", "rule/test.behavior-check", Path.Combine(temp.Path, "behavior.md"), """{"kind": "invariant", "statement": "Each flag carries a behavior.", "governs": []}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "term-clash").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("behaviour", finding.Message, StringComparison.Ordinal);
        Assert.Contains("behavior", finding.Message, StringComparison.Ordinal);
    }
}
