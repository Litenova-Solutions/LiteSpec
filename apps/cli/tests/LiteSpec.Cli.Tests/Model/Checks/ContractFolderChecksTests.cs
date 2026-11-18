using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the module-event contract handling: Contracts/ModuleEvents/ is a Contracts kind folder, ModuleEvents is a placeless namespace segment, and a module-event contract record reads technical by its mark.</summary>
public sealed class ContractFolderChecksTests
{
    /// <summary>A ModuleEvents Contracts Kind Folder Reports Nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void ModuleEventsContractsKindFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        var folder = ContractsFolder(temp.Path, "ModuleEvents");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "ExpenseReimbursed.cs"), "namespace Library.Lending.Contracts.ModuleEvents;\n\npublic sealed record ExpenseReimbursed(Guid ExpenseId);\n");

        Assert.Empty(Run(model));
    }

    /// <summary>An Unknown Contracts Kind Folder Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void UnknownContractsKindFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(ContractsFolder(temp.Path, "DomainEvents"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("names no Contracts kind folder", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A File Directly In Contracts Beside ModuleEvents Reports Drift.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void FileDirectlyInContractsBesideModuleEventsReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(ContractsFolder(temp.Path, "ModuleEvents"));
        File.WriteAllText(Path.Combine(ContractsFolder(temp.Path), "ExpenseContracts.cs"), "namespace Library.Lending.Contracts;\n\npublic static class ExpenseContracts\n{\n}\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("sits directly in Contracts/", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>ModuleEvents Between Expected Segments Matches.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleEventsBetweenExpectedSegmentsMatches()
    {
        Assert.True(NamespacePlacement.NamespaceMatches(["Lending", "Contracts", "ModuleEvents", "Loans"], ["Lending", "Loans"]));
    }

    /// <summary>An Unknown Folder Between Expected Segments Mismatches.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void UnknownFolderBetweenExpectedSegmentsMismatches()
    {
        Assert.False(NamespacePlacement.NamespaceMatches(["Lending", "Contracts", "DomainEvents", "Loans"], ["Lending", "Loans"]));
    }

    /// <summary>ModuleEvents Matching Ignores Case.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleEventsMatchingIgnoresCase()
    {
        Assert.True(NamespacePlacement.NamespaceMatches(["Lending", "Contracts", "moduleevents", "Loans"], ["Lending", "Loans"]));
    }

    /// <summary>A Module Event Contract Record Reads Technical.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ModuleEventContractRecordReadsTechnical()
    {
        using var temp = TestCli.Temp("reader");
        WriteReaderProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending.Contracts", "ModuleEvents"), "ExpenseReimbursed.cs", """
            namespace Library.Spending.Contracts.ModuleEvents;

            /// <implements>module-event/spending.expense-reimbursed</implements>
            public sealed record ExpenseReimbursed(Guid ExpenseId);
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        var element = Assert.Single(code.Elements);
        Assert.Equal(CodeKind.Technical, element.Kind);
        Assert.Equal("ExpenseReimbursed", element.Class);
        Assert.Equal("module-event/spending.expense-reimbursed", Assert.Single(element.Marks).RecordId);
        Assert.Empty(code.StrayMarks);
    }

    /// <summary>A Module Event Mark In Domain Reads Stray.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ModuleEventMarkInDomainReadsStray()
    {
        using var temp = TestCli.Temp("reader");
        WriteReaderProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending", "Domain", "Spending"), "ExpenseReimbursed.cs", """
            namespace Library.Spending.Domain.Spending;

            /// <implements>module-event/spending.expense-reimbursed</implements>
            public sealed record ExpenseReimbursed(Guid ExpenseId);
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Empty(code.Elements);
        var stray = Assert.Single(code.StrayMarks);
        Assert.Equal("ExpenseReimbursed", stray.Class);
    }

    /// <summary>A Nested Module Event Contract Reads Stray.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void NestedModuleEventContractReadsStray()
    {
        using var temp = TestCli.Temp("reader");
        WriteReaderProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending.Contracts", "ModuleEvents"), "ExpenseContracts.cs", """
            namespace Library.Spending.Contracts.ModuleEvents;

            public static class ExpenseContracts
            {
                /// <implements>module-event/spending.expense-reimbursed</implements>
                public sealed record ExpenseReimbursed(Guid ExpenseId);
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Empty(code.Elements);
        var stray = Assert.Single(code.StrayMarks);
        Assert.Equal("ExpenseReimbursed", stray.Class);
    }

    /// <summary>Runs the closed folder check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The code placement findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        ClosedFolderChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "code-placement-drift").ToList();
    }

    /// <summary>A model with the modules root and the declared lending module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the module record.</returns>
    static ProjectModel FolderModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"paths\": {\"testRoots\": [\"apps/api/tests\"]}, \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"kernel\": \"apps/api/src/Library.Kernel\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };
        model.Records.Add(TestModel.Record("module", "module/lending", Path.Combine(root, ".litespec", "lending.yaml"), """{"summary": "Lending lends copies to readers."}"""));

        return model;
    }

    /// <summary>One folder under the lending module Contracts folder.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="segments">The path segments under the Contracts folder.</param>
    /// <returns>The full path to the folder.</returns>
    static string ContractsFolder(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library.Modules", "Lending", "Contracts" };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }

    /// <summary>Writes the reader fixture project with the module, its sibling Contracts project, and the layer roots.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteReaderProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": ["src/Library.Spending", "src/Library.Spending.Contracts"], "webapi": "src/WebApi"}}}
            """);

    /// <summary>Writes one C# source file under the fixture source root.</summary>
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
}
