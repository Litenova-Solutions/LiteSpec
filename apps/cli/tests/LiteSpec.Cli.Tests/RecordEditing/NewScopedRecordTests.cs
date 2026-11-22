using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec new rule</c>, <c>new setting</c> and <c>new domain-event</c>: each scaffolds a named record folder at the home its identifier names.</summary>
public sealed class NewScopedRecordTests
{
    const string ModuleFile = ".litespec/behavior/modules/spending/module.yaml";
    const string AggregateFile = ".litespec/behavior/modules/spending/aggregates/expenses/aggregate.yaml";

    /// <summary>New rule writes its own folder under the aggregate its home names.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new rule writes its folder under its aggregate")]
    public void NewRuleWritesItsFolderUnderItsAggregate()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "new", "rule", "expense.reimbursed-once", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(consumer.Path, ".litespec/behavior/modules/spending/aggregates/expenses/rules/reimbursed-once/reimbursed-once.yaml");
        Assert.True(File.Exists(file), result.Output);
        var text = File.ReadAllText(file);
        Assert.Contains("type: rule", text);
        Assert.Contains("id: rule/expense.reimbursed-once", text);
        Assert.Contains("title: Reimbursed Once", text);
    }

    /// <summary>New rule writes its own folder under the module its home names.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new rule writes its folder under its module")]
    public void NewRuleWritesItsFolderUnderItsModule()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "new", "rule", "spending.receipt-filed-once", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(consumer.Path, ".litespec/behavior/modules/spending/rules/receipt-filed-once/receipt-filed-once.yaml");
        Assert.True(File.Exists(file), result.Output);
        var text = File.ReadAllText(file);
        Assert.Contains("id: rule/spending.receipt-filed-once", text);
        Assert.Contains("title: Receipt Filed Once", text);
    }

    /// <summary>New rule writes a product folder when its home names no aggregate or module.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new rule writes a product folder when its home names no owner")]
    public void NewRuleWritesAProductFolder()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "new", "rule", "library.receipt-kept-once", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(consumer.Path, ".litespec/behavior/rules/receipt-kept-once/receipt-kept-once.yaml");
        Assert.True(File.Exists(file), result.Output);
        Assert.Contains("id: rule/library.receipt-kept-once", File.ReadAllText(file));
    }

    /// <summary>New setting writes its own folder under its module.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new setting writes its folder under its module")]
    public void NewSettingWritesItsFolderUnderItsModule()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "new", "setting", "spending.receipt-threshold", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(consumer.Path, ".litespec/behavior/modules/spending/settings/receipt-threshold/receipt-threshold.yaml");
        Assert.True(File.Exists(file), result.Output);
        var text = File.ReadAllText(file);
        Assert.Contains("type: setting", text);
        Assert.Contains("id: setting/spending.receipt-threshold", text);
        Assert.Contains("title: Receipt Threshold", text);
    }

    /// <summary>New domain event writes its own folder under its aggregate.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new domain event writes its folder under its aggregate")]
    public void NewDomainEventWritesItsFolderUnderItsAggregate()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "new", "domain-event", "expense.reimbursed", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(consumer.Path, ".litespec/behavior/modules/spending/aggregates/expenses/domain-events/reimbursed/reimbursed.yaml");
        Assert.True(File.Exists(file), result.Output);
        var text = File.ReadAllText(file);
        Assert.Contains("type: domain-event", text);
        Assert.Contains("id: domain-event/expense.reimbursed", text);
        Assert.Contains("title: Reimbursed", text);
    }

    /// <summary>New rule writes its explanation beside its record on an explained layer.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new rule writes its explanation on an explained layer")]
    public void NewRuleWritesItsExplanation()
    {
        using var consumer = Consumer();
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"), "explanations:\n  layers:\n  - behavior\n");

        var result = TestCli.Run(
            "new", "rule", "expense.reimbursed-once", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var explanation = Path.Combine(consumer.Path, ".litespec/behavior/modules/spending/aggregates/expenses/rules/reimbursed-once/reimbursed-once.md");
        Assert.True(File.Exists(explanation), result.Output);
        var lines = File.ReadAllText(explanation).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        Assert.Equal("# Reimbursed Once", lines[0]);
        Assert.Equal(
            ["## Rationale", "## Example", "## Limits"],
            lines.Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray());
    }

    /// <summary>New rule refuses an identifier a record already carries.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new rule refuses a taken identifier")]
    public void NewRuleRefusesATakenIdentifier()
    {
        using var consumer = Consumer();
        var existing = Path.Combine(consumer.Path, ".litespec/behavior/modules/spending/aggregates/expenses/rules/reimbursed-once/reimbursed-once.yaml");
        TestRecords.Write(consumer.Path, ".litespec/behavior/modules/spending/aggregates/expenses/rules/reimbursed-once/reimbursed-once.yaml", """
            {"type":"rule","id":"rule/expense.reimbursed-once","title":"Reimbursed Once","kind":"invariant","statement":"An expense is reimbursed once."}
            """);
        var before = File.ReadAllText(existing);

        var result = TestCli.Run(
            "new", "rule", "expense.reimbursed-once", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(existing));
    }

    /// <summary>New setting refuses an identifier a record already carries.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new setting refuses a taken identifier")]
    public void NewSettingRefusesATakenIdentifier()
    {
        using var consumer = Consumer();
        var relative = ".litespec/behavior/modules/spending/settings/receipt-threshold/receipt-threshold.yaml";
        TestRecords.Write(consumer.Path, relative, """
            {"type":"setting","id":"setting/spending.receipt-threshold","title":"Receipt Threshold","valueType":"money","setBy":"product"}
            """);
        var before = File.ReadAllText(Path.Combine(consumer.Path, relative));

        var result = TestCli.Run(
            "new", "setting", "spending.receipt-threshold", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(Path.Combine(consumer.Path, relative)));
    }

    /// <summary>New domain event refuses an identifier a record already carries.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new domain event refuses a taken identifier")]
    public void NewDomainEventRefusesATakenIdentifier()
    {
        using var consumer = Consumer();
        var relative = ".litespec/behavior/modules/spending/aggregates/expenses/domain-events/reimbursed/reimbursed.yaml";
        TestRecords.Write(consumer.Path, relative, """
            {"type":"domain-event","id":"domain-event/expense.reimbursed","title":"Expense Reimbursed","meaning":"An expense was reimbursed.","carries":[]}
            """);
        var before = File.ReadAllText(Path.Combine(consumer.Path, relative));

        var result = TestCli.Run(
            "new", "domain-event", "expense.reimbursed", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(Path.Combine(consumer.Path, relative)));
    }

    /// <summary>A consumer holding the module and aggregate that give the scoped types their folders.</summary>
    /// <returns>The temporary consumer folder.</returns>
    static TempFolder Consumer()
    {
        var temp = TestCli.Temp("new-scoped-record");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), "{}");
        TestRecords.Write(temp.Path, ModuleFile, """
            {"type":"module","id":"module/spending","title":"Spending"}
            """);
        TestRecords.Write(temp.Path, AggregateFile, """
            {"type":"aggregate","id":"aggregate/spending.expense","title":"Expense"}
            """);
        return temp;
    }
}
