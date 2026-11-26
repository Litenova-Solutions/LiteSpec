using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the Contracts closed set: aggregate folders with the identifier, an On fact folder per fact with the event and its payload parts, and public use case folders, no interface, no stray command, each public use case message at its Contracts path, and each declared fact with its folder.</summary>
public sealed class ContractFolderChecksTests
{
    /// <summary>An Aggregate Folder With Identifier Facts And Public Query Reports Nothing.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void AggregateFolderWithIdentifierFactsAndPublicQueryReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        var loans = ContractsFolder(temp.Path, "Loans");
        Directory.CreateDirectory(loans);
        File.WriteAllText(Path.Combine(loans, "LoanId.cs"), "namespace Library.Lending.Contracts.Loans;\n\npublic readonly record struct LoanId(Guid Value);\n");
        var borrowed = Path.Combine(loans, "OnLoanBorrowed");
        Directory.CreateDirectory(borrowed);
        File.WriteAllText(Path.Combine(borrowed, "LoanBorrowedModuleEvent.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanBorrowed;\n\npublic sealed record LoanBorrowedModuleEvent(Guid LoanId);\n");
        File.WriteAllText(Path.Combine(borrowed, "BorrowedCopy.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanBorrowed;\n\npublic sealed record BorrowedCopy(Guid CopyId);\n");
        var lent = Path.Combine(loans, "OnLoanLent");
        Directory.CreateDirectory(lent);
        File.WriteAllText(Path.Combine(lent, "LoanLentIntegrationEvent.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanLent;\n\npublic sealed record LoanLentIntegrationEvent(Guid LoanId);\n");
        var readLoan = Path.Combine(loans, "ReadLoan");
        Directory.CreateDirectory(readLoan);
        File.WriteAllText(Path.Combine(readLoan, "ReadLoanQuery.cs"), "namespace Library.Lending.Contracts.Loans.ReadLoan;\n\npublic sealed record ReadLoanQuery(Guid LoanId);\n");
        File.WriteAllText(Path.Combine(readLoan, "ReadLoanResult.cs"), "namespace Library.Lending.Contracts.Loans.ReadLoan;\n\npublic sealed record ReadLoanResult(Guid LoanId);\n");
        var readLedger = ContractsFolder(temp.Path, "ReadLedger");
        Directory.CreateDirectory(readLedger);
        File.WriteAllText(Path.Combine(readLedger, "ReadLedgerQuery.cs"), "namespace Library.Lending.Contracts.ReadLedger;\n\npublic sealed record ReadLedgerQuery();\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A Fact Beside Its Contracts Aggregate Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void FactBesideItsContractsAggregateReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        var loans = ContractsFolder(temp.Path, "Loans");
        Directory.CreateDirectory(loans);
        File.WriteAllText(Path.Combine(loans, "LoanBorrowedModuleEvent.cs"), "namespace Library.Lending.Contracts.Loans;\n\npublic sealed record LoanBorrowedModuleEvent(Guid LoanId);\n");

        var findings = Run(model);
        var finding = Assert.Single(findings, found => found.Message.Contains("holds a fact outside its On fact folder", StringComparison.Ordinal));
        Assert.Contains("it belongs in 'Loans/OnLoanBorrowed/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Fact File Naming No Declared Fact Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void FactFileNamingNoDeclaredFactReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        var borrowed = ContractsFolder(temp.Path, "Loans", "OnLoanBorrowed");
        Directory.CreateDirectory(borrowed);
        File.WriteAllText(Path.Combine(borrowed, "LoanBorrowedModuleEvent.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanBorrowed;\n\npublic sealed record LoanBorrowedModuleEvent(Guid LoanId);\n");
        File.WriteAllText(Path.Combine(borrowed, "LoanVanishedModuleEvent.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanBorrowed;\n\npublic sealed record LoanVanishedModuleEvent(Guid LoanId);\n");
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanLent"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds a module event naming no declared module event of module 'Lending'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Fact In The Wrong Aggregate On Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void FactInWrongAggregateOnFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        model.Records.Add(Rec("aggregate", "aggregate/lending.copy", "lending", "aggregates/copies", """{"summary": "A physical copy."}""", temp.Path));
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanBorrowed"));
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanLent"));
        var copies = ContractsFolder(temp.Path, "Copies", "OnLoanBorrowed");
        Directory.CreateDirectory(copies);
        File.WriteAllText(Path.Combine(copies, "LoanBorrowedModuleEvent.cs"), "namespace Library.Lending.Contracts.Copies.OnLoanBorrowed;\n\npublic sealed record LoanBorrowedModuleEvent(Guid LoanId);\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds the fact 'module-event/lending.loan-borrowed'", finding.Message);
        Assert.Contains("it belongs in 'Loans/OnLoanBorrowed/'", finding.Message);
        Assert.Equal("module-event/lending.loan-borrowed", finding.Subject);
    }

    /// <summary>A Fact Of Another On Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void FactOfAnotherOnFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        var borrowed = ContractsFolder(temp.Path, "Loans", "OnLoanBorrowed");
        Directory.CreateDirectory(borrowed);
        File.WriteAllText(Path.Combine(borrowed, "LoanLentIntegrationEvent.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanBorrowed;\n\npublic sealed record LoanLentIntegrationEvent(Guid LoanId);\n");
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanLent"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds the fact 'integration-event/loan-lent'", finding.Message);
        Assert.Contains("it belongs in 'Loans/OnLoanLent/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Declared Fact With No On Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void DeclaredFactWithNoOnFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanBorrowed"));
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds no 'OnLoanLent/' folder for declared integration event 'integration-event/loan-lent'", finding.Message);
        Assert.Equal("integration-event/loan-lent", finding.Subject);
    }

    /// <summary>An Unknown Contracts Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void UnknownContractsFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(ContractsFolder(temp.Path, "DomainEvents"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("names no declared aggregate or public module-level use case", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>An Unknown On Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void UnknownOnFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanBorrowed"));
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanLent"));
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanVanished"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("names no public use case or On fact folder of aggregate 'Loans'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Non Public Use Case Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void NonPublicUseCaseFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        var loans = ContractsFolder(temp.Path, "Loans");
        Directory.CreateDirectory(loans);
        Directory.CreateDirectory(Path.Combine(loans, "RetireCopy"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("which is no public use case", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A File Directly In Contracts Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void FileDirectlyInContractsReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans"));
        File.WriteAllText(Path.Combine(ContractsFolder(temp.Path), "LendingContracts.cs"), "namespace Library.Lending.Contracts;\n\npublic static class LendingContracts\n{\n}\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("sits directly in Contracts/", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>An Interface In Contracts Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void InterfaceInContractsReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        var loans = ContractsFolder(temp.Path, "Loans");
        Directory.CreateDirectory(loans);
        File.WriteAllText(Path.Combine(loans, "ILoanReader.cs"), "namespace Library.Lending.Contracts.Loans;\n\npublic interface ILoanReader\n{\n}\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("sits in Contracts, which publishes data only", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Command Beside Its Contracts Aggregate Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void CommandBesideItsContractsAggregateReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        var loans = ContractsFolder(temp.Path, "Loans");
        Directory.CreateDirectory(loans);
        File.WriteAllText(Path.Combine(loans, "BorrowCopyCommand.cs"), "namespace Library.Lending.Contracts.Loans;\n\npublic sealed record BorrowCopyCommand(Guid CopyId);\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds a command message outside a use case folder", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Public Command Folder With Message And Result Reports Nothing.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void PublicCommandFolderWithMessageAndResultReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy", "lending", "aggregates/loans/use-cases/borrow-copy", """{"kind": "command", "reachedThrough": ["module/finance"]}""", temp.Path));
        var borrow = ContractsFolder(temp.Path, "Loans", "BorrowCopy");
        Directory.CreateDirectory(borrow);
        File.WriteAllText(Path.Combine(borrow, "BorrowCopyCommand.cs"), "namespace Library.Lending.Contracts.Loans.BorrowCopy;\n\npublic sealed record BorrowCopyCommand(Guid CopyId);\n");
        File.WriteAllText(Path.Combine(borrow, "BorrowCopyCommandResult.cs"), "namespace Library.Lending.Contracts.Loans.BorrowCopy;\n\npublic sealed record BorrowCopyCommandResult(Guid CopyId);\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A Stray Command File In A Public Command Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void StrayCommandFileInPublicCommandFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy", "lending", "aggregates/loans/use-cases/borrow-copy", """{"kind": "command", "reachedThrough": ["module/finance"]}""", temp.Path));
        var borrow = ContractsFolder(temp.Path, "Loans", "BorrowCopy");
        Directory.CreateDirectory(borrow);
        File.WriteAllText(Path.Combine(borrow, "BorrowCopyCommand.cs"), "namespace Library.Lending.Contracts.Loans.BorrowCopy;\n\npublic sealed record BorrowCopyCommand(Guid CopyId);\n");
        File.WriteAllText(Path.Combine(borrow, "RetireCopyCommand.cs"), "namespace Library.Lending.Contracts.Loans.BorrowCopy;\n\npublic sealed record RetireCopyCommand(Guid CopyId);\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds a command of no public use case", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Command File In An On Fact Folder Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void CommandFileInOnFactFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        AddFactRecords(model, temp.Path);
        var borrowed = ContractsFolder(temp.Path, "Loans", "OnLoanBorrowed");
        Directory.CreateDirectory(borrowed);
        File.WriteAllText(Path.Combine(borrowed, "LoanBorrowedModuleEvent.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanBorrowed;\n\npublic sealed record LoanBorrowedModuleEvent(Guid LoanId);\n");
        File.WriteAllText(Path.Combine(borrowed, "BorrowCopyCommand.cs"), "namespace Library.Lending.Contracts.Loans.OnLoanBorrowed;\n\npublic sealed record BorrowCopyCommand(Guid CopyId);\n");
        Directory.CreateDirectory(ContractsFolder(temp.Path, "Loans", "OnLoanLent"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds a command of no public use case", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Public Command Message In The Module Project Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void PublicCommandMessageInModuleProjectReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy", "lending", "aggregates/loans/use-cases/borrow-copy", """{"kind": "command", "reachedThrough": ["module/finance"]}""", temp.Path));
        var useCase = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Application", "Loans", "BorrowCopy");
        Directory.CreateDirectory(useCase);
        File.WriteAllText(Path.Combine(useCase, "BorrowCopyCommand.cs"), "namespace Library.Lending.Application.Loans.BorrowCopy;\n\npublic sealed record BorrowCopyCommand(Guid CopyId);\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds the message of public command 'use-case/lending.borrow-copy'", finding.Message);
        Assert.Contains("it belongs in 'Lending/Contracts/Loans/BorrowCopy/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Public Query Message In The Module Project Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void PublicQueryMessageInModuleProjectReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = FolderModel(temp.Path);
        var useCase = Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Application", "Loans", "ReadLoan");
        Directory.CreateDirectory(useCase);
        File.WriteAllText(Path.Combine(useCase, "ReadLoanQuery.cs"), "namespace Library.Lending.Application.Loans.ReadLoan;\n\npublic sealed record ReadLoanQuery(Guid LoanId);\n");

        var finding = Assert.Single(Run(model));
        Assert.Contains("it belongs in 'Lending/Contracts/Loans/ReadLoan/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Public Query Message In A Sibling Module Project Reports Drift.</summary>
    /// <proves>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void SiblingPublicQueryMessageReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"paths\": {\"testRoots\": [\"apps/api/tests\"]}, \"stackConfig\": {\"dotnet\": {\"modules\": [\"apps/api/src/Modules/Library.Lending\", \"apps/api/src/Modules/Library.Lending.Contracts\"], \"kernel\": \"apps/api/src/Library.Kernel\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        AddLendingRecords(model, temp.Path);
        var useCase = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "ReadLoan");
        Directory.CreateDirectory(useCase);
        File.WriteAllText(Path.Combine(useCase, "ReadLoanQuery.cs"), "namespace Library.Lending.Application.Loans.ReadLoan;\n\npublic sealed record ReadLoanQuery(Guid LoanId);\n");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending.Contracts"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("it belongs in 'Library.Lending.Contracts/Loans/ReadLoan/'", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Contracts Aggregate Between Expected Segments Matches.</summary>
    /// <proves>litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ContractsAggregateBetweenExpectedSegmentsMatches()
    {
        Assert.True(NamespacePlacement.NamespaceMatches(["Lending", "Contracts", "Loans"], ["Lending", "Loans"]));
    }

    /// <summary>Contracts On Folder After Expected Segments Matches.</summary>
    /// <proves>litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ContractsOnFolderAfterExpectedSegmentsMatches()
    {
        Assert.True(NamespacePlacement.NamespaceMatches(["Lending", "Contracts", "Loans", "OnLoanBorrowed"], ["Lending", "Loans"]));
    }

    /// <summary>An Unknown Folder Between Expected Segments Mismatches.</summary>
    /// <proves>litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void UnknownFolderBetweenExpectedSegmentsMismatches()
    {
        Assert.False(NamespacePlacement.NamespaceMatches(["Lending", "Contracts", "DomainEvents", "Loans"], ["Lending", "Loans"]));
    }

    /// <summary>A ModuleEvents Folder Between Expected Segments Mismatches.</summary>
    /// <proves>litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleEventsFolderBetweenExpectedSegmentsMismatches()
    {
        Assert.False(NamespacePlacement.NamespaceMatches(["Lending", "Contracts", "ModuleEvents", "Loans"], ["Lending", "Loans"]));
    }

    /// <summary>A Module Event Contract Record Reads Technical.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ModuleEventContractRecordReadsTechnical()
    {
        using var temp = TestCli.Temp("reader");
        WriteReaderProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending.Contracts", "Expenses", "OnExpenseReimbursed"), "ExpenseReimbursed.cs", """
            namespace Library.Spending.Contracts.Expenses.OnExpenseReimbursed;

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
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
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
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void NestedModuleEventContractReadsStray()
    {
        using var temp = TestCli.Temp("reader");
        WriteReaderProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Library.Spending.Contracts", "Expenses", "OnExpenseReimbursed"), "ExpenseContracts.cs", """
            namespace Library.Spending.Contracts.Expenses.OnExpenseReimbursed;

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

    /// <summary>A model with the modules root and the declared lending module, aggregate and use cases.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel FolderModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"paths\": {\"testRoots\": [\"apps/api/tests\"]}, \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"kernel\": \"apps/api/src/Library.Kernel\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };
        AddLendingRecords(model, root);

        return model;
    }

    /// <summary>Adds the lending module, the loan aggregate, two public queries and one command to the model.</summary>
    /// <param name="model">The project model receiving the records.</param>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    static void AddLendingRecords(ProjectModel model, string root)
    {
        model.Records.Add(Rec("module", "module/lending", "lending", "", """{"summary": "Lending lends copies to readers."}""", root));
        model.Records.Add(Rec("aggregate", "aggregate/lending.loan", "lending", "aggregates/loans", """{"summary": "A lent copy."}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.read-loan", "lending", "aggregates/loans/use-cases/read-loan", """{"kind": "query", "reachedThrough": ["module/finance"]}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.read-ledger", "lending", "use-cases/read-ledger", """{"kind": "query", "reachedThrough": ["module/finance"]}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.retire-copy", "lending", "aggregates/loans/use-cases/retire-copy", """{"kind": "command"}""", root));
    }

    /// <summary>Adds two loan domain events with the module event and integration event translating them.</summary>
    /// <param name="model">The project model receiving the records.</param>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    static void AddFactRecords(ProjectModel model, string root)
    {
        model.Records.Add(Rec("domain-event", "domain-event/loan.borrowed", "lending", "aggregates/loans", """{}""", root));
        model.Records.Add(Rec("domain-event", "domain-event/loan.lent", "lending", "aggregates/loans", """{}""", root));
        model.Records.Add(Rec("module-event", "module-event/lending.loan-borrowed", "lending", "aggregates/loans/module-events/loan-borrowed", """{"translates": "domain-event/loan.borrowed"}""", root));
        model.Records.Add(Rec("integration-event", "integration-event/loan-lent", "lending", "aggregates/loans/integration-events/loan-lent", """{"translates": "domain-event/loan.lent"}""", root));
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
            File = Path.Combine(root, ".litespec", "lending.yaml"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
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
