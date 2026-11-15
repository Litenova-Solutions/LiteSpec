using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the refusal ownership checks: a code carried from another module, and a code two exception types carry.</summary>
public sealed class RefusalOwnershipTests
{
    /// <summary>Code Carried From Another Module Reports Foreign.</summary>
    /// <proves>litespec/backend/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void CodeCarriedFromAnotherModuleReportsForeign()
    {
        using var temp = TestCli.Temp("checks");
        var model = OwnershipModel(temp.Path);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "src", "App", "Reporting", "Domain", "Exceptions", "ReportReceiptMissingException.cs"),
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
    /// <proves>litespec/backend/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void CodeCarriedByTwoExceptionTypesReportsDuplicate()
    {
        using var temp = TestCli.Temp("checks");
        var model = OwnershipModel(temp.Path);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "src", "App", "Spending", "Domain", "Exceptions", "ExpenseReceiptMissingException.cs"),
            11,
            "ExpenseReceiptMissingException"));
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "src", "App", "Spending", "Receipts", "Domain", "Exceptions", "ReceiptMissingException.cs"),
            11,
            "ReceiptMissingException"));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "refusal-module-foreign");
        var message = Assert.Single(findings, finding => finding.Code == "refusal-code-duplicate").Message;
        Assert.Contains("ExpenseReceiptMissingException", message, StringComparison.Ordinal);
        Assert.Contains("ReceiptMissingException", message, StringComparison.Ordinal);
    }

    /// <summary>Code Carried By One Exception Type In Its Module Passes.</summary>
    /// <proves>litespec/backend/dotnet/domain#throw-only-refusal-codes-the-module-owns</proves>
    [Fact]
    public void CodeCarriedByOneExceptionTypeInItsModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = OwnershipModel(temp.Path);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            Path.Combine(temp.Path, "src", "App", "Spending", "Domain", "Exceptions", "ExpenseReceiptMissingException.cs"),
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
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": "src/App"}}}
            """);
        Directory.CreateDirectory(Path.Combine(root, "src", "App", "Spending"));
        Directory.CreateDirectory(Path.Combine(root, "src", "App", "Reporting"));

        var model = TestModel.Model(TestModel.Config(root));
        model.RefusalSets.Add(new RefusalEntry(
            "spending",
            "spending.receipt-missing",
            "The expense is above the receipt threshold and carries no receipt.",
            "https://claimwick.dev/problems/receipt-missing",
            Path.Combine(root, "refusals.yaml")));

        return model;
    }
}
