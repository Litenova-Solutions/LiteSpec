using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a technical decision states its kind, and a decision followed only inside one module reports decision-scope-invalid.</summary>
public sealed class DecisionChecksTests
{
    /// <summary>Technical Decision Without Kind Reports Kind Missing.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void TechnicalDecisionWithoutKindReportsKindMissing()
    {
        using var temp = TestCli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), DecisionJson(null, "requirement/checkout.receipt-on-request")),
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.yaml"), """{"kind": "functional", "statement": "A shopper takes a receipt on request."}"""));

        var finding = Assert.Single(WithCode(Checker.Check(model).Findings, "decision-kind-missing"));

        Assert.Equal("Decision 'decision/0002-store-orders-as-documents' states no kind.", finding.Message);
        Assert.Equal("decision/0002-store-orders-as-documents", finding.Subject);
    }

    /// <summary>Technical Decision With Kind Passes Kind.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void TechnicalDecisionWithKindPassesKind()
    {
        using var temp = TestCli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), DecisionJson("package", "requirement/checkout.receipt-on-request")),
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.yaml"), """{"kind": "functional", "statement": "A shopper takes a receipt on request."}"""));

        Assert.DoesNotContain("decision-kind-missing", TestModel.Codes(model));
    }

    /// <summary>Product Decision Without Kind Passes Kind.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ProductDecisionWithoutKindPassesKind()
    {
        using var temp = TestCli.Temp("checks");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.yaml"), """{"kind": "functional", "statement": "A shopper takes a receipt on request."}"""),
            TestModel.Record("decision", "decision/0001-receipt-rule", ProductDecisionFile(config), """{"options": [{"name": "Always", "pros": "Every receipt is provable.", "cons": "Paper piles up."}], "choice": "Always", "consequences": "Every receipt is provable.", "reopenedWhen": "Paper costs double.", "decides": ["requirement/checkout.receipt-on-request"]}"""));

        Assert.DoesNotContain("decision-kind-missing", TestModel.Codes(model));
    }

    /// <summary>Decision With One Follower Implementing A Use Case Reports Scope Invalid.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void DecisionWithOneFollowerImplementingAUseCaseReportsScopeInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path);
        var file = ModuleFile(temp.Path, "Booking", "Holds", "UseCases", "HoldRoom", "HoldRoomCache.cs");
        model.Code.Elements.Add(Element("HoldRoomCache", file, "use-case/booking.hold-room", "decision/0002-store-orders-as-documents"));

        var finding = Assert.Single(WithCode(Checker.Check(model).Findings, "decision-scope-invalid"));

        Assert.Equal("Decision 'decision/0002-store-orders-as-documents' is followed only from module 'Booking', and each follower already carries an implements mark.", finding.Message);
        Assert.Equal("decision/0002-store-orders-as-documents", finding.Subject);
    }

    /// <summary>Decision Followed From Two Modules Passes Scope.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void DecisionFollowedFromTwoModulesPassesScope()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path);
        model.Code.Elements.Add(Element("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "use-case/booking.hold-room", "decision/0002-store-orders-as-documents"));
        model.Code.Elements.Add(Element("InvoiceCache", ModuleFile(temp.Path, "Billing", "Invoices", "InvoiceCache.cs"), "use-case/billing.raise-invoice", "decision/0002-store-orders-as-documents"));

        Assert.DoesNotContain("decision-scope-invalid", TestModel.Codes(model));
    }

    /// <summary>Decision Followed Without Implements Passes Scope.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void DecisionFollowedWithoutImplementsPassesScope()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path);
        var file = ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.Technical, "HoldRoomCache", null, file, 3,
            [new CodeMark("decision/0002-store-orders-as-documents", MarkStyle.Xml, MarkVerb.Follows, file, 1)],
            InTests: false));

        Assert.DoesNotContain("decision-scope-invalid", TestModel.Codes(model));
    }

    /// <summary>Product Decision Followed Passes Scope.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ProductDecisionFollowedPassesScope()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path);
        model.Records.Add(TestModel.Record(
            "decision", "decision/0001-receipt-rule", ProductDecisionFile(model.Config),
            """{"options": [{"name": "Always", "pros": "Every receipt is provable.", "cons": "Paper piles up."}], "choice": "Always", "consequences": "Every receipt is provable.", "reopenedWhen": "Paper costs double.", "decides": ["requirement/checkout.receipt-on-request"]}"""));
        model.Records.Add(TestModel.Record(
            "requirement", "requirement/checkout.receipt-on-request", Path.Combine(temp.Path, "checkout.yaml"),
            """{"kind": "functional", "statement": "A shopper takes a receipt on request."}"""));
        var file = ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs");
        model.Code.Elements.Add(Element("HoldRoomCache", file, "use-case/booking.hold-room", "decision/0001-receipt-rule"));

        Assert.DoesNotContain("decision-scope-invalid", TestModel.Codes(model));
    }

    /// <summary>Unfollowed Decision Passes Scope.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void UnfollowedDecisionPassesScope()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path);

        Assert.DoesNotContain("decision-scope-invalid", TestModel.Codes(model));
        Assert.Contains("decision-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Decision Followed Outside Modules Passes Scope.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void DecisionFollowedOutsideModulesPassesScope()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path);
        var file = Path.Combine(temp.Path, "scratch", "HoldRoomCache.cs");
        model.Code.Elements.Add(Element("HoldRoomCache", file, "use-case/booking.hold-room", "decision/0002-store-orders-as-documents"));

        Assert.DoesNotContain("decision-scope-invalid", TestModel.Codes(model));
    }

    /// <summary>A model with the modules root, the use cases the followers implement, and one technical decision stating its kind and basis.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel ModuleModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\"}}}");
        var config = TestModel.Config(root);

        return TestModel.Model(
            config,
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(root, "hold.yaml"), """{"kind": "command"}"""),
            TestModel.Record("use-case", "use-case/billing.raise-invoice", Path.Combine(root, "raise.yaml"), """{"kind": "command"}"""),
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), DecisionJson("package", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(root, "retention.yaml"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""));
    }

    /// <summary>One technical element carrying an implements mark and a follows mark on the decision.</summary>
    /// <param name="class">The class name carrying the marks.</param>
    /// <param name="file">The file the element sits in.</param>
    /// <param name="implements">The record id the implements mark names.</param>
    /// <param name="follows">The decision id the follows mark names.</param>
    /// <returns>The marked code element.</returns>
    static MarkedElement Element(string @class, string file, string implements, string follows) =>
        new(
            CodeKind.Technical, @class, null, file, 3,
            [
                new CodeMark(implements, MarkStyle.Xml, MarkVerb.Implements, file, 1),
                new CodeMark(follows, MarkStyle.Xml, MarkVerb.Follows, file, 2),
            ],
            InTests: false);

    /// <summary>One file under the modules root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="segments">The path segments under the modules root.</param>
    /// <returns>The full path to the file.</returns>
    static string ModuleFile(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library.Modules" };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }

    /// <summary>A technical decision naming its kind and its basis.</summary>
    /// <param name="kind">The decision kind, or null to leave the field out.</param>
    /// <param name="basedOn">The target id placed in the decision basedOn list.</param>
    /// <returns>The decision fields as JSON.</returns>
    static string DecisionJson(string? kind, string basedOn) =>
        "{\"options\": [{\"name\": \"Documents\", \"pros\": \"Reads stay simple.\", \"cons\": \"Queries need an index.\"}], \"choice\": \"Documents\", \"consequences\": \"The document store owns order persistence.\", \"reopenedWhen\": \"Queries pass one hundred a day.\""
        + (kind is null ? "" : ", \"kind\": \"" + kind + "\"")
        + ", \"basedOn\": [\"" + basedOn + "\"]}";

    /// <summary>A decision file under the implementation decisions folder, so the check reads it as technical.</summary>
    /// <param name="config">The consumer config carrying the root folder.</param>
    /// <param name="name">The decision file name without its folder.</param>
    /// <returns>The full path to the decision file.</returns>
    static string TechnicalDecisionFile(ConsumerConfig config, string name) =>
        Path.Combine(config.Root, ".litespec", "implementation", "decisions", $"{name}.yaml");

    /// <summary>A decision file outside the implementation decisions folder, so the check reads it as a product decision.</summary>
    /// <param name="config">The consumer config carrying the root folder.</param>
    /// <returns>The full path to the decision file.</returns>
    static string ProductDecisionFile(ConsumerConfig config) =>
        Path.Combine(config.Root, ".litespec", "product", "decisions", "0001-receipt-rule.yaml");

    /// <summary>Keeps the findings of one code.</summary>
    /// <param name="findings">The findings to filter.</param>
    /// <param name="code">The finding code to keep.</param>
    /// <returns>The findings carrying the code.</returns>
    static IReadOnlyList<Finding> WithCode(IReadOnlyList<Finding> findings, string code) =>
        findings.Where(finding => finding.Code == code).ToList();
}
