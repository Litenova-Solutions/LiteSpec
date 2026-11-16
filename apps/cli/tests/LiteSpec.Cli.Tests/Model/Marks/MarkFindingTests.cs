using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves the mark presence and link findings against the marks fixtures, and the frontend count finding against an inline model.</summary>
public sealed class MarkFindingTests
{
    /// <summary>Handler Without A Mark Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void HandlerWithoutAMarkReportsMarkMissing()
    {
        using var fixture = MarksFixture.Prepare("missing");
        var missing = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Handler", finding.Message);
        Assert.Contains("BorrowCopyCommandHandler", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Mark Naming No Record Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkNamingNoRecordReportsMarkUnresolved()
    {
        using var fixture = MarksFixture.Prepare("unresolved");
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("matches no record"));
        Assert.Contains("use-case/lending.ghost", finding.Message);
        Assert.Equal("use-case/lending.ghost", finding.Subject);
    }

    /// <summary>Mark Naming A Record Of Another Type Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkNamingARecordOfAnotherTypeReportsMarkUnresolved()
    {
        using var fixture = MarksFixture.Prepare("unresolved");
        var unresolved = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "mark-unknown");

        var finding = Assert.Single(unresolved, found => found.Message.Contains("not a use-case record"));
        Assert.Contains("rule/loan.copy-available", finding.Message);
        Assert.Contains("rule record", finding.Message);
        Assert.Equal("rule/loan.copy-available", finding.Subject);
    }

    /// <summary>Route With Two Marks Reports Mark Count.</summary>
    /// <proves>litespec/frontend/structure#mark-each-route-with-its-screen</proves>
    [Fact]
    public void RouteWithTwoMarksReportsMarkCount()
    {
        using var temp = TestCli.Temp("marks");
        var model = RouteModel(temp.Path);
        var webFile = Path.Combine(temp.Path, "apps", "web", "src", "routes", "loans", "+page.svelte");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendRoute, "/loans", null, webFile, 3,
            [
                new CodeMark("screen/app.loans", MarkStyle.Xml, MarkVerb.Implements, webFile, 1),
                new CodeMark("screen/app.holds", MarkStyle.Xml, MarkVerb.Implements, webFile, 2),
            ],
            InTests: false));

        var counts = MarksFixture.WithCode(Checker.Check(model).Findings, "mark-count-invalid");

        var finding = Assert.Single(counts);
        Assert.Contains("carries 2 marks", finding.Message);
        Assert.Contains("takes one", finding.Message);
    }

    /// <summary>A model with the screen record the route marks name.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the records.</returns>
    static ProjectModel RouteModel(string root)
    {
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\"}");
        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("screen", "screen/app.loans", root, """{"status": "reviewed"}"""));
        model.Records.Add(Rec("screen", "screen/app.holds", root, """{"status": "reviewed"}"""));

        return model;
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
            File = Path.Combine(root, ".litespec", "record.md"),
            Line = 1,
            Raw = raw,
        };
    }
}
