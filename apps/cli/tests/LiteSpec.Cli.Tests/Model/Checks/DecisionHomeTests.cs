using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a technical decision kept at a level its followers do not match reports decision-home-drift.</summary>
public sealed class DecisionHomeTests
{
    /// <summary>Module Decision Followed From Two Modules Reports Home Drift.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ModuleDecisionFollowedFromTwoModulesReportsHomeDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path, "Booking");
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "decision/0002-store-orders-as-documents"));
        model.Code.Elements.Add(FollowsOnly("InvoiceCache", ModuleFile(temp.Path, "Billing", "Invoices", "InvoiceCache.cs"), "decision/0002-store-orders-as-documents"));

        var finding = Assert.Single(WithCode(Checker.Check(model).Findings, "decision-home-drift"));

        Assert.Equal("Decision 'decision/0002-store-orders-as-documents' sits in module 'Booking' but is followed from modules 'Billing', 'Booking', so it belongs at product level.", finding.Message);
        Assert.Equal("decision/0002-store-orders-as-documents", finding.Subject);
    }

    /// <summary>Product Decision Followed From One Module Reports Home Drift.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ProductDecisionFollowedFromOneModuleReportsHomeDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = ProductModel(temp.Path);
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "decision/0002-store-orders-as-documents"));

        var finding = Assert.Single(WithCode(Checker.Check(model).Findings, "decision-home-drift"));

        Assert.Equal("Decision 'decision/0002-store-orders-as-documents' sits at product level but is followed only from module 'Booking', so it belongs in that module.", finding.Message);
        Assert.Equal("decision/0002-store-orders-as-documents", finding.Subject);
    }

    /// <summary>Module Decision Followed From Another Module Reports Home Drift.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ModuleDecisionFollowedFromAnotherModuleReportsHomeDrift()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path, "Billing");
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "decision/0002-store-orders-as-documents"));

        var finding = Assert.Single(WithCode(Checker.Check(model).Findings, "decision-home-drift"));

        Assert.Equal("Decision 'decision/0002-store-orders-as-documents' sits in module 'Billing' but is followed only from module 'Booking', so it belongs in that module.", finding.Message);
        Assert.Equal("decision/0002-store-orders-as-documents", finding.Subject);
    }

    /// <summary>Module Decision Followed From Its Home Module Passes Home.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ModuleDecisionFollowedFromItsHomeModulePassesHome()
    {
        using var temp = TestCli.Temp("checks");
        var model = ModuleModel(temp.Path, "Booking");
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "decision/0002-store-orders-as-documents"));
        model.Code.Elements.Add(FollowsOnly("HoldRoomPolicy", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomPolicy.cs"), "decision/0002-store-orders-as-documents"));

        Assert.DoesNotContain("decision-home-drift", TestModel.Codes(model));
    }

    /// <summary>Product Decision Followed From Two Modules Passes Home.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ProductDecisionFollowedFromTwoModulesPassesHome()
    {
        using var temp = TestCli.Temp("checks");
        var model = ProductModel(temp.Path);
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "decision/0002-store-orders-as-documents"));
        model.Code.Elements.Add(FollowsOnly("InvoiceCache", ModuleFile(temp.Path, "Billing", "Invoices", "InvoiceCache.cs"), "decision/0002-store-orders-as-documents"));

        Assert.DoesNotContain("decision-home-drift", TestModel.Codes(model));
    }

    /// <summary>Unfollowed Decision Passes Home.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void UnfollowedDecisionPassesHome()
    {
        using var temp = TestCli.Temp("checks");
        var model = ProductModel(temp.Path);

        Assert.DoesNotContain("decision-home-drift", TestModel.Codes(model));
        Assert.Contains("decision-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Intent Decision Followed Passes Home.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void IntentDecisionFollowedPassesHome()
    {
        using var temp = TestCli.Temp("checks");
        var model = ProductModel(temp.Path);
        model.Records.Add(TestModel.Record(
            "decision", "decision/0001-receipt-rule", ProductDecisionFile(model.Config),
            """{"options": [{"name": "Always", "pros": "Every receipt is provable.", "cons": "Paper piles up."}], "choice": "Always", "consequences": "Every receipt is provable.", "reopenedWhen": "Paper costs double.", "decides": ["constraint/receipts-stay-in-the-eu"]}"""));
        var file = ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs");
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", file, "decision/0001-receipt-rule"));

        Assert.DoesNotContain("decision-home-drift", TestModel.Codes(model));
    }

    /// <summary>Product Decision Followed Outside Modules Passes Home.</summary>
    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact]
    public void ProductDecisionFollowedOutsideModulesPassesHome()
    {
        using var temp = TestCli.Temp("checks");
        var model = ProductModel(temp.Path);
        var file = Path.Combine(temp.Path, "scratch", "HoldRoomCache.cs");
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", file, "decision/0002-store-orders-as-documents"));

        Assert.DoesNotContain("decision-home-drift", TestModel.Codes(model));
    }

    /// <summary>A model with the modules root, one module-level technical decision stating its kind and basis, and its basis.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <param name="home">The module the decision file sits in.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel ModuleModel(string root, string home)
    {
        WriteProject(root);
        var config = TestModel.Config(root);

        return TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", ModuleDecisionFile(config, home, "0002-store-orders-as-documents"), DecisionJson("package", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(root, "retention.yaml"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""));
    }

    /// <summary>A model with the modules root, one product-level technical decision stating its kind and basis, and its basis.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel ProductModel(string root)
    {
        WriteProject(root);
        var config = TestModel.Config(root);

        return TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0002-store-orders-as-documents", TechnicalDecisionFile(config, "0002-store-orders-as-documents"), DecisionJson("package", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(root, "retention.yaml"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""));
    }

    /// <summary>Writes the minimal project file naming the modules root.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\"}}}");
    }

    /// <summary>One technical element carrying only a follows mark on the decision.</summary>
    /// <param name="class">The class name carrying the mark.</param>
    /// <param name="file">The file the element sits in.</param>
    /// <param name="follows">The decision id the follows mark names.</param>
    /// <returns>The marked code element.</returns>
    static MarkedElement FollowsOnly(string @class, string file, string follows) =>
        new(
            CodeKind.Technical, @class, null, file, 3,
            [new CodeMark(follows, MarkStyle.Xml, MarkVerb.Follows, file, 1)],
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

    /// <summary>A decision file under the product implementation decisions folder, so the check reads it as product level.</summary>
    /// <param name="config">The consumer config carrying the root folder.</param>
    /// <param name="name">The decision file name without its folder.</param>
    /// <returns>The full path to the decision file.</returns>
    static string TechnicalDecisionFile(ConsumerConfig config, string name) =>
        Path.Combine(config.Root, ".litespec", "implementation", "decisions", $"{name}.yaml");

    /// <summary>A decision file under one module implementation decisions folder, so the check reads it as module level.</summary>
    /// <param name="config">The consumer config carrying the root folder.</param>
    /// <param name="module">The module the decision file sits in.</param>
    /// <param name="name">The decision file name without its folder.</param>
    /// <returns>The full path to the decision file.</returns>
    static string ModuleDecisionFile(ConsumerConfig config, string module, string name) =>
        Path.Combine(config.Root, ".litespec", "implementation", "modules", module, "decisions", $"{name}.yaml");

    /// <summary>A decision file under the intent decisions folder, so the check reads it as a product decision.</summary>
    /// <param name="config">The consumer config carrying the root folder.</param>
    /// <returns>The full path to the decision file.</returns>
    static string ProductDecisionFile(ConsumerConfig config) =>
        Path.Combine(config.Root, ".litespec", "intent", "decisions", "0001-receipt-rule.yaml");

    /// <summary>Keeps the findings of one code.</summary>
    /// <param name="findings">The findings to filter.</param>
    /// <param name="code">The finding code to keep.</param>
    /// <returns>The findings carrying the code.</returns>
    static IReadOnlyList<Finding> WithCode(IReadOnlyList<Finding> findings, string code) =>
        findings.Where(finding => finding.Code == code).ToList();
}
