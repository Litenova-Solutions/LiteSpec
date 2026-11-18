using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves the backend mark-to-folder check.</summary>
public sealed class MarkFolderDriftTests
{
    /// <summary>Handler In Another Module Folder Reports Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void HandlerInAnotherModuleFolderReportsMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Billing", "Application", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Handler 'HoldRoomCommandHandler'", finding.Message);
        Assert.Contains("module folder 'Billing'", finding.Message);
        Assert.Contains("use-case/booking.hold-room", finding.Message);
        Assert.Equal("use-case/booking.hold-room", finding.Subject);
        Assert.EndsWith(".", finding.Message);
    }

    /// <summary>Handler Marking Another Use Case In Its Module Reports Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void HandlerMarkingAnotherUseCaseInItsModuleReportsMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Booking", "Application", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.release-hold"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Handler 'HoldRoomCommandHandler'", finding.Message);
        Assert.Contains("folder 'HoldRoom'", finding.Message);
        Assert.Contains("use-case/booking.release-hold", finding.Message);
        Assert.Equal("use-case/booking.release-hold", finding.Subject);
    }

    /// <summary>Handler In Its Module And Use Case Folder Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void HandlerInItsModuleAndUseCaseFolderReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Booking", "Application", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Aggregate Root Outside Its Module Reports Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void AggregateRootOutsideItsModuleReportsMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Domain", "Loans", "Invoice.cs");
        model.Code.Elements.Add(Element(CodeKind.AggregateRoot, "Invoice", file, "aggregate/billing.invoice"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Aggregate root 'Invoice'", finding.Message);
        Assert.Contains("module folder 'Lending'", finding.Message);
        Assert.Contains("aggregate/billing.invoice", finding.Message);
        Assert.Equal("aggregate/billing.invoice", finding.Subject);
    }

    /// <summary>Job Outside Its Module Reports Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void JobOutsideItsModuleReportsMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Lending", "Application", "SendReminders", "SendRemindersJob.cs");
        model.Code.Elements.Add(Element(CodeKind.Job, "SendRemindersJob", file, "job/billing.send-reminders"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Job 'SendRemindersJob'", finding.Message);
        Assert.Contains("module folder 'Lending'", finding.Message);
        Assert.Contains("job/billing.send-reminders", finding.Message);
        Assert.Equal("job/billing.send-reminders", finding.Subject);
    }

    /// <summary>Element Outside Layer Roots Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ElementOutsideLayerRootsReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = Path.Combine(temp.Path, "scratch", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Element Directly In Modules Root Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ElementDirectlyInModulesRootReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Mark Naming No Record Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void MarkNamingNoRecordReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Billing", "Application", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.ghost"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Test Element Outside Its Module Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void TestElementOutsideItsModuleReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Billing", "Application", "HoldRoom", "HoldRoomCommandHandler.cs");
        var element = Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room") with
        {
            InTests = true,
        };
        model.Code.Elements.Add(element);

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Element Under Provider Plumbing Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ElementUnderProviderPlumbingReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Booking", "Infrastructure", "Persistence", "Postgres", "NpgsqlModule.cs");
        model.Code.Elements.Add(Element(CodeKind.Technical, "NpgsqlModule", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Element Under Module Contracts Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ElementUnderModuleContractsReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Booking", "Contracts", "ModuleEvents", "Money.cs");
        model.Code.Elements.Add(Element(CodeKind.Technical, "Money", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Element Under Kernel Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ElementUnderKernelReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.Kernel", "Money.cs");
        model.Code.Elements.Add(Element(CodeKind.Technical, "Money", file, "aggregate/billing.invoice"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Handler Outside Its Module In The Module Layout Reports Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHandlerOutsideItsModuleReportsMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Billing", "Application", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.Contains("module folder 'Billing'", finding.Message);
        Assert.Contains("use-case/booking.hold-room", finding.Message);
    }

    /// <summary>Handler In Its Module In The Module Layout Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHandlerInItsModuleReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = ProductFile(temp.Path, "Booking", "Application", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>Host Endpoint In The Module Layout Reports No Mark Folder Drift.</summary>
    /// <proves>litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case</proves>
    [Fact]
    public void ModuleLayoutHostEndpointReportsNoMarkFolderDrift()
    {
        using var temp = TestCli.Temp("marks");
        var model = ModuleModel(temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.WebApi", "Protocol", "SignOutEndpoint.cs");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "SignOutEndpoint", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <summary>A model with the modules root and the records the drift cases mark.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel ModuleModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\", \"webapi\": \"apps/api/src/Library.WebApi\"}}}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("use-case", "use-case/booking.hold-room", root, """{"status": "reviewed", "kind": "command"}"""));
        model.Records.Add(Rec("use-case", "use-case/booking.release-hold", root, """{"status": "reviewed", "kind": "command"}"""));
        model.Records.Add(Rec("use-case", "use-case/billing.raise-invoice", root, """{"status": "reviewed", "kind": "command"}"""));
        model.Records.Add(Rec("aggregate", "aggregate/billing.invoice", root, """{"status": "reviewed"}"""));
        model.Records.Add(Rec("job", "job/billing.send-reminders", root, """{"status": "reviewed"}"""));

        return model;
    }

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

    /// <summary>One backend element carrying one mark.</summary>
    /// <param name="kind">The code element kind.</param>
    /// <param name="class">The class name carrying the mark.</param>
    /// <param name="file">The file the element sits in.</param>
    /// <param name="recordId">The record id the mark names.</param>
    /// <param name="style">The mark style to write.</param>
    /// <returns>The marked code element.</returns>
    static MarkedElement Element(CodeKind kind, string @class, string file, string recordId, MarkStyle style = MarkStyle.Xml) =>
        new(kind, @class, null, file, 3, [new CodeMark(recordId, style, MarkVerb.Implements, file, 1)], InTests: false);

    static Record Rec(string type, string id, string root, string json)
    {
        using var document = JsonDocument.Parse(json);
        var raw = document.RootElement.Clone();

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = Path.Combine(root, ".litespec", "record.md"),
            Line = 1,
            Raw = raw,
        };
    }
}
