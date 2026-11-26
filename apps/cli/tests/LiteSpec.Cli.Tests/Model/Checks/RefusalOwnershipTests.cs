using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the refusal ownership checks: a code carried from another module, and a code two exception types carry.</summary>
public sealed class RefusalOwnershipTests
{
    /// <summary>Code Carried From Another Module Reports Foreign.</summary>
    /// <proves>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void CodeCarriedFromAnotherModuleReportsForeign()
    {
        using var temp = TestCli.Temp("checks");
        var model = OwnershipModel(temp.Path);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Reporting", "Domain", "Reports", "Exceptions", "ReportReceiptMissingException.cs"),
            11,
            "ReportReceiptMissingException"));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "refusal-code-duplicate");
        var message = Assert.Single(findings, finding => finding.Code == "refusal-module-foreign").Message;
        Assert.Contains("spending.receipt-missing", message, StringComparison.Ordinal);
        Assert.Contains("spending", message, StringComparison.Ordinal);
        Assert.Contains("Reporting", message, StringComparison.Ordinal);
    }

    /// <summary>Code Carried By Two Exception Types Reports Duplicate.</summary>
    /// <proves>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void CodeCarriedByTwoExceptionTypesReportsDuplicate()
    {
        using var temp = TestCli.Temp("checks");
        var model = OwnershipModel(temp.Path);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs"),
            11,
            "ExpenseReceiptMissingException"));
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Spending", "Domain", "Receipts", "Exceptions", "ReceiptMissingException.cs"),
            11,
            "ReceiptMissingException"));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "refusal-module-foreign");
        var message = Assert.Single(findings, finding => finding.Code == "refusal-code-duplicate").Message;
        Assert.Contains("ExpenseReceiptMissingException", message, StringComparison.Ordinal);
        Assert.Contains("ReceiptMissingException", message, StringComparison.Ordinal);
    }

    /// <summary>Code Carried By Two Exceptions Marking One Rule Names The Rule.</summary>
    /// <proves>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    /// <proves>litespec/packs/dotnet/domain#reject-business-violations-with-domain-exceptions</proves>
    [Fact]
    public void CodeCarriedByTwoExceptionsMarkingOneRuleNamesTheRule()
    {
        using var temp = TestCli.Temp("checks");
        var first = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs");
        var second = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Spending", "Domain", "Receipts", "Exceptions", "ReceiptMissingException.cs");
        var model = OwnershipModel(temp.Path);
        model.Records.Add(TestModel.Record(
            "rule",
            "rule/expense.receipt-required",
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "receipt-required", "receipt-required.yaml"),
            4,
            """{"title": "Receipt Required", "kind": "invariant", "statement": "An expense above the threshold carries a receipt.", "refusal": "refusal/spending.receipt-missing"}""",
            isEntry: true));
        model.Code.Elements.Add(new MarkedElement(CodeKind.RuleException, "ExpenseReceiptMissingException", null, first, 11, [new CodeMark("rule/expense.receipt-required", MarkStyle.Xml, MarkVerb.Implements, first, 10)], false));
        model.Code.Elements.Add(new MarkedElement(CodeKind.RuleException, "ReceiptMissingException", null, second, 11, [new CodeMark("rule/expense.receipt-required", MarkStyle.Xml, MarkVerb.Implements, second, 10)], false));
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", first, 11, "ExpenseReceiptMissingException"));
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", second, 11, "ReceiptMissingException"));

        var message = Assert.Single(Checker.Check(model).Findings, finding => finding.Code == "refusal-code-duplicate").Message;

        Assert.Contains("rule/expense.receipt-required", message, StringComparison.Ordinal);
        Assert.Contains("litespec/packs/dotnet/domain#reject-business-violations-with-domain-exceptions", message, StringComparison.Ordinal);
    }

    /// <summary>Code Carried By Two Exceptions Marking Other Rules Names No Rule.</summary>
    /// <proves>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void CodeCarriedByTwoExceptionsMarkingOtherRulesNamesNoRule()
    {
        using var temp = TestCli.Temp("checks");
        var first = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs");
        var second = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Spending", "Domain", "Receipts", "Exceptions", "ReceiptMissingException.cs");
        var model = OwnershipModel(temp.Path);
        model.Records.Add(TestModel.Record(
            "rule",
            "rule/expense.receipt-required",
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "rules", "receipt-required", "receipt-required.yaml"),
            4,
            """{"title": "Receipt Required", "kind": "invariant", "statement": "An expense above the threshold carries a receipt.", "refusal": "refusal/spending.receipt-missing"}""",
            isEntry: true));
        model.Records.Add(TestModel.Record(
            "rule",
            "rule/receipt.kept-readable",
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "aggregates", "receipts", "rules", "kept-readable", "kept-readable.yaml"),
            4,
            """{"title": "Kept Readable", "kind": "invariant", "statement": "A kept receipt stays readable.", "refusal": "refusal/spending.receipt-unreadable"}""",
            isEntry: true));
        model.Code.Elements.Add(new MarkedElement(CodeKind.RuleException, "ExpenseReceiptMissingException", null, first, 11, [new CodeMark("rule/expense.receipt-required", MarkStyle.Xml, MarkVerb.Implements, first, 10)], false));
        model.Code.Elements.Add(new MarkedElement(CodeKind.RuleException, "ReceiptMissingException", null, second, 11, [new CodeMark("rule/receipt.kept-readable", MarkStyle.Xml, MarkVerb.Implements, second, 10)], false));
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", first, 11, "ExpenseReceiptMissingException"));
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", second, 11, "ReceiptMissingException"));

        var message = Assert.Single(Checker.Check(model).Findings, finding => finding.Code == "refusal-code-duplicate").Message;

        Assert.Contains("ExpenseReceiptMissingException", message, StringComparison.Ordinal);
        Assert.Contains("ReceiptMissingException", message, StringComparison.Ordinal);
        Assert.DoesNotContain("rule/", message, StringComparison.Ordinal);
        Assert.DoesNotContain("reject-business-violations-with-domain-exceptions", message, StringComparison.Ordinal);
    }

    /// <summary>Code Carried By One Exception Type In Its Module Passes.</summary>
    /// <proves>litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void CodeCarriedByOneExceptionTypeInItsModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = OwnershipModel(temp.Path);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs"),
            11,
            "ExpenseReceiptMissingException"));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("refusal-module-foreign", codes);
        Assert.DoesNotContain("refusal-code-duplicate", codes);
    }

    /// <summary>A model whose spending set declares the receipt-missing code, under a modules root with spending and reporting folders.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The project model holding the refusal set.</returns>
    static ProjectModel OwnershipModel(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(Path.Combine(root, ".litespec/project.yaml"), """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": "apps/api/src/Library.Modules"}}}
            """);
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Modules", "Spending"));
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Modules", "Reporting"));

        var model = TestModel.Model(TestModel.Config(root));
        model.RefusalSets.Add(new RefusalEntry(
            "spending",
            "spending.receipt-missing",
            "The expense is above the receipt threshold and carries no receipt.",
            Path.Combine(root, "refusals.yaml"),
            null));

        return model;
    }
}
