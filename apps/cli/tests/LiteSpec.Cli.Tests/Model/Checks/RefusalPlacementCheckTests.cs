using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the refusal placement checks: a refusal declared under an aggregate is carried under that aggregate, and a module-level refusal raised by exactly one aggregate belongs under it.</summary>
public sealed class RefusalPlacementCheckTests
{
    /// <summary>Refusal Declared Under Its Aggregate And Carried There Passes.</summary>
    /// <proves>litespec/core/use-cases#declare-each-aggregates-refusals-once</proves>
    [Fact]
    public void RefusalDeclaredUnderItsAggregateAndCarriedTherePasses()
    {
        using var temp = TestCli.Temp("checks");
        var carrier = CarrierFile(temp.Path, "Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs");
        var model = PlacementModel(temp.Path, "expenses");
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", carrier, 11, "ExpenseReceiptMissingException"));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "code-placement-drift" && finding.Path == carrier);
    }

    /// <summary>Refusal Declared Under An Aggregate But Carried Under Another Reports Drift.</summary>
    /// <proves>litespec/core/use-cases#declare-each-aggregates-refusals-once</proves>
    [Fact]
    public void RefusalDeclaredUnderAnAggregateButCarriedUnderAnotherReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var carrier = CarrierFile(temp.Path, "Spending", "Domain", "Receipts", "Exceptions", "ReceiptMissingException.cs");
        var model = PlacementModel(temp.Path, "expenses");
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", carrier, 11, "ReceiptMissingException"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "code-placement-drift" && found.Path == carrier);

