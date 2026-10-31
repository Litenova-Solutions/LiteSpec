using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Support;
using Record = LiteSpec.Model.Record;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves the backend mark-to-folder check and the per-language mark style check.</summary>
public sealed class MarkFolderDriftTests
{
    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Handler_In_Another_Module_Folder_Reports_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Application", "Billing", "Holds", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Handler 'HoldRoomCommandHandler'", finding.Message);
        Assert.Contains("module folder 'Billing'", finding.Message);
        Assert.Contains("use-case/booking.hold-room", finding.Message);
        Assert.Equal("use-case/booking.hold-room", finding.Subject);
        Assert.EndsWith(".", finding.Message);
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Handler_Marking_Another_Use_Case_In_Its_Module_Reports_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Application", "Booking", "Holds", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.release-hold"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Handler 'HoldRoomCommandHandler'", finding.Message);
        Assert.Contains("folder 'HoldRoom'", finding.Message);
        Assert.Contains("use-case/booking.release-hold", finding.Message);
        Assert.Equal("use-case/booking.release-hold", finding.Subject);
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Handler_In_Its_Module_And_Use_Case_Folder_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Application", "Booking", "Holds", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Endpoint_Under_Endpoints_Folder_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "WebApi", "Endpoints", "Billing", "Invoices", "RaiseInvoice", "RaiseInvoiceEndpoint.cs");
        model.Code.Elements.Add(Element(CodeKind.Endpoint, "RaiseInvoiceEndpoint", file, "use-case/billing.raise-invoice"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Aggregate_Root_Outside_Its_Module_Reports_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Domain", "Lending", "Loans", "Invoice.cs");
        model.Code.Elements.Add(Element(CodeKind.AggregateRoot, "Invoice", file, "aggregate/billing.invoice"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Aggregate root 'Invoice'", finding.Message);
        Assert.Contains("module folder 'Lending'", finding.Message);
        Assert.Contains("aggregate/billing.invoice", finding.Message);
        Assert.Equal("aggregate/billing.invoice", finding.Subject);
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Job_Outside_Its_Module_Reports_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Application", "Lending", "Jobs", "SendRemindersJob.cs");
        model.Code.Elements.Add(Element(CodeKind.Job, "SendRemindersJob", file, "job/billing.send-reminders"));

        var drifts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift");

        var finding = Assert.Single(drifts);
        Assert.StartsWith("Job 'SendRemindersJob'", finding.Message);
        Assert.Contains("module folder 'Lending'", finding.Message);
        Assert.Contains("job/billing.send-reminders", finding.Message);
        Assert.Equal("job/billing.send-reminders", finding.Subject);
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Element_Outside_Layer_Roots_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = Path.Combine(temp.Path, "scratch", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Element_Directly_In_Layer_Root_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Application", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Mark_Naming_No_Record_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Application", "Billing", "Holds", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.ghost"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Test_Element_Outside_Its_Module_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Application", "Billing", "Holds", "HoldRoom", "HoldRoomCommandHandler.cs");
        var element = Element(CodeKind.Handler, "HoldRoomCommandHandler", file, "use-case/booking.hold-room") with
        {
            InTests = true,
        };
        model.Code.Elements.Add(element);

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Element_Under_Provider_Plumbing_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = LayerFile(temp.Path, "Infrastructure", "Persistence", "Postgres", "NpgsqlModule.cs");
        model.Code.Elements.Add(Element(CodeKind.Technical, "NpgsqlModule", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Element_Under_Cross_Module_Contracts_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Library.Application.Abstractions", "Shared", "Money.cs");
        model.Code.Elements.Add(Element(CodeKind.Technical, "Money", file, "use-case/booking.hold-room"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/architecture#organize-every-layer-by-module-and-use-case</proves>
    [Fact]
    public void Element_Under_Shared_Kernel_Reports_No_Mark_Folder_Drift()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path, "xml", "\"sharedKernel\": \"apps/api/src/Library.Domain/Shared\"");
        var file = LayerFile(temp.Path, "Domain", "Shared", "Money.cs");
        model.Code.Elements.Add(Element(CodeKind.Technical, "Money", file, "aggregate/billing.invoice"));

        Assert.Empty(MarksFixture.WithCode(Checker.Check(model).Findings, "mark-folder-drift"));
    }

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact]
    public void Frontend_Mark_In_Undeclared_Style_Reports_No_Mark_Style_Invalid()
    {
        using var temp = Cli.Temp("marks");
        var model = DriftModel(temp.Path, "attribute");
        var webFile = Path.Combine(temp.Path, "apps", "web", "src", "lib", "modules", "booking", "holds", "hold-room", "litespec.ts");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, "booking/holds/hold-room", null, webFile, 3,
            [new CodeMark("use-case/booking.hold-room", MarkStyle.Xml, MarkVerb.Implements, webFile, 1)],
            InTests: false));
        var handlerFile = LayerFile(temp.Path, "Application", "Booking", "Holds", "HoldRoom", "HoldRoomCommandHandler.cs");
        model.Code.Elements.Add(Element(CodeKind.Handler, "HoldRoomCommandHandler", handlerFile, "use-case/booking.hold-room"));

        var styled = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-style-invalid");

        var finding = Assert.Single(styled);
        Assert.Contains("HoldRoomCommandHandler", finding.Message);
        Assert.DoesNotContain("Frontend module", finding.Message);
    }

    /// <summary>A model with layer roots and the records the drift cases mark.</summary>
    static ProjectModel DriftModel(string root, string marks = "xml", string? dotnetExtra = null)
    {
        var dotnet = """
            "application": "apps/api/src/Library.Application",
            "domain": "apps/api/src/Library.Domain",
            "webapi": "apps/api/src/Library.WebApi",
            "infrastructure": "apps/api/src/Library.Infrastructure",
            "worker": "apps/api/src/Library.Worker"
            """;

        if (dotnetExtra is { Length: > 0 })
        {
            dotnet += "," + dotnetExtra;
        }

        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"" + marks + "\", \"stackConfig\": {\"dotnet\": {" + dotnet + "}}}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("use-case", "use-case/booking.hold-room", root, """{"status": "reviewed", "kind": "command"}"""));
        model.Records.Add(Rec("use-case", "use-case/booking.release-hold", root, """{"status": "reviewed", "kind": "command"}"""));
        model.Records.Add(Rec("use-case", "use-case/billing.raise-invoice", root, """{"status": "reviewed", "kind": "command"}"""));
        model.Records.Add(Rec("aggregate", "aggregate/billing.invoice", root, """{"status": "reviewed"}"""));
        model.Records.Add(Rec("job", "job/billing.send-reminders", root, """{"status": "reviewed"}"""));

        return model;
    }

    /// <summary>One backend element carrying one mark.</summary>
    static MarkedElement Element(CodeKind kind, string @class, string file, string recordId, MarkStyle style = MarkStyle.Xml) =>
        new(kind, @class, null, file, 3, [new CodeMark(recordId, style, MarkVerb.Implements, file, 1)], InTests: false);

    /// <summary>One file under a layer root.</summary>
    static string LayerFile(string root, string layer, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library." + layer };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }

    static Record Rec(string type, string id, string root, string json)
    {
        using var document = JsonDocument.Parse(json);
        var raw = document.RootElement.Clone();

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            Owner = "lending-team",
            Status = "reviewed",
            File = Path.Combine(root, "docs", "record.md"),
            Line = 1,
            Raw = raw,
        };
    }
}
