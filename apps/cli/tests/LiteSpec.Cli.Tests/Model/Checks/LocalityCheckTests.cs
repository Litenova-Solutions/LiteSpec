using LiteSpec.Cli.Model;
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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/finance.refund-needs-receipt", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "rules", "refund-needs-receipt", "refund-needs-receipt.yaml"), """{"kind": "invariant", "statement": "A refund needs a receipt.", "governs": ["aggregate/lending.loan"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "link-locality-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Rule 'rule/finance.refund-needs-receipt' in module 'finance' governs 'aggregate/lending.loan' in module 'lending' with no declared dependency. State the check as its own rule in module 'lending' at its boundary and name this rule in prose; module 'finance' checks again when it acts.", finding.Message);
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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": ["module/lending"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/finance.refund-needs-receipt", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "rules", "refund-needs-receipt", "refund-needs-receipt.yaml"), """{"kind": "invariant", "statement": "A refund needs a receipt.", "governs": ["aggregate/lending.loan"]}"""));

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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "integration-events", "loan-borrowed.yaml"), """{"action": "send"}"""),
            TestModel.Record("reaction", "reaction/finance.notify-on-loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "reactions", "notify-on-loan-borrowed.yaml"), """{"triggeredBy": ["integration-event/loan-borrowed"]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
    }

    /// <summary>Public Query Reached Through Module Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void PublicQueryReachedThroughModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["module/finance"]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
    }

    /// <summary>Queries Edge To Listed Public Query Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void QueriesEdgeToListedPublicQueryPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["module/finance"]}"""),
            TestModel.Record("use-case", "use-case/finance.record-repayment", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "record-repayment.yaml"), """{"kind": "command", "effects": [{"queries": "use-case/lending.read-loan", "timing": "inline"}]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
        Assert.DoesNotContain("query-target-invalid", TestModel.Codes(model));
    }

    /// <summary>Queries Edge To Unlisted Query Reports Once.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void QueriesEdgeToUnlistedQueryReportsOnce()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/reporting", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "read-loan.yaml"), """{"kind": "query", "reachedThrough": ["module/reporting"]}"""),
            TestModel.Record("use-case", "use-case/finance.record-repayment", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "record-repayment.yaml"), """{"kind": "command", "effects": [{"queries": "use-case/lending.read-loan", "timing": "inline"}]}"""));

        var findings = Checker.Check(model).Findings
            .Where(found => found.Subject == "use-case/finance.record-repayment"
                && found.Code is "link-locality-invalid" or "query-target-invalid");
        var finding = Assert.Single(findings);
        Assert.Equal("query-target-invalid", finding.Code);
    }

    /// <summary>Commands Edge To Listed Public Command Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void CommandsEdgeToListedPublicCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "borrow-copy.yaml"), """{"kind": "command", "reachedThrough": ["module/finance"]}"""),
            TestModel.Record("reaction", "reaction/finance.record-on-borrow", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "record-on-borrow.yaml"), """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"commands": "use-case/lending.borrow-copy"}]}"""));

        Assert.DoesNotContain("link-locality-invalid", TestModel.Codes(model));
        Assert.DoesNotContain("command-target-invalid", TestModel.Codes(model));
    }

    /// <summary>Commands Edge To Unlisted Command Reports Once.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void CommandsEdgeToUnlistedCommandReportsOnce()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/reporting", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "borrow-copy.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""),
            TestModel.Record("reaction", "reaction/finance.record-on-borrow", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "record-on-borrow.yaml"), """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"commands": "use-case/lending.borrow-copy"}]}"""));

        var findings = Checker.Check(model).Findings
            .Where(found => found.Subject == "reaction/finance.record-on-borrow"
                && found.Code is "link-locality-invalid" or "command-target-invalid");
        var finding = Assert.Single(findings);
        Assert.Equal("command-target-invalid", finding.Code);
    }

    /// <summary>Link Across Layers May Cross Modules.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void LinkAcrossLayersMayCrossModules()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "features", "borrowing.yaml"), """{}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, ".litespec", "intent", "modules", "lending", "features", "borrowing.yaml"), """{}"""),
            TestModel.Record("use-case", "use-case/finance.refund-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "aggregates", "refunds", "refund-fine.yaml"), """{"kind": "command", "fulfills": ["requirement/borrowing.max-active-loans"]}"""));

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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"), """{}"""),
            TestModel.Record("rule", "rule/lending.copy-barcode-unique", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "rules", "copy-barcode-unique", "copy-barcode-unique.yaml"), """{"kind": "invariant", "statement": "A copy barcode names one copy.", "governs": ["aggregate/lending.loan"]}"""));

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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": ["module/finance"]}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": ["module/lending"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/finance' holds a dependency cycle: module/finance -> module/lending -> module/finance. The edges are: 'module/finance' declares 'dependsOn' 'module/lending'; 'module/lending' declares 'dependsOn' 'module/finance'.", finding.Message);
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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": ["module/lending"]}"""));

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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": ["module/lending"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/lending' holds a dependency cycle: module/lending -> module/lending. The edges are: 'module/lending' declares 'dependsOn' 'module/lending'.", finding.Message);
        Assert.Equal("module/lending", finding.Subject);
    }

    /// <summary>Queries Cycle Reports.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void QueriesCycleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "read-loan.yaml"), """{"kind": "query", "effects": [{"queries": "use-case/finance.read-fine", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/finance.read-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "read-fine.yaml"), """{"kind": "query", "effects": [{"queries": "use-case/lending.read-loan", "timing": "inline"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/finance' holds a dependency cycle: module/finance -> module/lending -> module/finance. The edges are: 'module/finance' queries 'use-case/lending.read-loan'; 'module/lending' queries 'use-case/finance.read-fine'.", finding.Message);
        Assert.Equal("module/finance", finding.Subject);
    }

    /// <summary>Contracts Reference Cycle Reports.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void ContractsReferenceCycleReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteContractsProject(temp.Path, "Library.Lending.Contracts", "Library.Finance.Contracts");
        WriteContractsProject(temp.Path, "Library.Finance.Contracts", "Library.Lending.Contracts");
        WriteModulesConfig(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/finance' holds a dependency cycle: module/finance -> module/lending -> module/finance. The edges are: 'module/finance' references the Contracts project of 'module/lending'; 'module/lending' references the Contracts project of 'module/finance'.", finding.Message);
        Assert.Equal("module/finance", finding.Subject);
    }

    /// <summary>Trigger Cycle Reports.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void TriggerCycleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module-event", "module-event/lending.loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module-events.yaml"), """{"summary": "A copy was lent."}"""),
            TestModel.Record("module-event", "module-event/finance.fine-paid", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module-events.yaml"), """{"summary": "A fine was paid."}"""),
            TestModel.Record("reaction", "reaction/lending.notify-on-fine-paid", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "reactions.yaml"), """{"triggeredBy": ["module-event/finance.fine-paid"]}"""),
            TestModel.Record("reaction", "reaction/finance.notify-on-loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "reactions.yaml"), """{"triggeredBy": ["module-event/lending.loan-borrowed"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/finance' holds a dependency cycle: module/finance -> module/lending -> module/finance. The edges are: 'module/finance' reacts to 'module-event/lending.loan-borrowed'; 'module/lending' reacts to 'module-event/finance.fine-paid'.", finding.Message);
        Assert.Equal("module/finance", finding.Subject);
    }

    /// <summary>Mixed DependsOn And Queries Cycle Names Each Edge.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void MixedDependsOnAndQueriesCycleNamesEachEdge()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": ["module/finance"]}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "read-loan.yaml"), """{"kind": "query"}"""),
            TestModel.Record("use-case", "use-case/finance.read-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "read-fine.yaml"), """{"kind": "query", "effects": [{"queries": "use-case/lending.read-loan", "timing": "inline"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/finance' holds a dependency cycle: module/finance -> module/lending -> module/finance. The edges are: 'module/finance' queries 'use-case/lending.read-loan'; 'module/lending' declares 'dependsOn' 'module/finance'.", finding.Message);
        Assert.Equal("module/finance", finding.Subject);
    }

    /// <summary>One Way Queries Edge Passes.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void OneWayQueriesEdgePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.read-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "read-loan.yaml"), """{"kind": "query"}"""),
            TestModel.Record("use-case", "use-case/finance.read-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "read-fine.yaml"), """{"kind": "query", "effects": [{"queries": "use-case/lending.read-loan", "timing": "inline"}]}"""));

        Assert.DoesNotContain("module-dependency-invalid", TestModel.Codes(model));
    }

    /// <summary>Commands Cycle Reports.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void CommandsCycleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "borrow-copy.yaml"), """{"kind": "command", "effects": [{"commands": "use-case/finance.record-fine", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/finance.record-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "record-fine.yaml"), """{"kind": "command", "effects": [{"commands": "use-case/lending.borrow-copy", "timing": "inline"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "module-dependency-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("module/finance", finding.Subject);
        Assert.Contains("'module/finance' commands 'use-case/lending.borrow-copy'", finding.Message, StringComparison.Ordinal);
        Assert.Contains("'module/lending' commands 'use-case/finance.record-fine'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>One Way Commands Edge Passes.</summary>
    /// <proves>litespec/core/model#keep-module-dependencies-free-of-cycles</proves>
    [Fact]
    public void OneWayCommandsEdgePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "borrow-copy.yaml"), """{"kind": "command"}"""),
            TestModel.Record("use-case", "use-case/finance.record-fine", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "record-fine.yaml"), """{"kind": "command", "effects": [{"commands": "use-case/lending.borrow-copy", "timing": "inline"}]}"""));

        Assert.DoesNotContain("module-dependency-invalid", TestModel.Codes(model));
    }

    /// <summary>Writes one Contracts project referencing another Contracts project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="project">The Contracts project folder name.</param>
    /// <param name="referenced">The referenced Contracts project folder name.</param>
    static void WriteContractsProject(string root, string project, string referenced)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, project + ".csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <ProjectReference Include=\"../" + referenced + "/" + referenced + ".csproj\" />\n  </ItemGroup>\n</Project>\n");
    }

    /// <summary>Writes the project file declaring the two Contracts projects as the module roots.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteModulesConfig(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """{"stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": ["apps/api/src/Modules/Library.Lending.Contracts", "apps/api/src/Modules/Library.Finance.Contracts"]}}}""");
    }

    /// <summary>Event With Two Publishers Reports.</summary>
    /// <proves>litespec/core/model#publish-each-integration-event-from-one-module</proves>
    [Fact]
    public void EventWithTwoPublishersReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "domain-events", "loan-borrowed", "loan-borrowed.yaml"), """{"carries": ["loanId"]}"""),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "integration-events", "loan-borrowed.yaml"), """{"action": "send", "translates": "domain-event/loan.loan-borrowed"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "borrow-copy", "borrow-copy.yaml"), """{"kind": "command", "raises": ["domain-event/loan.loan-borrowed"]}"""),
            TestModel.Record("use-case", "use-case/finance.record-loan", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "use-cases", "record-loan", "record-loan.yaml"), """{"kind": "command", "raises": ["domain-event/loan.loan-borrowed"]}"""));

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
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("domain-event", "domain-event/loan.loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "domain-events", "loan-borrowed", "loan-borrowed.yaml"), """{"carries": ["loanId"]}"""),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "integration-events", "loan-borrowed.yaml"), """{"action": "send", "translates": "domain-event/loan.loan-borrowed"}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "borrow-copy", "borrow-copy.yaml"), """{"kind": "command", "raises": ["domain-event/loan.loan-borrowed"]}"""));

        Assert.DoesNotContain("event-publisher-invalid", TestModel.Codes(model));
    }
}
