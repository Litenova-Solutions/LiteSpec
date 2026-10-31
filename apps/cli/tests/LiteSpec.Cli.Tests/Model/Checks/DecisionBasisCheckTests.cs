using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves a technical decision points up to what it serves, and a product decision does not.</summary>
public sealed class DecisionBasisCheckTests
{
    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Technical_Decision_Naming_Requirement_Passes_Links()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.md"), """{"kind": "functional", "statement": "A shopper takes a receipt on request."}"""),
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), DecisionJson("requirement/checkout.receipt-on-request")));

        Assert.DoesNotContain("field-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Product_Decision_Naming_Requirement_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.md"), """{"kind": "functional", "statement": "A shopper takes a receipt on request."}"""),
            TestModel.Record("decision", "decision/0001-receipt-rule", ProductDecisionFile(config), DecisionJson("requirement/checkout.receipt-on-request")));

        Assert.True(Reports(model, "field-invalid", "links basedOn to 'requirement/checkout.receipt-on-request', which is a requirement"));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Requirement_Naming_Requirement_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.md"), """{"kind": "functional", "statement": "A shopper takes a receipt on request.", "cites": ["requirement/checkout.two-second-answer"]}"""),
            TestModel.Record("requirement", "requirement/checkout.two-second-answer", Path.Combine(temp.Path, "checkout.md"), """{"kind": "functional", "statement": "Checkout answers within two seconds at peak."}"""));

        Assert.True(Reports(model, "field-invalid", "links cites to 'requirement/checkout.two-second-answer', which is a requirement"));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Technical_Decision_With_Empty_Or_Missing_BasedOn_Reports_Without_Basis()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), """{"status": "approved", "options": [{"name": "Documents", "pros": "Reads stay simple.", "cons": "Queries need an index."}], "choice": "Documents", "reopenedWhen": "Queries pass one hundred a day.", "basedOn": []}"""),
            TestModel.Record("decision", "decision/0003-loan-cache-region", TechnicalDecisionFile(config, "0003-loan-cache-region"), """{"status": "approved", "options": [{"name": "Edge cache", "pros": "Reads stay near the reader.", "cons": "Invalidation lags."}], "choice": "Edge cache", "reopenedWhen": "Reads pass one thousand a day."}"""));

        var basis = Checker.Check(model).Findings.Where(finding => finding.Code == "decision-basis-missing").ToList();

        Assert.Equal(2, basis.Count);
        Assert.All(basis, finding => Assert.Contains("names no constraint or requirement in basedOn", finding.Message, StringComparison.Ordinal));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Technical_Decision_Naming_Constraint_Passes_Basis()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(temp.Path, "retention.md"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""),
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), DecisionJson("constraint/receipts-stay-in-the-eu")));

        Assert.DoesNotContain("decision-basis-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Product_Decision_With_Empty_BasedOn_Passes_Basis()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.md"), """{"kind": "functional", "statement": "A shopper takes a receipt on request."}"""),
            TestModel.Record("decision", "decision/0001-receipt-rule", ProductDecisionFile(config), """{"status": "approved", "options": [{"name": "Always", "pros": "Every receipt is provable.", "cons": "Paper piles up."}], "choice": "Always", "reopenedWhen": "Paper costs double.", "decides": ["requirement/checkout.receipt-on-request"], "basedOn": []}"""));

        Assert.DoesNotContain("decision-basis-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Technical_Decision_Naming_Missing_Requirement_Reports_Unresolved()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), DecisionJson("requirement/checkout.no-such-target")));

        Assert.True(Reports(model, "reference-unknown", "cites 'requirement/checkout.no-such-target', which no record holds"));
        Assert.DoesNotContain("field-invalid", TestModel.Codes(model));
    }

    /// <summary>An approved technical decision naming the target in basedOn.</summary>
    static string DecisionJson(string basedOn) =>
        "{\"status\": \"approved\", \"options\": [{\"name\": \"Documents\", \"pros\": \"Reads stay simple.\", \"cons\": \"Queries need an index.\"}], \"choice\": \"Documents\", \"reopenedWhen\": \"Queries pass one hundred a day.\", \"basedOn\": [\"" + basedOn + "\"]}";

    /// <summary>A decision file under the implementation decisions folder, so the check reads it as technical.</summary>
    static string TechnicalDecisionFile(ConsumerConfig config, string name) =>
        Path.Combine(config.Root, "docs", "implementation", "decisions", $"{name}.md");

    /// <summary>A decision file outside the implementation decisions folder, so the check reads it as a product decision.</summary>
    static string ProductDecisionFile(ConsumerConfig config) =>
        Path.Combine(config.Root, "docs", "product", "decisions", "0001-receipt-rule.md");

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
