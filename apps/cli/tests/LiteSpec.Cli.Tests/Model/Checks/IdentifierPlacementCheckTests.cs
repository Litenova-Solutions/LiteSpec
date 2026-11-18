using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves every business identifier lives in the Contracts or Domain folder of the module owning its aggregate, the Kernel holds one only with a naming decision, and the Platform holds none.</summary>
public sealed class IdentifierPlacementCheckTests
{
    /// <summary>Shared Identifier In Contracts Passes.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void SharedIdentifierInContractsPasses()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending.Contracts", "Expenses"), "ExpenseId.cs", """
            namespace Library.Spending.Contracts.Expenses;

            public readonly record struct ExpenseId(Guid Value) : ITypedId<Guid>;
            """);
        WriteNaming(temp.Path);

        Assert.Empty(Run(model));
    }

    /// <summary>Shared Identifier In Domain Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void SharedIdentifierInDomainReportsDrift()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Domain", "Expenses"), "ExpenseId.cs", """
            namespace Library.Spending.Domain.Expenses;

            public readonly record struct ExpenseId(Guid Value) : ITypedId<Guid>;
            """);
        WriteNaming(temp.Path);

        var finding = Assert.Single(Run(model));
        Assert.Contains("but another module carries it, so it belongs in 'Library.Spending.Contracts/Expenses/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Unshared Identifier In Domain Passes.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void UnsharedIdentifierInDomainPasses()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Domain", "Expenses"), "ExpenseId.cs", """
            namespace Library.Spending.Domain.Expenses;

            public readonly record struct ExpenseId(Guid Value) : ITypedId<Guid>;
            """);

        Assert.Empty(Run(model));
    }

    /// <summary>Unshared Identifier In Contracts Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void UnsharedIdentifierInContractsReportsDrift()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending.Contracts", "Expenses"), "ExpenseId.cs", """
            namespace Library.Spending.Contracts.Expenses;

            public readonly record struct ExpenseId(Guid Value) : ITypedId<Guid>;
            """);

        var finding = Assert.Single(Run(model));
        Assert.Contains("but no other module carries it, so it belongs in 'Spending/Domain/Expenses/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Identifier In Platform Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/architecture#name-no-business-identifier-in-the-platform</proves>
    [Fact]
    public void IdentifierInPlatformReportsDrift()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WritePlatformSource(temp.Path, "ExpenseId.cs", """
            namespace Library.Platform.Persistence;

            public readonly record struct ExpenseId(Guid Value) : ITypedId<Guid>;
            """);

        var finding = Assert.Single(Run(model));
        Assert.Contains("sits in the Platform, which names no business identifier", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Identifier In Kernel Without Decision Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void IdentifierInKernelWithoutDecisionReportsDrift()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WriteKernelSource(temp.Path, "ExpenseId.cs", """
            namespace Library.Kernel;

            public readonly record struct ExpenseId(Guid Value) : ITypedId<Guid>;
            """);

        var finding = Assert.Single(Run(model));
        Assert.Contains("sits in the Kernel with no decision naming it", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Identifier In Kernel With Decision Passes.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void IdentifierInKernelWithDecisionPasses()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        model.Records.Add(Rec("decision", "decision/0007-share-expense-identifier", "", "", """{"summary": "The Kernel holds ExpenseId to break the cycle between the Spending and Reporting Contracts projects."}""", temp.Path));
        WriteKernelSource(temp.Path, "ExpenseId.cs", """
            namespace Library.Kernel;

            public readonly record struct ExpenseId(Guid Value) : ITypedId<Guid>;
            """);

        Assert.Empty(Run(model));
    }

    /// <summary>Guid Struct Without Marker In Wrong Place Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void GuidStructWithoutMarkerInWrongPlaceReportsDrift()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Domain", "Expenses"), "ExpenseId.cs", """
            namespace Library.Spending.Domain.Expenses;

            public readonly record struct ExpenseId(Guid Value);
            """);
        WriteNaming(temp.Path);

        var finding = Assert.Single(Run(model));
        Assert.Contains("but another module carries it, so it belongs in 'Library.Spending.Contracts/Expenses/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Struct With Two Guids Is No Identifier.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</proves>
    [Fact]
    public void StructWithTwoGuidsIsNoIdentifier()
    {
        using var temp = TestCli.Temp("identifiers");
        WriteProject(temp.Path);
        var model = ModelWithExpense(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Domain", "Expenses"), "ExpenseId.cs", """
            namespace Library.Spending.Domain.Expenses;

            public readonly record struct ExpenseId(Guid First, Guid Second);
            """);
        WriteNaming(temp.Path);

        Assert.Empty(Run(model));
    }

    /// <summary>Runs the identifier placement check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The code placement findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        IdentifierPlacementChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "code-placement-drift").ToList();
    }

    /// <summary>A model with the expense aggregate of the spending module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the aggregate record.</returns>
    static ProjectModel ModelWithExpense(string root)
    {
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };
        model.Records.Add(Rec("aggregate", "aggregate/spending.expense", "spending", "aggregates/expenses", """{"summary": "A work expense."}""", root));

        return model;
    }

    /// <summary>One record with its module and folder.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="module">The module name, or empty outside a module.</param>
    /// <param name="under">The folder segments under the module folder, or empty.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <returns>The record.</returns>
    static LiteSpec.Cli.Model.Records.Record Rec(string type, string id, string module, string under, string json, string root)
    {
        using var document = JsonDocument.Parse(json);
        var folder = module.Length == 0 ? "" : $".litespec/behavior/modules/{module}" + (under.Length == 0 ? "" : "/" + under);

        return new LiteSpec.Cli.Model.Records.Record
        {
            Type = type,
            Id = id,
            Folder = folder,
            Module = module.Length == 0 ? null : module,
            Title = id,
            File = Path.Combine(root, ".litespec", "spending.yaml"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }

    /// <summary>Writes the fixture project with two modules, their Contracts projects, the Kernel and the Platform.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": ["src/Library.Spending", "src/Library.Spending.Contracts", "src/Library.Reporting"],
              "kernel": "src/Library.Kernel", "platform": "src/Library.Platform"}}}
            """);
    }

    /// <summary>Writes one C# source file under a module project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="project">The project folder path under the source root.</param>
    /// <param name="file">The file name.</param>
    /// <param name="text">The file text.</param>
    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }

    /// <summary>Writes one C# source file under the Kernel project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="file">The file name.</param>
    /// <param name="text">The file text.</param>
    static void WriteKernelSource(string root, string file, string text) =>
        WriteSource(root, "Library.Kernel", file, text);

    /// <summary>Writes one C# source file under the Platform project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="file">The file name.</param>
    /// <param name="text">The file text.</param>
    static void WritePlatformSource(string root, string file, string text) =>
        WriteSource(root, Path.Combine("Library.Platform", "Persistence"), file, text);

    /// <summary>Writes a reporting file naming the expense identifier.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteNaming(string root) =>
        WriteSource(root, Path.Combine("Library.Reporting", "Application", "Reimbursements"), "ReimbursementCart.cs", """
            namespace Library.Reporting.Application.Reimbursements;

            public sealed class ReimbursementCart
            {
                public ExpenseId Expense { get; init; } = default!;
            }
            """);
}
