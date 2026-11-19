using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Proof;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a use case holding scenarios keeps its Application folder: its record promises behavior its module folder holds.</summary>
public sealed class UseCaseFolderCheckTests
{
    /// <summary>Use Case With Scenarios And Its Application Folder Passes.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UseCaseWithScenariosAndItsApplicationFolderPasses()
    {
        using var temp = TestCli.Temp("proof");
        WriteProject(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "BorrowCopy"));
        var model = FolderModel(temp.Path);

        Assert.Empty(Run(model));
    }

    /// <summary>Use Case With Scenarios And No Application Folder Reports.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UseCaseWithScenariosAndNoApplicationFolderReports()
    {
        using var temp = TestCli.Temp("proof");
        WriteProject(temp.Path);
        var model = FolderModel(temp.Path);

        var finding = Assert.Single(Run(model));
        Assert.Equal("use-case-unimplemented", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' has scenarios but no 'Application/Loans/BorrowCopy/' folder.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>Use Case With No Scenarios And No Application Folder Passes.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void UseCaseWithNoScenariosAndNoApplicationFolderPasses()
    {
        using var temp = TestCli.Temp("proof");
        WriteProject(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            UseCase(temp.Path));

        Assert.Empty(Run(model));
    }

    /// <summary>Runs the proof checks and keeps the missing-folder findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The use-case-unimplemented findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        ProofChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "use-case-unimplemented").ToList();
    }

    /// <summary>A model with the modules root and the borrow-copy use case holding one scenario.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel FolderModel(string root) =>
        TestModel.Model(
            TestModel.Config(root),
            UseCase(root),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-an-available-copy-succeeds", Path.Combine(root, "borrow.yaml"), """{"title": "Borrowing an available copy succeeds"}"""));

    /// <summary>The borrow-copy use case record in its module aggregates folder.</summary>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <returns>The use case record.</returns>
    static LiteSpec.Cli.Model.Records.Record UseCase(string root)
    {
        using var document = JsonDocument.Parse("{}");

        return new LiteSpec.Cli.Model.Records.Record
        {
            Type = "use-case",
            Id = "use-case/lending.borrow-copy",
            Title = "use-case/lending.borrow-copy",
            File = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml"),
            Line = 1,
            Folder = ".litespec/behavior/modules/lending/aggregates/loans/use-cases/borrow-copy",
            Module = "lending",
            Raw = document.RootElement.Clone(),
        };
    }

    /// <summary>Writes the project file declaring the lending module project.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Modules/Library.Lending\"}}}");
    }
}
