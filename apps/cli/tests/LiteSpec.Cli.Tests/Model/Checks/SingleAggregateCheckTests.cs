using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the single-aggregate checks: a command writing more than one aggregate without a decision naming it, and a command use case at module level.</summary>
public sealed class SingleAggregateCheckTests
{
    /// <summary>Command Writing Two Aggregates Without A Decision Reports Writes Invalid.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void CommandWritingTwoAggregatesWithoutDecisionReportsWritesInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), "{}"),
            TestModel.Record("aggregate", "aggregate/spending.receipt", Path.Combine(temp.Path, "receipt.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/spending.attach-receipt", Path.Combine(temp.Path, "attach.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense", "aggregate/spending.receipt"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "use-case-writes-invalid");

        Assert.Equal("use-case/spending.attach-receipt", finding.Subject);
        Assert.Contains("aggregate/spending.expense", finding.Message, StringComparison.Ordinal);
        Assert.Contains("aggregate/spending.receipt", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Command Writing Two Aggregates Under A Rule Based On A Decision Passes.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void CommandWritingTwoAggregatesUnderRuleBasedOnDecisionPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), "{}"),
            TestModel.Record("aggregate", "aggregate/spending.receipt", Path.Combine(temp.Path, "receipt.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/spending.attach-receipt", Path.Combine(temp.Path, "attach.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense", "aggregate/spending.receipt"]}"""),
            TestModel.Record("rule", "rule/spending.receipt-attached-atomically", Path.Combine(temp.Path, "rules.yaml"), """{"kind": "atomicity", "governs": ["use-case/spending.attach-receipt"], "basedOn": ["decision/0002-attach-receipt-writes-two-aggregates"]}"""),
            TestModel.Record("decision", "decision/0002-attach-receipt-writes-two-aggregates", Path.Combine(temp.Path, "decisions.yaml"), """{"consequences": "Attaching a receipt writes both aggregates in one transaction."}"""));

        Assert.DoesNotContain("use-case-writes-invalid", TestModel.Codes(model));
    }

    /// <summary>Command Writing Two Aggregates That A Decision Only Mentions Reports.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void CommandWritingTwoAggregatesOnlyMentionedByDecisionReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), "{}"),
            TestModel.Record("aggregate", "aggregate/spending.receipt", Path.Combine(temp.Path, "receipt.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/spending.attach-receipt", Path.Combine(temp.Path, "attach.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense", "aggregate/spending.receipt"]}"""),
            TestModel.Record("decision", "decision/0002-attach-receipt-writes-two-aggregates", Path.Combine(temp.Path, "decisions.yaml"), """{"consequences": "use-case/spending.attach-receipt writes both aggregates in one transaction."}"""));

        Assert.Contains("use-case-writes-invalid", TestModel.Codes(model));
    }

    /// <summary>Command Writing One Aggregate Passes.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void CommandWritingOneAggregatePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense"]}"""));

        Assert.DoesNotContain("use-case-writes-invalid", TestModel.Codes(model));
    }

    /// <summary>Command Writing One Aggregate Twice Passes.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void CommandWritingOneAggregateTwicePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit.yaml"), """{"kind": "command", "writes": ["aggregate/spending.expense", "aggregate/spending.expense"]}"""));

        Assert.DoesNotContain("use-case-writes-invalid", TestModel.Codes(model));
    }

    /// <summary>Query Naming Two Aggregates Passes.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void QueryNamingTwoAggregatesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), "{}"),
            TestModel.Record("aggregate", "aggregate/spending.receipt", Path.Combine(temp.Path, "receipt.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/spending.export-expenses", Path.Combine(temp.Path, "export.yaml"), """{"kind": "query", "writes": ["aggregate/spending.expense", "aggregate/spending.receipt"]}"""));

        Assert.DoesNotContain("use-case-writes-invalid", TestModel.Codes(model));
    }

    /// <summary>Command At Module Level Reports Placement.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void CommandAtModuleLevelReportsPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var file = ModuleUseCaseFile(temp.Path, "lending", "purge-loans");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/lending.purge-loans", file, """{"kind": "command", "writes": ["aggregate/lending.loan"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-placement-invalid" && found.Subject == "use-case/lending.purge-loans");

        Assert.Equal(file, finding.Path);
    }

    /// <summary>Query At Module Level Passes Placement.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void QueryAtModuleLevelPassesPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var file = ModuleUseCaseFile(temp.Path, "lending", "list-loans");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", file, """{"kind": "query"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-placement-invalid" && found.Subject == "use-case/lending.list-loans");
    }

    /// <summary>Command Under Its Aggregate Passes Placement.</summary>
    /// <proves>litespec/core/use-cases#change-one-aggregate-in-each-command</proves>
    [Fact]
    public void CommandUnderItsAggregatePassesPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var file = AggregateUseCaseFile(temp.Path, "lending", "loans", "borrow-copy");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", Path.Combine(temp.Path, "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), "{}"),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", file, """{"kind": "command", "writes": ["aggregate/lending.loan"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-placement-invalid" && found.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>A module-level use-case file under the record root, with its folders created.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="module">The module name.</param>
    /// <param name="name">The use-case name.</param>
    /// <returns>The use-case file path.</returns>
    static string ModuleUseCaseFile(string root, string module, string name)
    {
        var file = Path.Combine(root, ".litespec", "behavior", "modules", module, "use-cases", name, name + ".yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        return file;
    }

    /// <summary>An aggregate use-case file under the record root, with its folders created.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="module">The module name.</param>
    /// <param name="aggregate">The aggregate folder name.</param>
    /// <param name="name">The use-case name.</param>
    /// <returns>The use-case file path.</returns>
    static string AggregateUseCaseFile(string root, string module, string aggregate, string name)
    {
        var file = Path.Combine(root, ".litespec", "behavior", "modules", module, "aggregates", aggregate, "use-cases", name, name + ".yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        return file;
    }
}
