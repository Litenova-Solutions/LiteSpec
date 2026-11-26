using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Screens;

/// <summary>Proves the screen shows checks over hand-built models: each rule with a passing and a failing case.</summary>
public sealed class ScreenShowsCheckTests
{
    /// <summary>A row field an offered query outputs reports nothing.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsRowFieldFromOfferedQueryReportsNothing()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """[{"field":"state","from":"use-case/lending.list-loans"}]"""));

        Assert.Empty(WithCode(model, "screen-shows-unknown"));
        Assert.Empty(WithCode(model, "screen-shows-drift"));
    }

    /// <summary>An input field an offered command accepts reports nothing.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsInputFieldFromOfferedCommandReportsNothing()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.borrow", "/borrow", ["use-case/lending.borrow-copy"],
            """[{"field":"reference","from":"use-case/lending.borrow-copy"}]"""));

        Assert.Empty(WithCode(model, "screen-shows-unknown"));
        Assert.Empty(WithCode(model, "screen-shows-drift"));
    }

    /// <summary>A field no offered use case outputs or accepts reports screen-shows-unknown.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsFieldNoOfferedUseCaseCarriesReportsShowsUnknown()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans", "use-case/lending.borrow-copy"],
            """[{"field":"balance","from":"use-case/lending.list-loans"}]"""));

        var finding = Assert.Single(WithCode(model, "screen-shows-unknown"));
        Assert.Contains("balance", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A field from a use case the screen does not offer reports screen-shows-unknown.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsFieldFromUnofferedUseCaseReportsShowsUnknown()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """[{"field":"reference","from":"use-case/lending.borrow-copy"}]"""));

        var finding = Assert.Single(WithCode(model, "screen-shows-unknown"));
        Assert.Contains("does not offer", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A field another offered use case carries reports screen-shows-drift.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsFieldAnotherOfferedUseCaseCarriesReportsShowsDrift()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans", "use-case/lending.borrow-copy"],
            """[{"field":"reference","from":"use-case/lending.list-loans"}]"""));

        var finding = Assert.Single(WithCode(model, "screen-shows-drift"));
        Assert.Contains("use-case/lending.borrow-copy", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A field matching only when case and separators are ignored reports screen-shows-drift.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsFieldMatchingOnlyByNameReportsShowsDrift()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """[{"field":"State","from":"use-case/lending.list-loans"}]"""));

        var finding = Assert.Single(WithCode(model, "screen-shows-drift"));
        Assert.Contains("'state'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A field from a record nothing holds reports reference-unknown.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsFieldFromUnknownRecordReportsReferenceUnknown()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """[{"field":"state","from":"use-case/lending.ghost"}]"""));

        var finding = Assert.Single(WithCode(model, "reference-unknown"));
        Assert.Contains("use-case/lending.ghost", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A field from a record that is no use case reports record-invalid.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ShowsFieldFromNonUseCaseReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """[{"field":"state","from":"entry-point/app"}]"""));

        var finding = Assert.Single(WithCode(model, "record-invalid"));
        Assert.Contains("entry-point/app", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A screen naming no shows entries reports nothing.</summary>
    /// <proves>litespec/core/ui-ia#compose-each-screen-from-its-owners-queries</proves>
    [Fact]
    public void ScreenWithoutShowsReportsNothing()
    {
        using var temp = TestCli.Temp("screen-shows");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], null));

        Assert.Empty(WithCode(model, "screen-shows-unknown"));
        Assert.Empty(WithCode(model, "screen-shows-drift"));
    }

    static List<Finding> WithCode(ProjectModel model, string code)
    {
        var findings = new List<Finding>();
        ScreenShowsChecks.Run(model, findings);
        return findings.Where(finding => finding.Code == code).ToList();
    }

    static ProjectModel BaseModel(string root)
    {
        if (!File.Exists(Path.Combine(root, ".litespec/project.yaml")))
        {
            Directory.CreateDirectory(Path.Combine(root, ".litespec"));
            File.WriteAllText(
                Path.Combine(root, ".litespec/project.yaml"),
                """
                {
                  "owners": ["lending-team"],
                  "frontends": [
                      { "name": "web", "path": "apps/web", "platform": "svelte" }
                    ], "paths": {
                  }
                }
                """);
        }

        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser"}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans",
            """{"kind":"query","reachedThrough":["entry-point/app"],"output":{"include":{"rows":{"fields":[{"name":"loans","type":"list","rowShape":{"fields":[{"name":"loanId"},{"name":"state"}]}}]}}}}""",
            root));
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy",
            """{"kind":"command","reachedThrough":["entry-point/app"],"input":[{"name":"reference"},{"name":"reason"}]}""",
            root));

        return model;
    }

    static Record Screen(string id, string route, IReadOnlyList<string> offers, string? showsJson) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            File = "screen.md",
            Line = 1,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":\"entry-point/app\",\"route\":" + JsonSerializer.Serialize(route)
                + ",\"offers\":" + JsonSerializer.Serialize(offers)
                + ",\"serves\":[],\"leadsTo\":[]"
                + ",\"pageFrame\":{\"pattern\":\"list-page\",\"option\":\"search-and-a-state-filter-only\"}"
                + (showsJson is null ? string.Empty : ",\"fieldSources\":" + showsJson) + "}").RootElement.Clone(),
        };

    static Record Rec(string type, string id, string fields, string root)
    {
        using var document = JsonDocument.Parse(fields);

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = Path.Combine(root, $"{type}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
