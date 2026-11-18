using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the code placement check matches the module layout: module-level use cases, the element's own folder, Contracts publishing, reactions, enforcing aggregates, and mirrored tests.</summary>
public sealed class PlacementDriftModuleLayoutTests
{
    /// <summary>A module-level use case endpoint in its WebApi folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLevelEndpointInWebApiFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Controls", "WebApi", "ReadFeatureFlag", "ReadFeatureFlagEndpoint.cs");
        WriteCode(file, "Library.Controls.WebApi.ReadFeatureFlag", "ReadFeatureFlagEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "ReadFeatureFlagEndpoint", file, "use-case/controls.read-feature-flag"));

        Assert.Empty(Run(model));
        Assert.Empty(FolderDrift(model));
    }

    /// <summary>A module-level use case handler nested under an aggregate folder reports that folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLevelHandlerNestedUnderAggregateReportsFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Controls", "Flags", "Application", "ReadFeatureFlag", "ReadFeatureFlagQueryHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "ReadFeatureFlagQueryHandler", file, "use-case/controls.read-feature-flag"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'ReadFeatureFlagQueryHandler' sits in folder 'Flags', but marks 'use-case/controls.read-feature-flag'.", finding.Message);
        Assert.Equal("use-case/controls.read-feature-flag", finding.Subject);
    }

    /// <summary>An endpoint of an aggregate-owned use case in its WebApi folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedEndpointInWebApiFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Cancellations", "WebApi", "RecordEventChangeAnswer", "RecordEventChangeAnswerEndpoint.cs");
        WriteCode(file, "Library.Cancellations.WebApi.RecordEventChangeAnswer", "RecordEventChangeAnswerEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "RecordEventChangeAnswerEndpoint", file, "use-case/cancellations.record-event-change-answer"));

        Assert.Empty(Run(model));
    }

    /// <summary>An endpoint nested under an aggregate folder reports that folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedEndpointInWrongAggregateReportsFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Cancellations", "EventChanges", "WebApi", "RecordEventChangeAnswer", "RecordEventChangeAnswerEndpoint.cs");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "RecordEventChangeAnswerEndpoint", file, "use-case/cancellations.record-event-change-answer"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Endpoint 'RecordEventChangeAnswerEndpoint' sits in folder 'EventChanges', but marks 'use-case/cancellations.record-event-change-answer'.", finding.Message);
        Assert.Equal("use-case/cancellations.record-event-change-answer", finding.Subject);
    }

    /// <summary>An aggregate-owned handler in its Application folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedHandlerInApplicationFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Cancellations", "Application", "RecordEventChangeAnswer", "RecordEventChangeAnswerCommandHandler.cs");
        WriteCode(file, "Library.Cancellations.Application.RecordEventChangeAnswer", "RecordEventChangeAnswerCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "RecordEventChangeAnswerCommandHandler", file, "use-case/cancellations.record-event-change-answer"));

        Assert.Empty(Run(model));
        Assert.Empty(FolderDrift(model));
    }

    /// <summary>A domain event of a module named like a layer folder reports nothing in its singular Domain aggregate folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void DomainEventOfModuleNamedLikeLayerFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Events", "Domain", "ComingSoonPage", "Events", "ComingSoonPagePublishedEvent.cs");
        WriteCode(file, "Library.Events.Domain.ComingSoonPage.Events", "ComingSoonPagePublishedEvent");
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "ComingSoonPagePublishedEvent", file, "domain-event/coming-soon-page.published"));

        Assert.Empty(Run(model));
    }

    /// <summary>An endpoint beside an aggregate named like its module reports nothing in its WebApi folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void EndpointWithSameNamedAggregateInWebApiFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Directory", "WebApi", "SearchAccounts", "SearchAccountsEndpoint.cs");
        WriteCode(file, "Library.Directory.WebApi.SearchAccounts", "SearchAccountsEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "SearchAccountsEndpoint", file, "use-case/directory.search-accounts"));

        Assert.Empty(Run(model));
    }

    /// <summary>A rule exception in its module Contracts folder reports that Contracts holds no Exceptions kind folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void RuleExceptionInModuleContractsReportsKindFolderDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Contracts", "Exceptions", "LoanNotFoundException.cs");
        WriteCode(file, "Library.Lending.Contracts.Exceptions", "LoanNotFoundException");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "LoanNotFoundException", file, "rule/loan.request-names-an-existing-loan"));

        Assert.Contains(Run(model), finding => finding.Message.Contains("names no Contracts kind folder", StringComparison.Ordinal));
    }

    /// <summary>A product rule exception in the Kernel reports nothing, wherever its record file sits.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ProductRuleExceptionInKernelReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        AddProductRule(model, temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "TextRequiredException.cs");
        WriteCode(file, "Library.Kernel", "TextRequiredException");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "TextRequiredException", file, "rule/library.required-text-is-never-blank"));

        Assert.Empty(Run(model));
    }

    /// <summary>A product rule exception in a module reports that it belongs in the Kernel.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ProductRuleExceptionInAModuleReportsTheKernel()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        AddProductRule(model, temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "Exceptions", "TextRequiredException.cs");
        WriteCode(file, "Library.Lending.Domain.Loans.Exceptions", "TextRequiredException");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "TextRequiredException", file, "rule/library.required-text-is-never-blank"));

        var finding = Assert.Single(Run(model));
        Assert.Equal("Rule exception 'TextRequiredException' implements the product rule 'rule/library.required-text-is-never-blank', so it belongs in the Kernel.", finding.Message);
    }

    /// <summary>Adds the product and one rule it holds, with the rule record filed under a module.</summary>
    /// <param name="model">The model receiving the records.</param>
    /// <param name="root">The fixture root folder.</param>
    static void AddProductRule(ProjectModel model, string root)
    {
        model.Records.Add(Rec(RecordTypes.Product, "product/library", "library", root));
        model.Records.Add(Rec(RecordTypes.Rule, "rule/library.required-text-is-never-blank", "lending/loans/rule/required-text-is-never-blank", root));
    }

    /// <summary>A rule exception in another module Contracts folder reports the module folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void RuleExceptionInAnotherModuleContractsReportsModuleFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Billing", "Contracts", "Exceptions", "LoanNotFoundException.cs");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "LoanNotFoundException", file, "rule/loan.request-names-an-existing-loan"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Rule exception 'LoanNotFoundException' sits in module folder 'Billing', but marks 'rule/loan.request-names-an-existing-loan'.", finding.Message);
        Assert.Equal("rule/loan.request-names-an-existing-loan", finding.Subject);
    }

    /// <summary>A domain event in its module Contracts folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void DomainEventInOwnModuleContractsReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Contracts", "IntegrationEvents", "LoanBorrowedEvent.cs");
        WriteCode(file, "Library.Lending.Contracts.IntegrationEvents", "LoanBorrowedEvent");
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "LoanBorrowedEvent", file, "domain-event/loan.loan-borrowed"));

        Assert.Empty(Run(model));
    }

    /// <summary>A rule exception beside an aggregate its rule governs reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void RuleExceptionBesideGovernedAggregateReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Admission", "Domain", "AdmissionGate", "Exceptions", "AdmissionGateEventMismatchException.cs");
        WriteCode(file, "Library.Admission.Domain.AdmissionGate.Exceptions", "AdmissionGateEventMismatchException");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "AdmissionGateEventMismatchException", file, "rule/scanner-device.device-stands-at-own-event-gate"));

        Assert.Empty(Run(model));
    }

    /// <summary>A rule exception beside an aggregate its rule neither homes nor governs reports that folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void RuleExceptionBesideUngovernedAggregateReportsFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Admission", "Domain", "Boxes", "Exceptions", "AdmissionGateEventMismatchException.cs");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "AdmissionGateEventMismatchException", file, "rule/scanner-device.device-stands-at-own-event-gate"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Rule exception 'AdmissionGateEventMismatchException' sits in folder 'Boxes', but marks 'rule/scanner-device.device-stands-at-own-event-gate'.", finding.Message);
        Assert.Equal("rule/scanner-device.device-stands-at-own-event-gate", finding.Subject);
    }

    /// <summary>A test under its use case folder for a module-level use case reports nothing.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void TestForModuleLevelUseCaseInUseCaseFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = TestFile(temp.Path, "Controls", "ReadFeatureFlag", "ReadFeatureFlagScenarios.cs");
        WriteCode(file, "Library.Acceptance.Tests.Controls.ReadFeatureFlag", "ReadFeatureFlagScenarios");
        model.Code.Elements.Add(TestElement("ReadFeatureFlagScenarios", "Reading_a_flag_succeeds", file, "scenario/read-feature-flag.reading-a-flag-succeeds"));

        Assert.Empty(Run(model));
    }

    /// <summary>A test under its use case folder for an aggregate-owned use case reports nothing.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void TestForAggregateOwnedUseCaseInUseCaseFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = TestFile(temp.Path, "Lending", "BorrowCopy", "BorrowCopyScenarios.cs");
        WriteCode(file, "Library.Acceptance.Tests.Lending.BorrowCopy", "BorrowCopyScenarios");
        model.Code.Elements.Add(TestElement("BorrowCopyScenarios", "Borrowing_an_available_copy_succeeds", file, "scenario/borrow-copy.borrowing-an-available-copy-succeeds"));

        Assert.Empty(Run(model));
    }

    /// <summary>A module-level handler directly under the module leaves the folder to the closed folder check.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLevelHandlerDirectlyUnderModuleReportsFolderOnly()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Controls", "ReadFeatureFlag", "ReadFeatureFlagQueryHandler.cs");
        WriteCode(file, "Library.Controls.ReadFeatureFlag", "ReadFeatureFlagQueryHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "ReadFeatureFlagQueryHandler", file, "use-case/controls.read-feature-flag"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Controls/ReadFeatureFlag' names no module folder.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>An aggregate-owned handler nested under its aggregate folder reports that folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedHandlerNestedUnderAggregateReportsFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Loans", "Application", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'BorrowCopyCommandHandler' sits in folder 'Loans', but marks 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An endpoint flat in its module folder reports the missing layer level.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void EndpointFlatInModuleReportsModuleFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "BorrowCopy", "BorrowCopyEndpoint.cs");
        WriteCode(file, "Library.Lending.BorrowCopy", "BorrowCopyEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "BorrowCopyEndpoint", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Lending/BorrowCopy' names no module folder.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A reaction that runs a use case, in that use case's Application folder, reports nothing.</summary>
    /// <proves>litespec/workspace/naming#place-each-trigger-with-what-it-runs</proves>
    [Fact]
    public void ReactionInItsUseCaseApplicationFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        RunsBorrowCopy(model);
        var file = ProductFile(temp.Path, "Lending", "Application", "BorrowCopy", "SendDueNoticeOnLoanBorrowedHandler.cs");
        WriteCode(file, "Library.Lending.Application.BorrowCopy", "SendDueNoticeOnLoanBorrowedHandler");
        model.Code.Elements.Add(Element(CodeKind.EventHandler, "SendDueNoticeOnLoanBorrowedHandler", file, "reaction/lending.send-due-notice"));

        Assert.Empty(Run(model));
        Assert.Empty(FolderDrift(model));
    }

    /// <summary>A reaction in an Application On fact folder reports the use case folder its effects name, and the folder that names no use case.</summary>
    /// <proves>litespec/workspace/naming#place-each-trigger-with-what-it-runs</proves>
    [Fact]
    public void ReactionInOnFactFolderReportsTriggerDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        RunsBorrowCopy(model);
        var file = ProductFile(temp.Path, "Lending", "Application", "OnLoanBorrowed", "SendDueNoticeOnLoanBorrowedHandler.cs");
        WriteCode(file, "Library.Lending.Application.OnLoanBorrowed", "SendDueNoticeOnLoanBorrowedHandler");
        model.Code.Elements.Add(Element(CodeKind.EventHandler, "SendDueNoticeOnLoanBorrowedHandler", file, "reaction/lending.send-due-notice"));

        var placed = Run(model);

        Assert.Contains(placed, finding => finding.Message == "Event handler 'SendDueNoticeOnLoanBorrowedHandler' sits in folder 'OnLoanBorrowed', but its trigger 'reaction/lending.send-due-notice' belongs in 'Lending/Application/BorrowCopy'.");
        Assert.Contains(placed, finding => finding.Message.StartsWith("Folder ", StringComparison.Ordinal) && finding.Message.EndsWith("OnLoanBorrowed' names no declared use case.", StringComparison.Ordinal));
    }

    /// <summary>Gives the drift model's reaction the effect that runs the borrow copy use case.</summary>
    /// <param name="model">The drift model whose reaction record is changed.</param>
    static void RunsBorrowCopy(ProjectModel model)
    {
        var index = model.Records.FindIndex(record => record.Id == "reaction/lending.send-due-notice");
        var reaction = model.Records[index];
        using var document = JsonDocument.Parse("""{"status": "reviewed", "triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.borrow-copy"}]}""");

        model.Records[index] = new LiteSpec.Cli.Model.Records.Record
        {
            Type = reaction.Type,
            Id = reaction.Id,
            Folder = reaction.Folder,
            Module = reaction.Module,
            Title = reaction.Title,
            File = reaction.File,
            Line = reaction.Line,
            Raw = document.RootElement.Clone(),
        };
    }

    /// <summary>A file directly in a module Contracts folder reports the kind folders.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void FileDirectlyInContractsReportsContractsKindFolders()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Contracts", "LoanId.cs");
        WriteCode(file, "Library.Lending.Contracts", "LoanId");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("File 'apps/api/src/Library.Modules/Lending/Contracts/LoanId.cs' sits directly in Contracts/, which holds only its kind folders.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>The Contracts project file in a module Contracts folder reports nothing, while a type beside it still reports the kind folders.</summary>
    /// <proves>litespec/backend/dotnet/architecture#compile-each-contracts-folder-as-its-own-project</proves>
    [Fact]
    public void ContractsProjectFileReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var project = ProductFile(temp.Path, "Lending", "Contracts", "Library.Lending.Contracts.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(project)!);
        File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        var file = ProductFile(temp.Path, "Lending", "Contracts", "LoanId.cs");
        WriteCode(file, "Library.Lending.Contracts", "LoanId");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("File 'apps/api/src/Library.Modules/Lending/Contracts/LoanId.cs' sits directly in Contracts/, which holds only its kind folders.", finding.Message);
    }

    /// <summary>A nested model and a row in a use case WebApi folder report nothing, a handler there still reports, and a row in Application stays a result part.</summary>
    /// <proves>litespec/backend/application#use-this-use-case-layout</proves>
    [Fact]
    public void WebApiNestedModelAndRowReportNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var nested = ProductFile(temp.Path, "Lending", "WebApi", "BorrowCopy", "BorrowCopyLineModel.cs");
        WriteCode(nested, "Library.Lending.WebApi.BorrowCopy", "BorrowCopyLineModel");
        var row = ProductFile(temp.Path, "Lending", "WebApi", "BorrowCopy", "BorrowCopyRow.cs");
        WriteCode(row, "Library.Lending.WebApi.BorrowCopy", "BorrowCopyRow");
        var handler = ProductFile(temp.Path, "Lending", "WebApi", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(handler, "Library.Lending.WebApi.BorrowCopy", "BorrowCopyCommandHandler");
        var resultRow = ProductFile(temp.Path, "Lending", "Application", "BorrowCopy", "BorrowCopyResultRow.cs");
        WriteCode(resultRow, "Library.Lending.Application.BorrowCopy", "BorrowCopyResultRow");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("File 'apps/api/src/Library.Modules/Lending/WebApi/BorrowCopy/BorrowCopyCommandHandler.cs' is not a transport type, which sits in Application/, not WebApi/.", finding.Message);
    }

    /// <summary>A folder in the Kernel reports that the Kernel holds files only.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void KernelFolderReportsFilesOnly()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var shared = Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Shared");
        Directory.CreateDirectory(shared);

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.Kernel/Shared' sits in the Kernel, which holds files only.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A Platform capability folder reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformCapabilityFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "Access"));

        Assert.Empty(Run(model));
    }

    /// <summary>A Platform folder outside its closed set of capability folders reports the Platform folders.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PlatformFolderOutsideClosedSetReportsPlatformFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var shared = Path.Combine(temp.Path, "apps", "api", "src", "Library.Platform", "Shared");
        Directory.CreateDirectory(shared);

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.Platform/Shared' names no Platform folder.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Runs the placement check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The code placement findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        BackendPlacementChecks.Run(model, findings);
        TestPlacementChecks.Run(model, findings);
        FrontendPlacementChecks.Run(model, findings);
        ClosedFolderChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "code-placement-drift").ToList();
    }

    /// <summary>Runs the mark checks and keeps the folder drift findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The mark folder drift findings.</returns>
    static List<Finding> FolderDrift(ProjectModel model)
    {
        var findings = new List<Finding>();
        MarkChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "mark-folder-drift").ToList();
    }

    /// <summary>A model with the modules root, the Kernel and Platform roots, the host, the test root and the records the drift cases mark.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel DriftModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"paths\": {\"testRoots\": [\"apps/api/tests\"]}, \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"kernel\": \"apps/api/src/Library.Kernel\", \"platform\": \"apps/api/src/Library.Platform\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec(RecordTypes.Module, "module/controls", "controls", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/controls.flag", "controls/flags", root));
        model.Records.Add(Rec(RecordTypes.UseCase, "use-case/controls.read-feature-flag", "controls/use-case/read-feature-flag", root));
        model.Records.Add(Rec(RecordTypes.Scenario, "scenario/read-feature-flag.reading-a-flag-succeeds", "controls/use-case/read-feature-flag/scenario/reading-a-flag-succeeds", root));
        model.Records.Add(Rec(RecordTypes.Module, "module/cancellations", "cancellations", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/cancellations.event-change-answers", "cancellations/event-change-answers", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/cancellations.event-changes", "cancellations/event-changes", root));
        model.Records.Add(Rec(RecordTypes.UseCase, "use-case/cancellations.record-event-change-answer", "cancellations/event-change-answers/use-case/record-event-change-answer", root));
        model.Records.Add(Rec(RecordTypes.Module, "module/lending", "lending", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/lending.loan", "lending/loans", root));
        model.Records.Add(Rec(RecordTypes.UseCase, "use-case/lending.borrow-copy", "lending/loans/use-case/borrow-copy", root));
        model.Records.Add(Rec(RecordTypes.Scenario, "scenario/borrow-copy.borrowing-an-available-copy-succeeds", "lending/loans/use-case/borrow-copy/scenario/borrowing-an-available-copy-succeeds", root));
        model.Records.Add(Rec(RecordTypes.Rule, "rule/loan.request-names-an-existing-loan", "lending/loans/rule/request-names-an-existing-loan", root,
            """{"status": "reviewed", "governs": ["aggregate/lending.loan"]}"""));
        model.Records.Add(Rec(RecordTypes.DomainEvent, "domain-event/loan.loan-borrowed", "lending/loans/domain-event/loan-borrowed", root));
        model.Records.Add(Rec(RecordTypes.Reaction, "reaction/lending.send-due-notice", "lending/reaction/send-due-notice", root,
            """{"status": "reviewed", "triggeredBy": ["domain-event/loan.loan-borrowed"]}"""));
        model.Records.Add(Rec(RecordTypes.Module, "module/billing", "billing", root));
        model.Records.Add(Rec(RecordTypes.Module, "module/admission", "admission", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/admission.admission-gate", "admission/admission-gates", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/admission.scanner-device", "admission/scanner-devices", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/admission.box", "admission/boxes", root));
        model.Records.Add(Rec(RecordTypes.Rule, "rule/scanner-device.device-stands-at-own-event-gate", "admission/scanner-devices/rule/device-stands-at-own-event-gate", root,
            """{"status": "reviewed", "governs": ["aggregate/admission.admission-gate"]}"""));
        model.Records.Add(Rec(RecordTypes.Module, "module/events", "events", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/events.coming-soon-page", "events/coming-soon-pages", root));
        model.Records.Add(Rec(RecordTypes.DomainEvent, "domain-event/coming-soon-page.published", "events/coming-soon-pages/domain-event/published", root));
        model.Records.Add(Rec(RecordTypes.Module, "module/directory", "directory", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/directory.directory", "directory/directory", root));
        model.Records.Add(Rec(RecordTypes.UseCase, "use-case/directory.search-accounts", "directory/directory/use-case/search-accounts", root));

        return model;
    }

    /// <summary>One backend element carrying one implements mark.</summary>
    /// <param name="kind">The code element kind.</param>
    /// <param name="class">The class name carrying the mark.</param>
    /// <param name="file">The file the element sits in.</param>
    /// <param name="recordId">The record id the mark names.</param>
    /// <returns>The marked code element.</returns>
    static MarkedElement Element(CodeKind kind, string @class, string file, string recordId) =>
        new(kind, @class, null, file, 3, [new CodeMark(recordId, MarkStyle.Xml, MarkVerb.Implements, file, 1)], InTests: false);

    /// <summary>One test proving the scenario.</summary>
    /// <param name="class">The test class name.</param>
    /// <param name="method">The test method name.</param>
    /// <param name="file">The file the test sits in.</param>
    /// <param name="scenarioId">The scenario id the proves mark names.</param>
    /// <returns>The marked test element.</returns>
    static MarkedElement TestElement(string @class, string method, string file, string scenarioId) =>
        new(CodeKind.Test, @class, method, file, 3, [new CodeMark(scenarioId, MarkStyle.Xml, MarkVerb.Proves, file, 1)], InTests: true);

    /// <summary>One file under the modules root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="segments">The path segments under the modules root.</param>
    /// <returns>The full path to the file.</returns>
    static string ProductFile(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library.Modules" };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }

    /// <summary>One file under the acceptance test project.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="segments">The path segments under the test project.</param>
    /// <returns>The full path to the file.</returns>
    static string TestFile(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "tests", "Library.Acceptance.Tests" };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }

    /// <summary>Writes one C# file declaring the namespace and the class.</summary>
    /// <param name="file">The full path of the file to write.</param>
    /// <param name="namespace">The declared namespace.</param>
    /// <param name="class">The declared class name.</param>
    static void WriteCode(string file, string @namespace, string @class)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "namespace " + @namespace + ";\n\npublic sealed class " + @class + "\n{\n}\n");
    }

    /// <summary>One record with its placement path.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="qualified">The placement path: module, aggregate and name joined by dots.</param>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <param name="raw">The raw record JSON.</param>
    /// <returns>The record.</returns>
    static LiteSpec.Cli.Model.Records.Record Rec(string type, string id, string qualified, string root, string raw = """{"status": "reviewed"}""")
    {
        using var document = JsonDocument.Parse(raw);

        return new LiteSpec.Cli.Model.Records.Record
        {
            Type = type,
            Id = id,
            Folder = FolderOf(type, qualified),
            Module = ModuleOf(type, qualified),
            Title = id,
            File = Path.Combine(root, ".litespec", "record.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }

    /// <summary>The module a record with the placement path sits under: its first segment, when the path names one.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="qualified">The placement path: module, aggregate and name joined by dots.</param>
    /// <returns>The module name, or null when the identifier names no module.</returns>
    static string? ModuleOf(string type, string qualified)
    {
        var segments = qualified.Split('/');

        return segments.Length >= 2 && segments[0] != type ? segments[0] : null;
    }

    /// <summary>The folder a record with the placement path sits in, under the behavior modules folder.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="qualified">The placement path: module, aggregate and name joined by dots.</param>
    /// <returns>The folder path, or empty when the identifier names no module.</returns>
    static string FolderOf(string type, string qualified)
    {
        var module = ModuleOf(type, qualified);

        if (module is null)
        {
            return "";
        }

        var segments = qualified.Split('/');

        return segments.Length >= 2 && segments[1] != type && segments[1] != RecordTypes.UseCase
            ? $".litespec/behavior/modules/{module}/{segments[1]}"
            : $".litespec/behavior/modules/{module}";
    }
}