        Assert.Contains("spending.receipt-missing", finding.Message, StringComparison.Ordinal);
        Assert.Contains("expenses", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Receipts", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Refusal Declared Under An Aggregate But Carried Directly In Domain Reports Drift.</summary>
    /// <proves>litespec/core/use-cases#declare-each-aggregates-refusals-once</proves>
    [Fact]
    public void RefusalDeclaredUnderAnAggregateButCarriedDirectlyInDomainReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var carrier = CarrierFile(temp.Path, "Spending", "Domain", "ExpenseReceiptMissingException.cs");
        var model = PlacementModel(temp.Path, "expenses");
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", carrier, 11, "ExpenseReceiptMissingException"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "code-placement-drift" && found.Path == carrier);

        Assert.Contains("directly in Domain/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Refusal Declared Under An Aggregate But Carried Outside Domain Reports Drift.</summary>
    /// <proves>litespec/core/use-cases#declare-each-aggregates-refusals-once</proves>
    [Fact]
    public void RefusalDeclaredUnderAnAggregateButCarriedOutsideDomainReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var carrier = CarrierFile(temp.Path, "Spending", "Application", "Expenses", "SubmitExpense", "ExpenseReceiptMissingException.cs");
        var model = PlacementModel(temp.Path, "expenses");
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", carrier, 11, "ExpenseReceiptMissingException"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "code-placement-drift" && found.Path == carrier);

        Assert.Contains("outside Domain/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Refusal Declared Under An Aggregate But Carried From Another Module Passes Placement.</summary>
    /// <proves>litespec/core/use-cases#declare-each-aggregates-refusals-once</proves>
    [Fact]
    public void RefusalDeclaredUnderAnAggregateButCarriedFromAnotherModulePassesPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var carrier = CarrierFile(temp.Path, "Reporting", "Domain", "Reports", "Exceptions", "ReportReceiptMissingException.cs");
        var model = PlacementModel(temp.Path, "expenses");
        model.Code.Literals.Add(new CodeLiteral("spending.receipt-missing", carrier, 11, "ReportReceiptMissingException"));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "code-placement-drift" && finding.Path == carrier);
    }

    /// <summary>Module Level Refusal Raised By Exactly One Aggregate Reports Placement.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ModuleLevelRefusalRaisedByExactlyOneAggregateReportsPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var model = PlacementModel(temp.Path, null);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            CarrierFile(temp.Path, "Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs"),
            11,
            "ExpenseReceiptMissingException"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-placement-invalid");

        Assert.Contains("spending.receipt-missing", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Expenses", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Module Level Refusal Raised By Two Aggregates Passes Placement.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ModuleLevelRefusalRaisedByTwoAggregatesPassesPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var model = PlacementModel(temp.Path, null);
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            CarrierFile(temp.Path, "Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs"),
            11,
            "ExpenseReceiptMissingException"));
        model.Code.Literals.Add(new CodeLiteral(
            "spending.receipt-missing",
            CarrierFile(temp.Path, "Spending", "Domain", "Receipts", "Exceptions", "ReceiptMissingException.cs"),
            11,
            "ReceiptMissingException"));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "record-placement-invalid");
    }

    /// <summary>Module Level Refusal Raised By No Code Passes Placement.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ModuleLevelRefusalRaisedByNoCodePassesPlacement()
    {
        using var temp = TestCli.Temp("checks");
        var model = PlacementModel(temp.Path, null);

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "record-placement-invalid");
    }

    /// <summary>Generator Exception Under Another Aggregate Reports Drift From Source.</summary>
    /// <proves>litespec/core/use-cases#declare-each-aggregates-refusals-once</proves>
    [Fact]
    public void GeneratorExceptionUnderAnotherAggregateReportsDriftFromSource()
    {
        using var temp = TestCli.Temp("checks");
        var model = PlacementModel(temp.Path, "expenses");
        var carrier = WriteGeneratorException(
            temp.Path,
            ["Spending", "Domain", "Receipts", "Exceptions", "ReceiptMissingException.cs"],
            "ReceiptMissingException",
            "rule/spending.receipt-required");
        AddRefusingRule(model, temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "code-placement-drift" && found.Path == carrier);

        Assert.Contains("spending.receipt-missing", finding.Message, StringComparison.Ordinal);
        Assert.Contains("expenses", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Receipts", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Generator Exception Under Its Aggregate Passes From Source.</summary>
    /// <proves>litespec/core/use-cases#declare-each-aggregates-refusals-once</proves>
    [Fact]
    public void GeneratorExceptionUnderItsAggregatePassesFromSource()
    {
        using var temp = TestCli.Temp("checks");
        var model = PlacementModel(temp.Path, "expenses");
        var carrier = WriteGeneratorException(
            temp.Path,
            ["Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs"],
            "ExpenseReceiptMissingException",
            "rule/spending.receipt-required");
        AddRefusingRule(model, temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "code-placement-drift" && finding.Path == carrier);
    }

    /// <summary>Module Level Refusal Raised By One Generator Exception Reports Placement From Source.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ModuleLevelRefusalRaisedByOneGeneratorExceptionReportsPlacementFromSource()
    {
        using var temp = TestCli.Temp("checks");
        var model = PlacementModel(temp.Path, null);
        WriteGeneratorException(
            temp.Path,
            ["Spending", "Domain", "Expenses", "Exceptions", "ExpenseReceiptMissingException.cs"],
            "ExpenseReceiptMissingException",
            "rule/spending.receipt-required");
        AddRefusingRule(model, temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-placement-invalid" && found.Message.Contains("spending.receipt-missing", StringComparison.Ordinal));

        Assert.Contains("spending.receipt-missing", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Expenses", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Module Level Refusal Carried Outside Domain Reports Drift From Source.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ModuleLevelRefusalCarriedOutsideDomainReportsDriftFromSource()
    {
        using var temp = TestCli.Temp("checks");
        var model = PlacementModel(temp.Path, null);
        var carrier = WriteGeneratorException(
            temp.Path,
            ["Spending", "Application", "Expenses", "SubmitExpense", "ExpenseReceiptMissingException.cs"],
            "ExpenseReceiptMissingException",
            "rule/spending.receipt-required");
        AddRefusingRule(model, temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "code-placement-drift" && found.Path == carrier);

        Assert.Contains("outside Domain/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>One carrier file path under the modules root, with its folders created.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="segments">The path segments under the modules root.</param>
    /// <returns>The full path to the file.</returns>
    static string CarrierFile(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library.Modules" };
        parts.AddRange(segments);
        var file = Path.Combine([.. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        return file;
    }

    /// <summary>A model whose spending set declares the receipt-missing code under one aggregate folder, or at module level, with spending and reporting module folders.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="aggregate">The aggregate folder holding the refusal set, or null for a module-level refusal.</param>
    /// <returns>The project model holding the refusal set.</returns>
    static ProjectModel PlacementModel(string root, string? aggregate)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(Path.Combine(root, ".litespec/project.yaml"), """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {"modules": "apps/api/src/Library.Modules"}}}
            """);
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Modules", "Spending"));
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Modules", "Reporting"));

        var model = TestModel.Model(TestModel.Config(root));
        var file = aggregate is null
            ? Path.Combine(root, "refusals.yaml")
            : Path.Combine(root, "aggregates", aggregate, "refusals.yaml");
        model.RefusalSets.Add(new RefusalEntry(
            "spending",
            "spending.receipt-missing",
            "The expense is above the receipt threshold and carries no receipt.",
            file,
            aggregate));

        return model;
    }

    /// <summary>One generator-shaped exception file under the modules root: a sealed partial type with a rule mark and no Code member.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="segments">The path segments under the modules root.</param>
    /// <param name="className">The exception class name.</param>
    /// <param name="ruleId">The rule mark the exception carries.</param>
    /// <returns>The full path to the file.</returns>
    static string WriteGeneratorException(string root, string[] segments, string className, string ruleId)
    {
        var file = CarrierFile(root, segments);
        File.WriteAllText(file, """
            namespace Library.Modules.Spending.Domain.Exceptions;

            /// <implements>RULE</implements>
            public sealed partial class CLASS : DomainException
            {
                public CLASS()
                    : base("The receipt is missing.")
                {
                }
            }
            """.Replace("RULE", ruleId, StringComparison.Ordinal).Replace("CLASS", className, StringComparison.Ordinal));

        return file;
    }

    /// <summary>Adds the rule refusing with the receipt-missing code, so a generator-shaped mark resolves.</summary>
    /// <param name="model">The project model holding the refusal set.</param>
    /// <param name="root">The consumer root folder the rule file sits under.</param>
    static void AddRefusingRule(ProjectModel model, string root) =>
        model.Records.Add(TestModel.Record(
            "rule",
            "rule/spending.receipt-required",
            Path.Combine(root, "rules.yaml"),
            """{"kind": "precondition", "refusal": "refusal/spending.receipt-missing"}"""));
}
