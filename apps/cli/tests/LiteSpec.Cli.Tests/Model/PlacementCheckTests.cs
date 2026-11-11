using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the code placement check against the broken overlay and module-layout models.</summary>
public sealed class PlacementCheckTests
{
    /// <summary>The overlay reports one placement finding per drift: the aggregate folder, the namespace, the test folder and the stray folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void BrokenOverlayReportsEachPlacementDriftOnce()
    {
        using var temp = ModelFixture.Prepare("small", "code-placement-drift");
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));
        var findings = new List<Finding>();
        PlacementChecks.Run(model, findings);
        var placed = findings.Where(finding => finding.Code == "code-placement-drift").ToList();

        Assert.Equal(4, placed.Count);
        Assert.Single(placed, finding => finding.Message == "Handler 'BorrowCopyCommandHandler' sits in folder 'Holds', but marks 'use-case/lending.borrow-copy'.");
        Assert.Single(placed, finding => finding.Message == "Handler 'BorrowCopyRetryHandler' declares namespace 'Library.Application.Billing.Loans.BorrowCopy', but 'use-case/lending.borrow-copy' places it under 'Library.Lending.Loans.BorrowCopy'.");
        Assert.Single(placed, finding => finding.Message == "Test 'BorrowCopyScenarios.Borrowing_an_available_copy_succeeds' sits in folder 'Holds', but marks 'scenario/borrow-copy.borrowing-an-available-copy-succeeds'.");
        Assert.Single(placed, finding => finding.Message == "Folder 'apps/api/src/Library.Application/Archive' names no declared module.");
        Assert.All(placed, finding => Assert.EndsWith(".", finding.Message));
    }

    /// <summary>A placed handler in the module layout reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutPlacedHandlerReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyCommandHandler.cs");
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
        var file = ProductFile(temp.Path, "Lending", "Holds", "UseCases", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'BorrowCopyCommandHandler' sits in folder 'Holds', but marks 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A handler in the right folder with the wrong namespace reports the namespace.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHandlerWithWrongNamespaceReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Billing.Loans.BorrowCopy", "BorrowCopyCommandHandler");
        model.Code.Elements.Add(Element(CodeKind.Handler, "BorrowCopyCommandHandler", file, "use-case/lending.borrow-copy"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Handler 'BorrowCopyCommandHandler' declares namespace 'Library.Billing.Loans.BorrowCopy', but 'use-case/lending.borrow-copy' places it under 'Library.Lending.Loans.BorrowCopy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A handler in the right folder with the right namespace reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHandlerWithRightNamespaceReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyCommandHandler.cs");
        WriteCode(file, "Library.Lending.Loans.UseCases.BorrowCopy", "BorrowCopyCommandHandler");
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
        var file = ProductFile(temp.Path, "Billing", "Invoices", "Domain", "Events", "LoanBorrowedEvent.cs");
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "LoanBorrowedEvent", file, "event/loan.loan-borrowed"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Domain event 'LoanBorrowedEvent' sits in module folder 'Billing', but marks 'event/loan.loan-borrowed'.", finding.Message);
        Assert.Equal("event/loan.loan-borrowed", finding.Subject);
    }

    /// <summary>A domain event in its aggregate reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutEventInItsAggregateReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Loans", "Domain", "Events", "LoanBorrowedEvent.cs");
        WriteCode(file, "Library.Lending.Loans.Events", "LoanBorrowedEvent");
        model.Code.Elements.Add(Element(CodeKind.DomainEvent, "LoanBorrowedEvent", file, "event/loan.loan-borrowed"));

        Assert.Empty(Run(model));
    }

    /// <summary>A test under its scenario's use case path reports nothing.</summary>
    /// <proves>litespec/backend/testing#mirror-production-module-names</proves>
    [Fact]
    public void ModuleLayoutPlacedTestReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        var file = TestFile(temp.Path, "Lending", "Loans", "UseCases", "BorrowCopy", "BorrowCopyScenarios.cs");
        WriteCode(file, "Library.Acceptance.Tests.Lending.Loans.UseCases.BorrowCopy", "BorrowCopyScenarios");
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
        var file = TestFile(temp.Path, "Billing", "Invoices", "UseCases", "BorrowCopy", "BorrowCopyScenarios.cs");
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
        var file = TestFile(temp.Path, "Billing", "Invoices", "Infrastructure", "TextGatewayTests.cs");
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

    /// <summary>A folder in the second declared WebApi host under layer projects that names no declared module reports the folder.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host</proves>
    [Fact]
    public void LayerProjectsSecondWebApiHostFolderNamingNoDeclaredModuleReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(
            temp.Path,
            modules: "apps/api/src/Library.Application",
            webapi: "[\"apps/api/src/Library.WebApi\", \"apps/api/src/Library.Search.WebApi\"]");
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Search.WebApi", "Archive"));

        var placed = Run(model);

        Assert.Contains(
            placed,
            finding => finding.Message == "Folder 'apps/api/src/Library.Search.WebApi/Archive' names no declared module.");
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
            CodeKind.FrontendModule, "lending/loans/borrow-copy", null, file, 3,
            [new CodeMark("use-case/lending.borrow-copy", MarkStyle.Typed, MarkVerb.Implements, file, 1)],
            InTests: false));

        Assert.Empty(Run(model));
    }

    /// <summary>A product folder that names no layer folder or declared module reports the folder.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutStrayProductFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Archive"));

        var placed = Run(model);

        var finding = Assert.Single(placed);
        Assert.Equal("Folder 'apps/api/src/Library.Modules/Archive' names no Composition folder or declared module.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>A product tree that mirrors its declared records reports nothing.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutDeclaredTreeReportsNothing()
    {
        using var temp = TestCli.Temp("placement");
        var model = PlacementModel(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "apps", "api", "src", "Library.Modules", "Lending", "Loans", "UseCases", "BorrowCopy"));

        Assert.Empty(Run(model));
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
        model.Records.Add(Rec(RecordTypes.Event, "event/loan.loan-borrowed", "lending/loans/use-case/borrow-copy/event/loan-borrowed", root));

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

    /// <summary>One record with its placement path.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="qualified">The placement path: module, aggregate and name joined by dots.</param>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <returns>The record.</returns>
    static LiteSpec.Cli.Model.Record Rec(string type, string id, string qualified, string root)
    {
        using var document = JsonDocument.Parse("""{"status": "reviewed"}""");

        return new LiteSpec.Cli.Model.Record
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
