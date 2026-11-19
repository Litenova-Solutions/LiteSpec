using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the code placement check against the broken overlay and module-layout models.</summary>
public sealed class PlacementCheckTests
{
    /// <summary>The overlay reports one placement finding per drift: the handler folder, the namespace, the test, the stray module folder, the stray use case folder and the stray test folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void BrokenOverlayReportsEachPlacementDriftOnce()
    {
        using var temp = ModelFixture.Prepare("small", "code-placement-drift");
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var findings = new List<Finding>();
        BackendPlacementChecks.Run(model, findings);
        TestPlacementChecks.Run(model, findings);
        FrontendPlacementChecks.Run(model, findings);
        ClosedFolderChecks.Run(model, findings);
        var placed = findings.Where(finding => finding.Code == "code-placement-drift").ToList();

        Assert.Equal(6, placed.Count);
        Assert.Single(placed, finding => finding.Message == "Handler 'BorrowCopyCommandHandler' sits in folder 'Holds', but marks 'use-case/lending.borrow-copy', so it belongs in 'Lending/Application/Loans/BorrowCopy/'.");
        Assert.Single(placed, finding => finding.Message == "Handler 'BorrowCopyRetryHandler' declares namespace 'Library.Billing.BorrowCopy', but 'use-case/lending.borrow-copy' places it under 'Library.Lending.Loans.BorrowCopy'.");
        Assert.Single(placed, finding => finding.Message == "Test 'BorrowCopyScenarios.Borrowing_an_available_copy_succeeds' sits in folder 'Holds', but marks 'scenario/borrow-copy.borrowing-an-available-copy-succeeds', so it belongs in 'Lending/Loans/BorrowCopy/'.");
        Assert.Single(placed, finding => finding.Message == "Folder 'apps/api/src/Modules/Library.Lending/Archive' names no module folder.");
        Assert.Single(placed, finding => finding.Message == "Folder 'apps/api/src/Modules/Library.Lending/Application/Holds/BorrowCopy' names no use case of aggregate 'Holds'.");
        Assert.Single(placed, finding => finding.Message == "Folder 'apps/api/tests/Library.Acceptance.Tests/Lending/Holds/BorrowCopy' names no use case of aggregate 'Holds'.");
        Assert.All(placed, finding => Assert.EndsWith(".", finding.Message));
    }

