using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the locality checks: cross-module links, module cycles and event publishers, at tier G.</summary>
public sealed class LocalityCheckTests
{
    /// <summary>Same Layer Link Across Modules Without Dependency Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void LinkAcrossModulesWithoutDependencyReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/finance.refund-needs-receipt", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "rules", "finance-rules.yaml"), """{"kind": "invariant", "statement": "A refund needs a receipt.", "governs": ["aggregate/lending.loan"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "link-locality-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Rule 'rule/finance.refund-needs-receipt' in module 'finance' links to 'aggregate/lending.loan' in module 'lending' through 'governs' with no declared dependency.", finding.Message);
        Assert.Equal("rule/finance.refund-needs-receipt", finding.Subject);
    }

    /// <summary>Same Layer Link Across Modules With Dependency Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void LinkAcrossModulesWithDependencyPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": ["module/lending"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/finance.refund-needs-receipt", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "rules", "finance-rules.yaml"), """{"kind": "invariant", "statement": "A refund needs a receipt.", "governs": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
    }

    /// <summary>Link Through Integration Event Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void LinkThroughIntegrationEventPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "integration-events", "loan-borrowed.yaml"), """{"direction": "out"}"""),
            TestModel.Record("reaction", "reaction/finance.notify-on-loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "reactions", "notify-on-loan-borrowed.yaml"), """{"triggeredBy": ["integration-event/loan-borrowed"]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
    }

    /// <summary>Link Across Layers May Cross Modules.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void LinkAcrossLayersMayCrossModules()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "features", "borrowing.yaml"), """{}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "features", "borrowing.yaml"), """{}"""),
            TestModel.Record("use-case", "use-case/finance.refund-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "refunds", "refund-fine.yaml"), """{"kind": "command", "fulfills": ["requirement/borrowing.max-active-loans"]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
    }

    /// <summary>Link Within Same Module Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void LinkWithinSameModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/lending.copy-barcode-unique", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "rules", "lending-rules.yaml"), """{"kind": "invariant", "statement": "A copy barcode names one copy.", "governs": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
    }

    /// <summary>Module Cycle Reports.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void ModuleCycleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": ["module/finance"]}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": ["module/lending"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/finance' holds a dependency cycle: module/finance -> module/lending -> module/finance.", finding.Message);
        Assert.Equal("module/finance", finding.Subject);
    }

    /// <summary>Acyclic Modules Pass.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void ModuleAcyclicPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": ["module/lending"]}"""));

        Assert.DoesNotContain("module-dependency-invalid", TestModel.Codes(model));
    }

    /// <summary>Module Depending On Itself Reports.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void ModuleSelfDependencyReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": ["module/lending"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/lending' holds a dependency cycle: module/lending -> module/lending.", finding.Message);
        Assert.Equal("module/lending", finding.Subject);
    }

    /// <summary>Event With Two Publishers Reports.</summary>
    /// <proves>litespec/core/model#publish-each-integration-event-from-one-module</proves>
    [Fact]
    public void EventWithTwoPublishersReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "integration-events", "loan-borrowed.yaml"), """{"direction": "out"}"""),
            TestModel.Record("job", "job/lending.announce-loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "jobs", "announce-loan-borrowed.yaml"), """{"kind": "manual", "guard": "An operator starts the job.", "effects": [{"emits": "integration-event/loan-borrowed"}]}"""),
            TestModel.Record("job", "job/finance.announce-loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "jobs", "announce-loan-borrowed.yaml"), """{"kind": "manual", "guard": "An operator starts the job.", "effects": [{"emits": "integration-event/loan-borrowed"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "event-publisher-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Integration event 'integration-event/loan-borrowed' is published from 2 modules: 'module/finance', 'module/lending'.", finding.Message);
        Assert.Equal("integration-event/loan-borrowed", finding.Subject);
    }

    /// <summary>Event With One Publisher Passes.</summary>
    /// <proves>litespec/core/model#publish-each-integration-event-from-one-module</proves>
    [Fact]
    public void EventWithOnePublisherPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "intent", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "integration-events", "loan-borrowed.yaml"), """{"direction": "out"}"""),
            TestModel.Record("job", "job/lending.announce-loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "jobs", "announce-loan-borrowed.yaml"), """{"kind": "manual", "guard": "An operator starts the job.", "effects": [{"emits": "integration-event/loan-borrowed"}]}"""));

        Assert.DoesNotContain("event-publisher-invalid", TestModel.Codes(model));
    }
}
