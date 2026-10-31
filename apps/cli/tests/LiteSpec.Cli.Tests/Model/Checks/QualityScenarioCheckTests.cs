using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves a quality requirement states its scenario, and a technical decision fulfills it.</summary>
public sealed class QualityScenarioCheckTests
{
    /// <proves>litespec/core/intent#state-each-quality-requirement-as-a-scenario</proves>
    [Theory]
    [InlineData("stimulus")]
    [InlineData("environment")]
    [InlineData("response")]
    [InlineData("measure")]
    [InlineData("dueBy")]
    public void Quality_Requirement_Missing_Field_Reports_Record_Shape(string field)
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/checkout.two-second-answer", Path.Combine(temp.Path, "checkout.md"), QualityJson(field)));

        Assert.True(Reports(model, "record-invalid", $"has no {field}"));
    }

    /// <proves>litespec/core/intent#state-each-quality-requirement-as-a-scenario</proves>
    [Fact]
    public void Quality_Requirement_With_Scenario_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/checkout.two-second-answer", Path.Combine(temp.Path, "checkout.md"), QualityJson()));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/intent#state-each-quality-requirement-as-a-scenario</proves>
    [Fact]
    public void Functional_Requirement_Without_Scenario_Passes_Shape()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.md"), """{"status": "draft", "decider": "lending-team", "decideBy": "2035-06-01", "kind": "functional", "statement": "A shopper takes a receipt on request.", "rationale": "Shoppers prove their purchase."}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Quality_Requirement_Served_Only_By_Technical_Decision_Is_Fulfilled()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("requirement", "requirement/checkout.two-second-answer", Path.Combine(temp.Path, "checkout.md"), QualityJson()),
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config), """{"status": "approved", "options": [{"name": "Documents", "pros": "Reads stay simple.", "cons": "Queries need an index."}], "choice": "Documents", "reopenedWhen": "Queries pass one hundred a day.", "basedOn": ["requirement/checkout.two-second-answer"]}"""));

        Assert.DoesNotContain("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#add-behavior-detail-when-specifying-intent</proves>
    [Fact]
    public void Quality_Requirement_With_No_Serving_Decision_Reports_Unfulfilled()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/checkout.two-second-answer", Path.Combine(temp.Path, "checkout.md"), QualityJson()));

        Assert.Contains("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact]
    public void Quality_Requirement_Named_Only_By_Product_Decision_Stays_Unfulfilled()
    {
        using var temp = Cli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("requirement", "requirement/checkout.two-second-answer", Path.Combine(temp.Path, "checkout.md"), QualityJson()),
            TestModel.Record("decision", "decision/0001-checkout-target", ProductDecisionFile(config), """{"status": "approved", "options": [{"name": "Two seconds", "pros": "Shoppers never wait.", "cons": "Peak load costs more."}], "choice": "Two seconds", "reopenedWhen": "Peak load doubles.", "decides": ["requirement/checkout.two-second-answer"], "basedOn": ["requirement/checkout.two-second-answer"]}"""));

        Assert.Contains("requirement-fulfillment-missing", TestModel.Codes(model));
    }

    /// <summary>A quality requirement carrying every scenario field except the missing ones, as a draft.</summary>
    static string QualityJson(params string[] missing)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["status"] = "draft",
            ["decider"] = "lending-team",
            ["decideBy"] = "2035-06-01",
            ["kind"] = "quality",
            ["statement"] = "Checkout answers within two seconds at peak.",
            ["rationale"] = "The sale crowds the checkout.",
            ["stimulus"] = "One thousand shoppers check out in the last hour of the sale.",
            ["environment"] = "Normal operation, one application instance down.",
            ["response"] = "Each checkout is stored and acknowledged.",
            ["measure"] = "Share of checkouts acknowledged within two seconds: at least 99 percent.",
            ["dueBy"] = "Holiday sale",
        };

        return "{" + string.Join(", ", fields
            .Where(field => !missing.Contains(field.Key, StringComparer.Ordinal))
            .Select(field => $"\"{field.Key}\": \"{field.Value}\"")) + "}";
    }

    /// <summary>A decision file under the implementation decisions folder, so the check reads it as technical.</summary>
    static string TechnicalDecisionFile(ConsumerConfig config) =>
        Path.Combine(config.Root, "docs", "implementation", "decisions", "0002-store-orders-as-documents.md");

    /// <summary>A decision file outside the implementation decisions folder, so the check reads it as a product decision.</summary>
    static string ProductDecisionFile(ConsumerConfig config) =>
        Path.Combine(config.Root, "docs", "product", "decisions", "0001-checkout-target.md");

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