    /// <summary>A placed handler in the module layout reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutPlacedHandlerReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        Assert.Empty(Run(model));
    }

    /// <summary>A handler in the wrong aggregate folder reports the folder once, without a namespace finding.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHandlerInWrongAggregateReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Holds", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'BorrowCopyCommandHandler' sits in folder 'Holds', but marks 'use-case/lending.borrow-copy', so it belongs in 'Lending/Application/Loans/BorrowCopy/'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A handler in the right folder with the wrong namespace reports the namespace.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHandlerWithWrongNamespaceReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Billing.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'BorrowCopyCommandHandler' declares namespace 'Library.Billing.BorrowCopy', but 'use-case/lending.borrow-copy' places it under 'Library.Lending.Loans.BorrowCopy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A handler in the right folder with the right namespace reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHandlerWithRightNamespaceReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Lending.Application.Loans.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        Assert.Empty(Run(model));
    }

    /// <summary>A domain event outside its module reports the module folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutEventOutsideItsModuleReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Billing", "Domain", "Invoices", "Events", "LoanBorrowedEvent.cs");
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "LoanBorrowedEvent", file, "domain-event/loan.loan-borrowed"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Domain event 'LoanBorrowedEvent' sits in module folder 'Billing', but marks 'domain-event/loan.loan-borrowed'.", finding.Message);
        Assert.Equal("domain-event/loan.loan-borrowed", finding.Subject);
    }

    /// <summary>A domain event in its aggregate reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutEventInItsAggregateReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "Events", "LoanBorrowedEvent.cs");
        WriteCode(file, "Library.Lending.Domain.Loans.Events", "LoanBorrowedEvent");
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "LoanBorrowedEvent", file, "domain-event/loan.loan-borrowed"));

        Assert.Empty(Run(model));
    }

    /// <summary>A test under its scenario's use case path reports nothing.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void ModuleLayoutPlacedTestReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = TestFile(temp.Path, "Lending", "Loans", "BorrowCopy", "BorrowCopyScenarios.cs");
        WriteCode(file, "Library.Acceptance.Tests.Lending.Loans.BorrowCopy", "BorrowCopyScenarios");
        model.Code.Elements.Add(TestElement("BorrowCopyScenarios", "Borrowing_an_available_copy_succeeds", file));

        Assert.Empty(Run(model));
    }

    /// <summary>A test outside its scenario's module reports the module folder.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void ModuleLayoutTestOutsideItsModuleReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = TestFile(temp.Path, "Billing", "BorrowCopy", "BorrowCopyScenarios.cs");
        model.Code.Elements.Add(TestElement("BorrowCopyScenarios", "Borrowing_an_available_copy_succeeds", file));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Test 'BorrowCopyScenarios.Borrowing_an_available_copy_succeeds' sits in module folder 'Billing', but marks 'scenario/borrow-copy.borrowing-an-available-copy-succeeds'.", finding.Message);
        Assert.Equal("scenario/borrow-copy.borrowing-an-available-copy-succeeds", finding.Subject);
    }

    /// <summary>An adapter test under its adapter's Infrastructure path is exempt by kind.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void ModuleLayoutAdapterTestIsExempt()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = TestFile(temp.Path, "Billing", "Infrastructure", "TextGatewayTests.cs");
        model.Code.Elements.Add(TestElement("TextGatewayTests", "Sending_succeeds", file));

        Assert.Empty(Run(model));
    }

    /// <summary>A host endpoint carries its mark without a use case folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHostEndpointIsAccepted()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.WebApi", "Protocol", "SignOutEndpoint.cs");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "SignOutEndpoint", file, "use-case/lending.borrow-copy"));

        Assert.Empty(Run(model));
    }

    /// <summary>An endpoint in the second declared WebApi host carries its mark without a use case folder.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void ModuleLayoutSecondWebApiHostEndpointIsAccepted()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path, webapi: "[\"apps/api/src/Library.WebApi\", \"apps/api/src/Library.Search.WebApi\"]");
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.Search.WebApi", "Protocol", "SignOutEndpoint.cs");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "SignOutEndpoint", file, "use-case/lending.borrow-copy"));

        Assert.Equal(2, model.Config.WebApiRoots.Count);
        Assert.Empty(Run(model));
    }

    /// <summary>A frontend module folder that names no declared module reports the module folder.</summary>
    /// <proves>litespec/frontend/structure#mirror-the-backend-use-case-path-in-kebab-case</proves>
    [Fact]
    public void ModuleLayoutFrontendModuleNamingNoDeclaredModuleReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path, withFrontend: true);
        var folder = Path.Combine(temp.Path, "apps", "web", "src", "lib", "modules", "archive", "holds", "hold-room");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "litespec.ts");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, "archive/holds/hold-room", null, file, 3,
            [new CodeMark("use-case/lending.borrow-copy", MarkStyle.Typed, MarkVerb.Implements, file, 1)],
            InTests: false));

        var placed = Run(model);

        Assert.Equal(2, placed.Count);
        Assert.Single(placed, finding => finding.Message == "Frontend module 'archive/holds/hold-room' sits in module folder 'archive', but no declared module names it.");
        Assert.Single(placed, finding => finding.Message == "Folder 'apps/web/src/lib/modules/archive' names no declared module.");
    }

    /// <summary>A frontend module folder that names a declared module reports nothing.</summary>
    /// <proves>litespec/frontend/structure#mirror-the-backend-use-case-path-in-kebab-case</proves>
    [Fact]
    public void ModuleLayoutFrontendModuleNamingADeclaredModuleReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path, withFrontend: true);
        var folder = Path.Combine(temp.Path, "apps", "web", "src", "lib", "modules", "lending", "loans", "borrow-copy");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "litespec.ts");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, "lending/borrow-copy", null, file, 3,
            [new CodeMark("use-case/lending.borrow-copy", MarkStyle.Typed, MarkVerb.Implements, file, 1)],
            InTests: false));

        Assert.Empty(Run(model));
    }

    /// <summary>A product folder that names no declared module reports the folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutStrayProductFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Archive"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Archive' names no declared module.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A product tree that mirrors its declared records reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutDeclaredTreeReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Domain", "Loans"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Application", "Loans", "BorrowCopy"));

        Assert.Empty(Run(model));
    }

    /// <summary>A handler in its use case's Application folder, with the namespace that folder names, reports nothing.</summary>
    /// <proves>litespec/backend/application#use-this-use-case-layout</proves>
    [Fact]
    public void ModuleLayoutHandlerInApplicationFolderWithRightNamespaceReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Lending.Application.Loans.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        Assert.Empty(Run(model));
    }

    /// <summary>An endpoint in a use case Application folder reports that a transport type belongs in WebApi.</summary>
    /// <proves>litespec/backend/application#use-this-use-case-layout</proves>
    [Fact]
    public void ModuleLayoutEndpointInApplicationFolderReportsTransportDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyEndpoint.cs");
        WriteCode(file, "Library.Lending.Application.Loans.BorrowCopy", "BorrowCopyEndpoint");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "BorrowCopyEndpoint", file, "use-case/lending.borrow-copy"));

        var finding = Assert.Single(Run(model));
        Assert.Contains("BorrowCopyEndpoint.cs' is a transport type, which sits in WebApi/, not Application/.", finding.Message);
    }

    /// <summary>A reaction that only sends a notification, in the Application Notifications folder of that notification, reports nothing.</summary>
    /// <proves>litespec/workspace/naming#place-each-trigger-with-what-it-runs</proves>
    [Fact]
    public void ModuleLayoutSendOnlyReactionInNotificationFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(WithEffects(Rec(RecordTypes.Reaction, "reaction/lending.notify-reader", "lending/reaction/notify-reader", temp.Path), """[{"sends": "notification/lending.loan-due"}]"""));
        var file = ProductFile(temp.Path, "Lending", "Application", "Notifications", "LoanDue", "NotifyReaderOnLoanBorrowedHandler.cs");
        WriteCode(file, "Library.Lending.Application.Notifications.LoanDue", "NotifyReaderOnLoanBorrowedHandler");
        model.Code.Elements.Add(Element(CodeKind.EventHandler, "NotifyReaderOnLoanBorrowedHandler", file, "reaction/lending.notify-reader"));

        Assert.Empty(Run(model));
    }

    /// <summary>A reaction that runs a use case, in an Application Notifications folder, reports the use case folder it belongs in.</summary>
    /// <proves>litespec/workspace/naming#place-each-trigger-with-what-it-runs</proves>
    [Fact]
    public void ModuleLayoutRunningReactionInNotificationFolderReportsTriggerDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(WithEffects(Rec(RecordTypes.Reaction, "reaction/lending.notify-reader", "lending/reaction/notify-reader", temp.Path), """[{"runs": "use-case/lending.borrow-copy"}]"""));
        var file = ProductFile(temp.Path, "Lending", "Application", "Notifications", "LoanDue", "NotifyReaderOnLoanBorrowedHandler.cs");
        WriteCode(file, "Library.Lending.Application.Notifications.LoanDue", "NotifyReaderOnLoanBorrowedHandler");
        model.Code.Elements.Add(Element(CodeKind.EventHandler, "NotifyReaderOnLoanBorrowedHandler", file, "reaction/lending.notify-reader"));

        var placed = Run(model);

        // The Notifications folder also names no notification, since the reaction now runs a use case.
        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Event handler 'NotifyReaderOnLoanBorrowedHandler' sits in folder 'LoanDue', but its trigger 'reaction/lending.notify-reader' belongs in 'Lending/Application/Loans/BorrowCopy'.");
    }

    /// <summary>A job that runs a use case, in that use case's Application folder, reports nothing.</summary>
    /// <proves>litespec/backend/application#use-this-use-case-layout</proves>
    [Fact]
    public void ModuleLayoutJobInItsUseCaseApplicationFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(WithEffects(Rec(RecordTypes.Job, "job/lending.erase-expired", "lending/job/erase-expired", temp.Path), """[{"runs": "use-case/lending.borrow-copy"}]"""));
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "EraseExpiredLoansJob.cs");
        WriteCode(file, "Library.Lending.Application.Loans.BorrowCopy", "EraseExpiredLoansJob");
        model.Code.Elements.Add(Element(CodeKind.Job, "EraseExpiredLoansJob", file, "job/lending.erase-expired"));

        Assert.Empty(Run(model));
    }

    /// <summary>A job in a module Jobs folder reports the use case Application folder it belongs in, and the Jobs folder that names no module folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void ModuleLayoutJobInJobsFolderReportsTriggerDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(WithEffects(Rec(RecordTypes.Job, "job/lending.erase-expired", "lending/job/erase-expired", temp.Path), """[{"runs": "use-case/lending.borrow-copy"}]"""));
        var file = ProductFile(temp.Path, "Lending", "Jobs", "EraseExpiredLoans", "EraseExpiredLoansJob.cs");
        WriteCode(file, "Library.Lending.Jobs.EraseExpiredLoans", "EraseExpiredLoansJob");
        model.Code.Elements.Add(Element(CodeKind.Job, "EraseExpiredLoansJob", file, "job/lending.erase-expired"));

        var placed = Run(model);

        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Job 'EraseExpiredLoansJob' sits in folder 'EraseExpiredLoans', but its trigger 'job/lending.erase-expired' belongs in 'Lending/Application/Loans/BorrowCopy'.");
        Assert.Contains(placed, finding => finding.Message.EndsWith("Lending/Jobs' names no module folder.", StringComparison.Ordinal));
    }

    /// <summary>A Contracts Commands folder reports that it names no aggregate, and its command file reports too.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    /// <proves>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void ModuleLayoutContractsCommandsFolderReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Contracts", "Commands", "BorrowCopyCommand.cs");
        WriteCode(file, "Library.Lending.Contracts.Commands", "BorrowCopyCommand");

        var placed = Run(model);

        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Folder 'apps/api/src/Library.Modules/Lending/Contracts/Commands' names no declared aggregate or public module-level use case of module 'Lending'.");
        Assert.Contains(placed, finding => finding.Message.EndsWith("holds a command of no public use case; only public use cases sit in Contracts.", StringComparison.Ordinal));
    }

    /// <summary>An aggregate WebApi folder reports that it names no Events or Exceptions folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void ModuleLayoutAggregateWebApiFolderReportsLayerDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "WebApi", "LoanResponseModel.cs");
        WriteCode(file, "Library.Lending.Domain.Loans.WebApi", "LoanResponseModel");

        var placed = Run(model);

        // The response model is also a transport model outside its use case folder.
        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Folder 'apps/api/src/Library.Modules/Lending/Domain/Loans/WebApi' names no Events or Exceptions folder.");
        Assert.Contains(placed, finding => finding.Message == "File 'apps/api/src/Library.Modules/Lending/Domain/Loans/WebApi/LoanResponseModel.cs' holds a message, result or transport model outside a use case folder.");
    }

    /// <summary>A read-model projection in its Application read-models folder reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder</proves>
    [Fact]
    public void ModuleLayoutReadModelProjectionInReadModelsFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec("read-model", "read-model/lending.reimbursed-expense", "lending/read-model/reimbursed-expense", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Application", "ReadModels", "ReimbursedExpenses", "ReimbursedExpenseOnLoanBorrowedProjection.cs");
        WriteCode(file, "Library.Lending.Application.ReadModels.ReimbursedExpenses", "ReimbursedExpenseOnLoanBorrowedProjection");
        model.Code.Elements.Add(Element(CodeKind.Technical, "ReimbursedExpenseOnLoanBorrowedProjection", file, "read-model/lending.reimbursed-expense"));

        Assert.Empty(Run(model));
    }

    /// <summary>A read-model projection outside its read model's ReadModels folder reports the folder the read model takes.</summary>
    /// <proves>litespec/backend/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder</proves>
    [Fact]
    public void ModuleLayoutReadModelProjectionOutsideReadModelsFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec("read-model", "read-model/lending.reimbursed-expense", "lending/read-model/reimbursed-expense", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "Projections", "ReimbursedExpenseOnLoanBorrowedProjection.cs");
        model.Code.Elements.Add(Element(CodeKind.Technical, "ReimbursedExpenseOnLoanBorrowedProjection", file, "read-model/lending.reimbursed-expense"));

        var placed = Run(model);

        Assert.Contains(placed, finding => finding.Message == "Technical class 'ReimbursedExpenseOnLoanBorrowedProjection' sits in folder 'Projections', but marks 'read-model/lending.reimbursed-expense', so it belongs in 'Lending/Application/ReadModels/ReimbursedExpenses/'.");
    }

    /// <summary>A command in its use case's Application folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#give-each-use-case-its-own-models</proves>
    [Fact]
    public void ModuleLayoutUseCaseCommandInApplicationFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommand.cs");
        WriteCode(file, "Library.Lending.Application.Loans.BorrowCopy", "BorrowCopyCommand");

        Assert.Empty(Run(model));
    }

    /// <summary>A command named for a declared use case, outside any use case folder, reports the folder it belongs in.</summary>
    /// <proves>litespec/backend/architecture#give-each-use-case-its-own-models</proves>
    [Fact]
    public void ModuleLayoutUseCaseCommandOutsideUseCaseFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "BorrowCopyCommand.cs");
        WriteCode(file, "Library.Lending.Domain.Loans", "BorrowCopyCommand");

        var finding = Assert.Single(Run(model));
        Assert.Equal("File 'apps/api/src/Library.Modules/Lending/Domain/Loans/BorrowCopyCommand.cs' holds a message, result or transport model outside a use case folder; it belongs in 'Lending/Application/Loans/BorrowCopy'.", finding.Message);
    }

    /// <summary>A handler in its plural aggregate folder reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedHandlerInPluralAggregateFolderReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Lending.Application.Loans.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        Assert.Empty(Run(model));
    }

    /// <summary>The same use case flat under its layer reports that it belongs in its aggregate folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateOwnedHandlerFlatUnderItsLayerReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("Lending/Application/Loans/BorrowCopy/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A module-level use case flat under its layer reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLevelHandlerFlatUnderItsLayerReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.UseCase, "use-case/lending.read-health", "lending/use-case/read-health", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Application", "ReadHealth", "ReadHealthQueryHandler.cs");
        WriteCode(file, "Library.Lending.Application.ReadHealth", "ReadHealthQueryHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "ReadHealthQueryHandler", file, "use-case/lending.read-health"));

        Assert.Empty(Run(model));
    }

    /// <summary>A singular Domain aggregate folder reports that it names no declared aggregate.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SingularDomainAggregateFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loan", "Loan.cs");
        WriteCode(file, "Library.Lending.Domain.Loan", "Loan");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("names no declared aggregate", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A file directly in an Application aggregate folder reports that the folder holds only use case folders.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void FileDirectlyInApplicationAggregateFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "LoansHelper.cs");
        WriteCode(file, "Library.Lending.Application.Loans", "LoansHelper");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("sits directly in Loans/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>An aggregate-owned use case folder flat under its layer reports its aggregate folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void FlatAggregateOwnedUseCaseFolderReportsItsAggregateFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "BorrowCopy", "BorrowCopyCommand.cs");
        WriteCode(file, "Library.Lending.Application.BorrowCopy", "BorrowCopyCommand");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("aggregate folder 'Loans/'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A folder under an aggregate folder that names no use case of that aggregate reports the aggregate.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void UnknownUseCaseFolderUnderAggregateReportsItsAggregate()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "StrayCase", "StrayCaseNotes.cs");
        WriteCode(file, "Library.Lending.Application.Loans.StrayCase", "StrayCaseNotes");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("names no use case of aggregate 'Loans'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A port holding its interface and a type a signature names, with its adapter beside it, reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortAndAdapterInNewShapeReportNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<PackRate> CurrentRate(string countryCode, CancellationToken cancellationToken);\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "PackRate.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record PackRate(string CountryCode, decimal Rate);\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A file directly in Ports reports that Ports holds only one folder per port.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void FileDirectlyInPortsReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Ports", "ICountryPack.cs");
        WriteSource(file, "namespace Library.Lending.Ports;\n\npublic interface ICountryPack\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(file, finding.Path);
        Assert.Contains("sits directly in Ports/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A port folder holding two interfaces reports that it holds only its own interface.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortFolderWithTwoInterfacesReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryRates.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryRates\n{\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("holds 2 interfaces", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A port folder whose name is not its interface name without the I reports the interface it holds.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortFolderNamedDifferentlyThanItsInterfaceReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "Pack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.Pack;\n\npublic interface ICountryPack\n{\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "Pack", "PackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.Pack;\n\npublic sealed class PackAdapter\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("whose interface is 'IPack'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A port folder holding no interface reports the interface it holds.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortFolderWithNoInterfaceReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "PackRate.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record PackRate(string CountryCode, decimal Rate);\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("holds no interface", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A type in a port folder that no interface signature names reports the folder it belongs in.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void TypeNoSignatureNamesReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        var stray = ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "RateCache.cs");
        WriteSource(stray, "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed class RateCache\n{\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(stray, finding.Path);
        Assert.Contains("move it to Domain/{Aggregates}/ or the use case folder that uses it", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A type a signature names only through a named result reports nothing: the check follows one level in.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void TypeNamedOneLevelIntoSignaturesReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<PackPage> CurrentPage(string countryCode);\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "PackPage.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record PackPage(PackRate Rate, int Total);\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "PackRate.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record PackRate(string CountryCode, decimal Rate);\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A type a signature names only two levels in reports: the check follows one level, not further.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void TypeNamedTwoLevelsIntoSignaturesReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<PackPage> CurrentPage(string countryCode);\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "PackPage.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record PackPage(PackRate Rate, int Total);\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "PackRate.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record PackRate(string CountryCode, Currency Amount);\n");
        var deep = ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "Currency.cs");
        WriteSource(deep, "namespace Library.Lending.Ports.CountryPack;\n\npublic sealed record Currency(string Code);\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal(deep, finding.Path);
        Assert.Contains("'Currency'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>An extension block in a port folder names no type, so the port folder still reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortFolderWithExtensionBlockReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "PackRateExtensions.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\nextension(PackRate rate)\n{\n    public bool IsZero => rate.Rate == 0;\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A port interface under Application/Ports reports the Ports folder it belongs in.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortInApplicationPortsReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/text-gateway", "text-gateway", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Application", "Ports", "TextGateway", "ITextGateway.cs");
        WriteSource(file, "namespace Library.Lending.Application.Ports.TextGateway;\n\npublic interface ITextGateway\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Port, "ITextGateway", file, "external-system/text-gateway"));

        var placed = Run(model);

        // The Application Ports folder also names no declared use case.
        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Port 'ITextGateway' sits in folder 'TextGateway', but marks 'external-system/text-gateway', so it belongs in 'Lending/Ports/TextGateway/'.");
    }

    /// <summary>A port interface in a use case folder reports the Ports folder it belongs in.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortInterfaceInUseCaseFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/text-gateway", "text-gateway", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "ITextGateway.cs");
        WriteSource(file, "namespace Library.Lending.Application.Loans.BorrowCopy;\n\npublic interface ITextGateway\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Port, "ITextGateway", file, "external-system/text-gateway"));

        var finding = Assert.Single(Run(model));

        Assert.Equal("Port 'ITextGateway' sits in folder 'BorrowCopy', but marks 'external-system/text-gateway', so it belongs in 'Lending/Ports/TextGateway/'.", finding.Message);
        Assert.Equal("external-system/text-gateway", finding.Subject);
    }

    /// <summary>An adapter in an ExternalSystems folder reports the Infrastructure folder it belongs in.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterInExternalSystemsFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/text-gateway", "text-gateway", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Infrastructure", "ExternalSystems", "TextGatewayAdapter.cs");
        WriteSource(file, "namespace Library.Lending.Infrastructure.ExternalSystems;\n\npublic sealed class TextGatewayAdapter\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Adapter, "TextGatewayAdapter", file, "external-system/text-gateway"));

        var placed = Run(model);

        // The ExternalSystems folder also names no port folder.
        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Adapter 'TextGatewayAdapter' sits in folder 'ExternalSystems', but marks 'external-system/text-gateway', so it belongs in 'Lending/Infrastructure/TextGateway/'.");
    }

    /// <summary>An adapter in a use case folder reports the Infrastructure folder it belongs in.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterInUseCaseFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/text-gateway", "text-gateway", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Infrastructure", "Loans", "BorrowCopy", "TextGatewayAdapter.cs");
        WriteSource(file, "namespace Library.Lending.Infrastructure.Loans.BorrowCopy;\n\npublic sealed class TextGatewayAdapter\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Adapter, "TextGatewayAdapter", file, "external-system/text-gateway"));

        var placed = Run(model);

        // The Loans folder also names no port folder.
        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Adapter 'TextGatewayAdapter' sits in folder 'BorrowCopy', but marks 'external-system/text-gateway', so it belongs in 'Lending/Infrastructure/TextGateway/'.");
    }

    /// <summary>An adapter beside the port its mark names reports nothing, whatever its own class name.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterBesideItsPortReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/text-gateway", "text-gateway", temp.Path));
        var port = ProductFile(temp.Path, "Lending", "Ports", "TextGateway", "ITextGateway.cs");
        WriteSource(port, "namespace Library.Lending.Ports.TextGateway;\n\npublic interface ITextGateway\n{\n}\n");
        var adapter = ProductFile(temp.Path, "Lending", "Infrastructure", "TextGateway", "TextGatewaySmtpClient.cs");
        WriteSource(adapter, "namespace Library.Lending.Infrastructure.TextGateway;\n\npublic sealed class TextGatewaySmtpClient\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Port, "ITextGateway", port, "external-system/text-gateway"));
        model.Code.Elements.Add(Element(CodeKind.Adapter, "TextGatewaySmtpClient", adapter, "external-system/text-gateway"));

        Assert.Empty(Run(model));
    }

    /// <summary>An adapter away from the port its mark names reports the port folder, not its own class name.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterAwayFromItsPortReportsPortFolder()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/text-gateway", "text-gateway", temp.Path));
        var port = ProductFile(temp.Path, "Lending", "Ports", "TextGateway", "ITextGateway.cs");
        WriteSource(port, "namespace Library.Lending.Ports.TextGateway;\n\npublic interface ITextGateway\n{\n}\n");
        var adapter = ProductFile(temp.Path, "Lending", "Infrastructure", "ExternalSystems", "TextGatewaySmtpClient.cs");
        WriteSource(adapter, "namespace Library.Lending.Infrastructure.ExternalSystems;\n\npublic sealed class TextGatewaySmtpClient\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Port, "ITextGateway", port, "external-system/text-gateway"));
        model.Code.Elements.Add(Element(CodeKind.Adapter, "TextGatewaySmtpClient", adapter, "external-system/text-gateway"));

        var placed = Run(model);

        // The ExternalSystems folder also names no port folder, and the port also names no adapter folder.
        Assert.Equal(3, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Adapter 'TextGatewaySmtpClient' sits in folder 'ExternalSystems', but marks 'external-system/text-gateway', so it belongs in 'Lending/Infrastructure/TextGateway/'.");
    }

    /// <summary>A port with no adapter folder of the same name reports the adapter folder it pairs with.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortWithNoAdapterReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "CountryPack", "ICountryPack.cs"),
            "namespace Library.Lending.Ports.CountryPack;\n\npublic interface ICountryPack\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("names no adapter folder", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Infrastructure/CountryPack/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>An adapter with no port folder of the same name reports the port folder it pairs with.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void AdapterWithNoPortReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "CountryPack", "CountryPackAdapter.cs"),
            "namespace Library.Lending.Infrastructure.CountryPack;\n\npublic sealed class CountryPackAdapter\n{\n}\n");

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Contains("names no port folder", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Ports/CountryPack/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A port in its module contracts reports the Contracts folder and the interface.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    /// <proves>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact]
    public void PortInModuleContractsReportsDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/text-gateway", "text-gateway", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Contracts", "Ports", "ITextGateway.cs");
        WriteSource(file, "namespace Library.Lending.Contracts.Ports;\n\npublic interface ITextGateway\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Port, "ITextGateway", file, "external-system/text-gateway"));

        var placed = Run(model);

        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Folder 'apps/api/src/Library.Modules/Lending/Contracts/Ports' names no declared aggregate or public module-level use case of module 'Lending'.");
        Assert.Contains(placed, finding => finding.Message == "Interface 'ITextGateway' sits in Contracts, which publishes data only.");
    }

    /// <summary>An acceptance test flat under its module reports that it belongs in its aggregate folder.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void AggregateOwnedTestFlatUnderItsModuleReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = TestFile(temp.Path, "Lending", "BorrowCopy", "BorrowCopyScenarios.cs");
        model.Code.Elements.Add(TestElement("BorrowCopyScenarios", "Borrowing_an_available_copy_succeeds", file));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("code-placement-drift", finding.Code);
        Assert.Equal("scenario/borrow-copy.borrowing-an-available-copy-succeeds", finding.Subject);
        Assert.Contains("Lending/Loans/BorrowCopy/", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A port folder naming a provider module reports that the module is reached through its Contracts.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PortFolderNamingProviderModuleReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.Module, "module/spending", "spending", temp.Path));
        WriteSource(
            ProductFile(temp.Path, "Lending", "Ports", "Spending", "ISpending.cs"),
            "namespace Library.Lending.Ports.Spending;\n\npublic interface ISpending\n{\n    Task<int> Ping();\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "Spending", "SpendingAdapter.cs"),
            "namespace Library.Lending.Infrastructure.Spending;\n\npublic sealed class SpendingAdapter\n{\n}\n");

        var placed = Run(model);

        Assert.Equal(2, placed.Count);
        Assert.Contains(placed, finding => finding.Message == "Port folder 'apps/api/src/Library.Modules/Lending/Ports/Spending' names the module 'spending'; a module reaches another module through its Contracts, not a port.");
        Assert.Contains(placed, finding => finding.Message == "Adapter folder 'apps/api/src/Library.Modules/Lending/Infrastructure/Spending' names the module 'spending'; a module reaches another module through its Contracts, not an adapter.");
    }

    /// <summary>A port folder spelling another name than its external-system record reports the record name.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortFolderSpellingAnotherNameThanItsRecordReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/mail-relay", "mail-relay", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Ports", "EmailMessenger", "IEmailMessenger.cs");
        WriteSource(file, "namespace Library.Lending.Ports.EmailMessenger;\n\npublic interface IEmailMessenger\n{\n    Task SendAsync(string key);\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "EmailMessenger", "EmailMessengerAdapter.cs"),
            "namespace Library.Lending.Infrastructure.EmailMessenger;\n\npublic sealed class EmailMessengerAdapter\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Port, "IEmailMessenger", file, "external-system/mail-relay"));

        var finding = Assert.Single(Run(model));
        Assert.Equal("Port folder 'apps/api/src/Library.Modules/Lending/Ports/EmailMessenger' carries the port for 'external-system/mail-relay', so it belongs in 'Ports/MailRelay/'.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A port folder spelling its external-system record reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</proves>
    [Fact]
    public void PortFolderSpellingItsRecordReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        model.Records.Add(Rec(RecordTypes.ExternalSystem, "external-system/mail-relay", "mail-relay", temp.Path));
        var file = ProductFile(temp.Path, "Lending", "Ports", "MailRelay", "IMailRelay.cs");
        WriteSource(file, "namespace Library.Lending.Ports.MailRelay;\n\npublic interface IMailRelay\n{\n    Task SendAsync(string key);\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "MailRelay", "MailRelayAdapter.cs"),
            "namespace Library.Lending.Infrastructure.MailRelay;\n\npublic sealed class MailRelayAdapter\n{\n}\n");
        model.Code.Elements.Add(Element(CodeKind.Port, "IMailRelay", file, "external-system/mail-relay"));

        Assert.Empty(Run(model));
    }

    /// <summary>A Persistence folder nesting its repositories by aggregate, beside module-wide store setup, reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceAggregateFolderWithStoreSetupReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "Persistence", "Loans", "LoanRepository.cs"),
            "namespace Library.Lending.Infrastructure.Persistence.Loans;\n\npublic sealed class LoanRepository\n{\n}\n");
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "Persistence", "LendingStoreContribution.cs"),
            "namespace Library.Lending.Infrastructure.Persistence;\n\npublic sealed class LendingStoreContribution\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A Persistence folder naming no declared aggregate reports the folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceStrayFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Infrastructure", "Persistence", "Archive", "ArchiveStore.cs"),
            "namespace Library.Lending.Infrastructure.Persistence.Archive;\n\npublic sealed class ArchiveStore\n{\n}\n");

        var finding = Assert.Single(Run(model));
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Lending/Infrastructure/Persistence/Archive' names no declared aggregate of module 'Lending'.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A use case model in Persistence reports that it belongs in its use case folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void PersistenceUseCaseModelFileReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Infrastructure", "Persistence", "BorrowCopyQuery.cs");
        WriteSource(file, "namespace Library.Lending.Infrastructure.Persistence;\n\npublic sealed record BorrowCopyQuery();\n");

        var finding = Assert.Single(Run(model));
        Assert.Equal($"File 'apps/api/src/Library.Modules/Lending/Infrastructure/Persistence/BorrowCopyQuery.cs' holds a message, result or transport model, which sits in its use case folder, not in Persistence/.", finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>A Settings folder holding the module options class reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SettingsWithModuleOptionsReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteSource(
            ProductFile(temp.Path, "Lending", "Application", "Settings", "LendingOptions.cs"),
            "namespace Library.Lending.Application.Settings;\n\npublic sealed class LendingOptions\n{\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A file beside the module options class reports that Settings holds only the options class.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SettingsStrayFileReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Settings", "ReminderPolicy.cs");
        WriteSource(file, "namespace Library.Lending.Application.Settings;\n\npublic sealed class ReminderPolicy\n{\n}\n");

        var finding = Assert.Single(Run(model));
        Assert.Equal("File 'apps/api/src/Library.Modules/Lending/Application/Settings/ReminderPolicy.cs' sits in 'Settings/', which holds only 'LendingOptions.cs'.", finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>A folder under Settings reports that Settings holds files only.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void SettingsSubfolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var folder = ProductFile(temp.Path, "Lending", "Application", "Settings", "Archive");
        Directory.CreateDirectory(folder);

        var finding = Assert.Single(Run(model));
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Lending/Application/Settings/Archive' sits in 'Settings/', which holds files only.", finding.Message);
        Assert.Equal(folder, finding.Path);
    }

    /// <summary>An exception beside its aggregate root reports its Exceptions folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void ExceptionBesideAggregateRootReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "LoanStuckException.cs");
        WriteCode(file, "Library.Lending.Domain.Loans", "LoanStuckException");

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds an exception beside its aggregate root; it belongs in 'Exceptions/'.", finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>An event outside its Events folder reports its Events folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void EventOutsideEventsFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "LoanBorrowedEvent.cs");
        WriteCode(file, "Library.Lending.Domain.Loans", "LoanBorrowedEvent");

        var finding = Assert.Single(Run(model));
        Assert.Contains("holds an event outside its Events/ folder; it belongs in 'Events/'.", finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>An event and an exception in their folders report nothing.</summary>
    /// <proves>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</proves>
    [Fact]
    public void EventAndExceptionInTheirFoldersReportNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteCode(ProductFile(temp.Path, "Lending", "Domain", "Loans", "Events", "LoanBorrowedEvent.cs"), "Library.Lending.Domain.Loans.Events", "LoanBorrowedEvent");
        WriteCode(ProductFile(temp.Path, "Lending", "Domain", "Loans", "Exceptions", "CopyNotAvailableException.cs"), "Library.Lending.Domain.Loans.Exceptions", "CopyNotAvailableException");
        WriteCode(ProductFile(temp.Path, "Lending", "Domain", "Loans", "Loan.cs"), "Library.Lending.Domain.Loans", "Loan");

        Assert.Empty(Run(model));
    }

    /// <summary>A test folder naming no use case of its aggregate reports the aggregate.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void TestFolderNamingNoUseCaseReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = TestFile(temp.Path, "Lending", "Loans", "StrayCase", "StrayCaseScenarios.cs");
        WriteCode(file, "Library.Acceptance.Tests.Lending.Loans.StrayCase", "StrayCaseScenarios");

        var finding = Assert.Single(Run(model));
        Assert.Equal("Folder 'apps/api/tests/Library.Acceptance.Tests/Lending/Loans/StrayCase' names no use case of aggregate 'Loans'.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Support and Infrastructure test folders report nothing.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void TestSupportAndInfrastructureFoldersReportNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        WriteCode(TestFile(temp.Path, "Support", "ScenarioSupport.cs"), "Library.Acceptance.Tests.Support", "ScenarioSupport");
        WriteCode(TestFile(temp.Path, "Lending", "Infrastructure", "GatewayTests.cs"), "Library.Acceptance.Tests.Lending.Infrastructure", "GatewayTests");

        Assert.Empty(Run(model));
    }

    /// <summary>A handler with a stale layer namespace reports the layer its folder states.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void HandlerWithStaleLayerNamespaceReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Lending.Domain.Loans.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        var finding = Assert.Single(Run(model));
        Assert.Equal("Handler 'BorrowCopyCommandHandler' declares namespace 'Library.Lending.Domain.Loans.BorrowCopy', but its file sits in 'Application/', which the namespace does not name.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A handler with the layer namespace its folder states reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void HandlerWithMatchingLayerNamespaceReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Lending.Application.Loans.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        Assert.Empty(Run(model));
    }

    /// <summary>Adds the effects a reaction or job record declares, as the record file's effects list.</summary>
    /// <param name="record">The reaction or job record to change.</param>
    /// <param name="effects">The effects list as JSON.</param>
    /// <returns>The same record, with its effects set.</returns>
    static LiteSpec.Cli.Model.Records.Record WithEffects(LiteSpec.Cli.Model.Records.Record record, string effects)
    {
        using var document = JsonDocument.Parse("{\"status\": \"reviewed\", \"effects\": " + effects + "}");

        return new LiteSpec.Cli.Model.Records.Record
        {
            Type = record.Type,
            Id = record.Id,
            Folder = record.Folder,
            Module = record.Module,
            Title = record.Title,
            File = record.File,
            Line = record.Line,
            Raw = document.RootElement.Clone(),
        };
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

    /// <summary>A model with the modules root, the host, the test root and the records the placement cases mark.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="withFrontend">Whether the model declares a frontend with a modules root.</param>
    /// <param name="modules">The modules root the model declares.</param>
    /// <param name="webapi">The webapi field the model declares, as JSON.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel PlacementModel(string root, bool withFrontend = false, string modules = "apps/api/src/Library.Modules", string webapi = "\"apps/api/src/Library.WebApi\"")
    {
        var frontends = withFrontend
            ? """, "frontends": [{"name": "web", "path": "apps/web", "platform": "svelte"}]"""
            : "";

        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\"" + frontends + ", \"paths\": {\"testRoots\": [\"apps/api/tests\"]}, \"stackConfig\": {\"dotnet\": {\"modules\": \"" + modules + "\", \"webapi\": " + webapi + "}}}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec(RecordTypes.Module, "module/lending", "lending", root));
        model.Records.Add(Rec(RecordTypes.Aggregate, "aggregate/lending.loan", "lending/loans", root));
        model.Records.Add(Rec(RecordTypes.UseCase, "use-case/lending.borrow-copy", "lending/loans/use-case/borrow-copy", root));
        model.Records.Add(Rec(RecordTypes.Scenario, "scenario/borrow-copy.borrowing-an-available-copy-succeeds", "lending/loans/use-case/borrow-copy/scenario/borrowing-an-available-copy-succeeds", root));
        model.Records.Add(Rec(RecordTypes.DomainEvent, "domain-event/loan.loan-borrowed", "lending/loans/use-case/borrow-copy/domain-event/loan-borrowed", root));

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

    /// <summary>One test proving the borrowing scenario.</summary>
    /// <param name="class">The test class name.</param>
    /// <param name="method">The test method name.</param>
    /// <param name="file">The file the test sits in.</param>
    /// <returns>The marked test element.</returns>
    static MarkedElement TestElement(string @class, string method, string file) =>
        new(CodeKind.Test, @class, method, file, 3, [new CodeMark("scenario/borrow-copy.borrowing-an-available-copy-succeeds", MarkStyle.Xml, MarkVerb.Proves, file, 1)], InTests: true);

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

    /// <summary>Writes one C# file with the source as given.</summary>
    /// <param name="file">The full path of the file to write.</param>
    /// <param name="source">The file contents.</param>
    static void WriteSource(string file, string source)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, source);
    }

    /// <summary>One record with its placement path.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="qualified">The placement path: module, aggregate and name joined by dots.</param>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <returns>The record.</returns>
    static LiteSpec.Cli.Model.Records.Record Rec(string type, string id, string qualified, string root)
    {
        using var document = JsonDocument.Parse("""{"status": "reviewed"}""");

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
