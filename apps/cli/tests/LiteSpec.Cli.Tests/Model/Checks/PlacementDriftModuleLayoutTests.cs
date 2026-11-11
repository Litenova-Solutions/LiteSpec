using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the code placement check matches the module layout: module-level use cases, the element's own folder, Contracts publishing, reactions, enforcing aggregates, and mirrored tests.</summary>
public sealed class PlacementDriftModuleLayoutTests
{
    /// <summary>A module-level use case endpoint in UseCases reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLevelEndpointInUseCasesReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Controls", "UseCases", "ReadFeatureFlag", "ReadFeatureFlagEndpoint.cs");
        WriteCode(file, "Library.Controls.UseCases.ReadFeatureFlag", "ReadFeatureFlagEndpoint");
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
        var file = ProductFile(temp.Path, "Controls", "Flags", "UseCases", "ReadFeatureFlag", "ReadFeatureFlagQueryHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "ReadFeatureFlagQueryHandler", file, "use-case/controls.read-feature-flag"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'ReadFeatureFlagQueryHandler' sits in folder 'Flags', but marks 'use-case/controls.read-feature-flag'.", finding.Message);
        Assert.Equal("use-case/controls.read-feature-flag", finding.Subject);
    }

    /// <summary>An endpoint in its aggregate use case folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedEndpointInOwnFoldersReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Cancellations", "EventChangeAnswers", "UseCases", "RecordEventChangeAnswer", "RecordEventChangeAnswerEndpoint.cs");
        WriteCode(file, "Library.Cancellations.EventChangeAnswers.UseCases.RecordEventChangeAnswer", "RecordEventChangeAnswerEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "RecordEventChangeAnswerEndpoint", file, "use-case/cancellations.record-event-change-answer"));

        Assert.Empty(Run(model));
    }

    /// <summary>An endpoint in another aggregate folder reports that folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedEndpointInWrongAggregateReportsFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Cancellations", "EventChanges", "UseCases", "RecordEventChangeAnswer", "RecordEventChangeAnswerEndpoint.cs");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "RecordEventChangeAnswerEndpoint", file, "use-case/cancellations.record-event-change-answer"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Endpoint 'RecordEventChangeAnswerEndpoint' sits in folder 'EventChanges', but marks 'use-case/cancellations.record-event-change-answer'.", finding.Message);
        Assert.Equal("use-case/cancellations.record-event-change-answer", finding.Subject);
    }

    /// <summary>An aggregate-owned handler in the module UseCases folder reports UseCases.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedHandlerInModuleUseCasesReportsUseCasesFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Cancellations", "UseCases", "RecordEventChangeAnswer", "RecordEventChangeAnswerCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "RecordEventChangeAnswerCommandHandler", file, "use-case/cancellations.record-event-change-answer"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'RecordEventChangeAnswerCommandHandler' sits in folder 'UseCases', but marks 'use-case/cancellations.record-event-change-answer'.", finding.Message);
        Assert.Equal("use-case/cancellations.record-event-change-answer", finding.Subject);
    }

    /// <summary>A domain event of a module named like a layer folder reports nothing in its aggregate Domain folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void DomainEventOfModuleNamedLikeLayerFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Events", "ComingSoonPages", "Domain", "Events", "ComingSoonPagePublishedEvent.cs");
        WriteCode(file, "Library.Events.ComingSoonPages.Domain.Events", "ComingSoonPagePublishedEvent");
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "ComingSoonPagePublishedEvent", file, "event/coming-soon-page.published"));

        Assert.Empty(Run(model));
    }

    /// <summary>An endpoint under an aggregate named like its module reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void EndpointUnderAggregateNamedLikeModuleReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Directory", "Directory", "UseCases", "SearchAccounts", "SearchAccountsEndpoint.cs");
        WriteCode(file, "Library.Directory.Directory.UseCases.SearchAccounts", "SearchAccountsEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "SearchAccountsEndpoint", file, "use-case/directory.search-accounts"));

        Assert.Empty(Run(model));
    }

    /// <summary>A rule exception in its module Contracts folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void RuleExceptionInOwnModuleContractsReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Contracts", "Exceptions", "LoanNotFoundException.cs");
        WriteCode(file, "Library.Lending.Contracts.Exceptions", "LoanNotFoundException");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "LoanNotFoundException", file, "rule/loan.request-names-an-existing-loan"));

        Assert.Empty(Run(model));
    }

    /// <summary>A product rule exception in the shared kernel reports nothing, wherever its record file sits.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ProductRuleExceptionInSharedKernelReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        AddProductRule(model, temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.SharedKernel", "Domain", "Exceptions", "TextRequiredException.cs");
        WriteCode(file, "Library.Domain.Exceptions", "TextRequiredException");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "TextRequiredException", file, "rule/library.required-text-is-never-blank"));

        Assert.Empty(Run(model));
    }

    /// <summary>A product rule exception in a module reports that it belongs in the shared kernel.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ProductRuleExceptionInAModuleReportsTheSharedKernel()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        AddProductRule(model, temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Contracts", "Exceptions", "TextRequiredException.cs");
        WriteCode(file, "Library.Lending.Contracts.Exceptions", "TextRequiredException");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "TextRequiredException", file, "rule/library.required-text-is-never-blank"));

        var finding = Assert.Single(Run(model));
        Assert.Equal("Rule exception 'TextRequiredException' implements the product rule 'rule/library.required-text-is-never-blank', so it belongs in the shared kernel.", finding.Message);
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
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "LoanBorrowedEvent", file, "event/loan.loan-borrowed"));

        Assert.Empty(Run(model));
    }

    /// <summary>A rule exception beside an aggregate its rule governs reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void RuleExceptionBesideGovernedAggregateReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Admission", "AdmissionGates", "Domain", "Exceptions", "AdmissionGateEventMismatchException.cs");
        WriteCode(file, "Library.Admission.AdmissionGates.Domain.Exceptions", "AdmissionGateEventMismatchException");
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
        var file = ProductFile(temp.Path, "Admission", "Boxes", "Domain", "Exceptions", "AdmissionGateEventMismatchException.cs");
        model.Code.Elements.Add(Element(CodeKind.RuleException, "AdmissionGateEventMismatchException", file, "rule/scanner-device.device-stands-at-own-event-gate"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Rule exception 'AdmissionGateEventMismatchException' sits in folder 'Boxes', but marks 'rule/scanner-device.device-stands-at-own-event-gate'.", finding.Message);
        Assert.Equal("rule/scanner-device.device-stands-at-own-event-gate", finding.Subject);
    }

    /// <summary>A test under UseCases for a module-level use case reports nothing.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void TestForModuleLevelUseCaseUnderUseCasesReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = TestFile(temp.Path, "Controls", "UseCases", "ReadFeatureFlag", "ReadFeatureFlagScenarios.cs");
        WriteCode(file, "Library.Acceptance.Tests.Controls.UseCases.ReadFeatureFlag", "ReadFeatureFlagScenarios");
        model.Code.Elements.Add(TestElement("ReadFeatureFlagScenarios", "Reading_a_flag_succeeds", file, "scenario/read-feature-flag.reading-a-flag-succeeds"));

        Assert.Empty(Run(model));
    }

    /// <summary>A test under UseCases for an aggregate-owned use case reports UseCases.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void TestForAggregateOwnedUseCaseUnderUseCasesReportsFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = TestFile(temp.Path, "Lending", "UseCases", "BorrowCopy", "BorrowCopyScenarios.cs");
        model.Code.Elements.Add(TestElement("BorrowCopyScenarios", "Borrowing_an_available_copy_succeeds", file, "scenario/borrow-copy.borrowing-an-available-copy-succeeds"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Test 'BorrowCopyScenarios.Borrowing_an_available_copy_succeeds' sits in folder 'UseCases', but marks 'scenario/borrow-copy.borrowing-an-available-copy-succeeds'.", finding.Message);
        Assert.Equal("scenario/borrow-copy.borrowing-an-available-copy-succeeds", finding.Subject);
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
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Controls/ReadFeatureFlag' names no layer folder or declared aggregate.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>An aggregate-owned handler under its aggregate UseCases folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedHandlerUnderUseCasesReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Lending.Loans.UseCases.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        Assert.Empty(Run(model));
        Assert.Empty(FolderDrift(model));
    }

    /// <summary>An aggregate-owned endpoint flat in its aggregate folder reports the missing UseCases level.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedEndpointFlatInAggregateFolderReportsUseCasesFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Loans", "BorrowCopy", "BorrowCopyEndpoint.cs");
        WriteCode(file, "Library.Lending.Loans.BorrowCopy", "BorrowCopyEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "BorrowCopyEndpoint", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Lending/Loans/BorrowCopy' names no layer folder or UseCases folder.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A reaction under its module UseCases On fact folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ReactionUnderUseCasesOnFactReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "UseCases", "OnLoanBorrowed", "SendDueNoticeOnLoanBorrowedHandler.cs");
        WriteCode(file, "Library.Lending.UseCases.OnLoanBorrowed", "SendDueNoticeOnLoanBorrowedHandler");
        model.Code.Elements.Add(Element(CodeKind.EventHandler, "SendDueNoticeOnLoanBorrowedHandler", file, "reaction/lending.send-due-notice"));

        Assert.Empty(Run(model));
        Assert.Empty(FolderDrift(model));
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

    /// <summary>A shared kernel folder outside its closed set of layer folders reports the layer folders.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void SharedKernelFolderOutsideClosedSetReportsLayerFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = DriftModel(temp.Path);
        var shared = Path.Combine(temp.Path, "apps", "api", "src", "Library.SharedKernel", "Shared");
        Directory.CreateDirectory(shared);

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.SharedKernel/Shared' names no shared kernel layer folder.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Runs the placement check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The code placement findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        PlacementChecks.Run(model, findings);

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

    /// <summary>A model with the modules root, the shared kernel root, the host, the test root and the records the drift cases mark.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel DriftModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"paths\": {\"testRoots\": [\"apps/api/tests\"]}, \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"sharedKernel\": \"apps/api/src/Library.SharedKernel\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
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
        model.Records.Add(Rec(RecordTypes.Event, "event/loan.loan-borrowed", "lending/loans/event/loan-borrowed", root));
        model.Records.Add(Rec(RecordTypes.Reaction, "reaction/lending.send-due-notice", "lending/reaction/send-due-notice", root,
            """{"status": "reviewed", "triggeredBy": ["event/loan.loan-borrowed"]}"""));
        model.Records.Add(Rec(RecordTypes.Module, "module/billing", "billing", root));
        model.Records.Add(Rec(RecordTypes.Module, "module/admission", "admission", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/admission.admission-gate", "admission/admission-gates", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/admission.scanner-device", "admission/scanner-devices", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/admission.box", "admission/boxes", root));
        model.Records.Add(Rec(RecordTypes.Rule, "rule/scanner-device.device-stands-at-own-event-gate", "admission/scanner-devices/rule/device-stands-at-own-event-gate", root,
            """{"status": "reviewed", "governs": ["aggregate/admission.admission-gate"]}"""));
        model.Records.Add(Rec(RecordTypes.Module, "module/events", "events", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/events.coming-soon-page", "events/coming-soon-pages", root));
        model.Records.Add(Rec(RecordTypes.Event, "event/coming-soon-page.published", "events/coming-soon-pages/event/published", root));
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
