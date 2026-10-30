using Record = LiteSpec.Model.Record;
using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Screens;

/// <summary>Proves the screen checks over hand-built models: each rule with a passing and a failing case.</summary>
public sealed class ScreenCheckTests
{
    /// <proves>litespec/core/behavior#give-every-page-its-screen-record</proves>
    [Fact]
    public void Screen_With_No_Route_Reports_Screen_Unimplemented()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));

        Assert.Single(WithCode(model, "screen-unimplemented"));
    }

    /// <proves>litespec/core/behavior#give-every-page-its-screen-record</proves>
    [Fact]
    public void Screen_With_A_Marked_Route_Reports_No_Screen_Unimplemented()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Empty(WithCode(model, "screen-unimplemented"));
        Assert.Empty(WithCode(model, "behavior-unrecorded"));
    }

    /// <proves>litespec/core/behavior#give-every-page-its-screen-record</proves>
    [Fact]
    public void Route_With_No_Mark_Reports_Behavior_Unrecorded()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Code.Elements.Add(RouteElement(null, "/loans", temp.Path));

        var finding = Assert.Single(WithCode(model, "behavior-unrecorded"), found => found.Message.Contains("no screen mark"));
        Assert.Equal(Finding.TypeScriptReader, finding.Reader);
    }

    /// <proves>litespec/core/behavior#give-every-page-its-screen-record</proves>
    [Fact]
    public void Route_Marking_No_Screen_Reports_Behavior_Unrecorded()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Code.Elements.Add(RouteElement("screen/app.ghost", "/loans", temp.Path));

        Assert.Single(WithCode(model, "behavior-unrecorded"), found => found.Message.Contains("names no screen"));
    }

    /// <proves>litespec/core/behavior#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Offers_Differing_From_Calls_Report_Screen_Calls_Mismatch()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.borrow-copy"]);

        var finding = Assert.Single(WithCode(model, "screen-calls-mismatch"));
        Assert.Contains("use-case/lending.list-loans", finding.Message);
        Assert.Contains("use-case/lending.borrow-copy", finding.Message);
    }

    /// <proves>litespec/core/behavior#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void Offers_Equal_To_Calls_Report_No_Screen_Calls_Mismatch()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Empty(WithCode(model, "screen-calls-mismatch"));
    }

    /// <proves>litespec/core/behavior#serve-only-the-steps-a-screens-use-cases-serve</proves>
    [Fact]
    public void Screen_Serving_A_Step_Its_Offers_Do_Not_Serve_Reports_Mismatch()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], ["journey/walk#s2"]));

        Assert.Single(WithCode(model, "screen-serves-mismatch"));
    }

    /// <proves>litespec/core/behavior#serve-only-the-steps-a-screens-use-cases-serve</proves>
    [Fact]
    public void Screen_Serving_A_Step_Its_Offers_Serve_Reports_No_Mismatch()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], ["journey/walk#s1"]));

        Assert.Empty(WithCode(model, "screen-serves-mismatch"));
    }

    /// <proves>litespec/core/behavior#give-every-browser-step-a-screen</proves>
    [Fact]
    public void Browser_Step_With_No_Screen_Reports_Step_Unscreened()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);

        Assert.Single(WithCode(model, "step-unscreened"));
    }

    /// <proves>litespec/core/behavior#give-every-browser-step-a-screen</proves>
    [Fact]
    public void Browser_Step_With_A_Screen_Reports_No_Step_Unscreened()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], ["journey/walk#s1"]));

        Assert.Empty(WithCode(model, "step-unscreened"));
    }

    /// <proves>litespec/core/behavior#show-every-refusal-a-screen-can-reach</proves>
    [Fact]
    public void Reachable_Refusal_With_No_Sentence_Or_Assert_Reports_Result_Unshown()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Single(WithCode(model, "result-unshown"));
    }

    /// <proves>litespec/core/behavior#show-every-refusal-a-screen-can-reach</proves>
    [Fact]
    public void Reachable_Refusal_With_A_Dictionary_Sentence_Reports_No_Result_Unshown()
    {
        using var temp = Cli.Temp("screens");
        WriteProject(temp.Path, "{ \"lending.refused\": \"That copy is not available.\" }");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Empty(WithCode(model, "result-unshown"));
    }

    /// <proves>litespec/core/behavior#show-every-refusal-a-screen-can-reach</proves>
    [Fact]
    public void Reachable_Refusal_With_An_Asserting_Browser_Test_Reports_No_Result_Unshown()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "shows the refusal", Path.Combine(temp.Path, "tests", "loans.spec.ts"), 3,
            [], null, [], ["lending.refused"], []));

        Assert.Empty(WithCode(model, "result-unshown"));
    }

    /// <proves>litespec/frontend/testing#prove-each-scenario-at-one-boundary</proves>
    [Fact]
    public void Scenario_Proved_By_Two_Browser_Tests_Reports_Proves_Duplicated()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(BrowserProof(temp.Path, "first", "/loans"));
        model.BrowserTests.Add(BrowserProof(temp.Path, "second", "/loans"));

        Assert.Single(WithCode(model, "proves-duplicated"));
    }

    /// <proves>litespec/frontend/testing#prove-each-scenario-at-one-boundary</proves>
    [Fact]
    public void Scenario_Proved_By_One_Browser_Test_Reports_No_Proves_Duplicated()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(BrowserProof(temp.Path, "only", "/loans"));

        Assert.Empty(WithCode(model, "proves-duplicated"));
    }

    /// <proves>litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case</proves>
    [Fact]
    public void Browser_Test_Reaching_No_Offering_Screen_Reports_Proves_Unconfirmed()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        model.BrowserTests.Add(BrowserProof(temp.Path, "stray", "/elsewhere"));

        Assert.Single(WithCode(model, "proves-unconfirmed"));
    }

    /// <proves>litespec/frontend/testing#assert-the-result-a-scenario-ends-in</proves>
    [Fact]
    public void Browser_Test_Reaching_The_Screen_And_Asserting_Text_Reports_No_Proves_Unconfirmed()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        model.BrowserTests.Add(BrowserProof(temp.Path, "proof", "/loans"));

        Assert.Empty(WithCode(model, "proves-unconfirmed"));
    }

    /// <proves>litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove</proves>
    [Fact]
    public void Route_Suite_Test_With_No_Proves_Reports_Mark_Missing()
    {
        using var temp = Cli.Temp("screens");
        WriteRouteSuiteProject(temp.Path);
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "every route", Path.Combine(temp.Path, "apps", "web", "tests", "routes", "every-route.spec.ts"), 3,
            [], null, [], [], []));

        Assert.Single(WithCode(model, "mark-missing"), found => found.Message.Contains("route suite"));
    }

    /// <proves>litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove</proves>
    [Fact]
    public void Route_Suite_Test_Proving_A_Provision_Reports_No_Mark_Finding()
    {
        using var temp = Cli.Temp("screens");
        WriteRouteSuiteProject(temp.Path);
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "every route", Path.Combine(temp.Path, "apps", "web", "tests", "routes", "every-route.spec.ts"), 3,
            ["litespec/frontend/web/ui#check-every-route-in-a-browser"], null, [], [], []));

        Assert.Empty(WithCode(model, "mark-missing"));
        Assert.Empty(WithCode(model, "mark-unresolved"));
    }

    /// <proves>litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove</proves>
    [Fact]
    public void Route_Suite_Test_Proving_A_Scenario_Reports_Mark_Unresolved()
    {
        using var temp = Cli.Temp("screens");
        WriteRouteSuiteProject(temp.Path);
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "every route", Path.Combine(temp.Path, "apps", "web", "tests", "routes", "every-route.spec.ts"), 3,
            ["scenario/list-loans.listing-succeeds"], null, [], [], []));

        Assert.Single(WithCode(model, "mark-unresolved"), found => found.Message.Contains("which is a scenario"));
    }

    static void WriteRouteSuiteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web", "tests", "routes"));
        Directory.CreateDirectory(Path.Combine(root, "docs"));
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {
              "owners": ["lending-team"],
              "paths": {
                "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte", "routeSuite": "apps/web/tests/routes" }
                ]
              }
            }
            """);
    }

    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void Module_Mark_Outside_The_Frontends_Entry_Points_Reports_Mark_Unresolved()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Rec("entry-point", "entry-point/admin", """{"kind":"browser"}""", temp.Path));
        model.Records.Add(Rec("use-case", "use-case/lending.other", """{"kind":"query","reachedThrough":["entry-point/admin"]}""", temp.Path));
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        var file = Path.Combine(temp.Path, "apps", "web", "src", "lib", "modules", "lending", "loans", "other", "litespec.ts");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, "lending/loans/other", null, file, 3,
            [new CodeMark("use-case/lending.other", MarkStyle.Xml, MarkVerb.Implements, file, 3)], InTests: false));

        Assert.Single(WithCode(model, "mark-unresolved"), found => found.Message.Contains("outside frontend 'web's entry points"));
    }

    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void Module_Mark_Inside_The_Frontends_Entry_Points_Reports_No_Scope_Finding()
    {
        using var temp = Cli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        var file = Path.Combine(temp.Path, "apps", "web", "src", "lib", "modules", "lending", "loans", "list-loans", "litespec.ts");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, "lending/loans/list-loans", null, file, 3,
            [new CodeMark("use-case/lending.list-loans", MarkStyle.Xml, MarkVerb.Implements, file, 3)], InTests: false));

        Assert.Empty(WithCode(model, "mark-unresolved"));
    }

    /// <proves>litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case</proves>
    [Fact]
    public void Address_Matching_Treats_Parameters_As_Any_Segment()
    {
        Assert.True(LiteSpec.Model.Readers.FrontendFactsRules.AddressMatches("/loans/[id]", "/loans/42"));
        Assert.True(LiteSpec.Model.Readers.FrontendFactsRules.AddressMatches("/loans/{}", "/loans/42"));
        Assert.False(LiteSpec.Model.Readers.FrontendFactsRules.AddressMatches("/loans/[id]", "/loans/42/edit"));
        Assert.False(LiteSpec.Model.Readers.FrontendFactsRules.AddressMatches("/loans", "/cards"));
    }

    /// <proves>litespec/core/model#mark-every-check-with-its-tier</proves>
    [Fact]
    public void Severity_Uses_The_Producing_Readers_Qualification()
    {
        var unqualified = new Finding("screen-unimplemented", "x", "f", 1, null)
        {
            Reader = Finding.TypeScriptReader,
            ReaderIsQualified = false,
        };
        var qualified = new Finding("screen-unimplemented", "x", "f", 1, null)
        {
            Reader = Finding.RoslynReader,
            ReaderIsQualified = true,
        };

        Assert.Equal(Outcome.Reported, Severity.Of(unqualified, null, false, "working", unqualified.ReaderIsQualified).Outcome);
        Assert.Equal(Outcome.Blocking, Severity.Of(qualified, null, false, "working", qualified.ReaderIsQualified).Outcome);
    }

    static List<Finding> WithCode(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(finding => finding.Code == code).ToList();

    static BrowserTestInfo BrowserProof(string root, string title, string target) =>
        new(
            "web", title, Path.Combine(root, "tests", $"{title}.spec.ts"), 3,
            ["scenario/list-loans.listing-succeeds"], null, [target], [], ["Listed"]);

    static void AddRoute(ProjectModel model, string screenId, string address, IReadOnlyList<string> useCases)
    {
        var file = Path.Combine(model.Config.Root, "apps", "web", "src", "routes", "loans", "+page.ts");

        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendRoute, address, null, file, 3,
            [new CodeMark(screenId, MarkStyle.Xml, MarkVerb.Implements, file, 3)], InTests: false));
        model.Code.Endpoints.Add(new EndpointOperation(
            "ListLoans", "use-case/lending.list-loans", "", "",
            Path.Combine(model.Config.Root, "ListLoansEndpoint.cs"), 3));
        model.Screens.Add(new ScreenInfo(
            "web", address, model.Config.Relative(file),
            [new GeneratedOperation("listLoans", "", "")], useCases, screenId));
    }

    static MarkedElement RouteElement(string? screenId, string address, string root)
    {
        var file = Path.Combine(root, "apps", "web", "src", "routes", "loans", "+page.ts");
        var marks = screenId is null
            ? new List<CodeMark>()
            : [new CodeMark(screenId, MarkStyle.Xml, MarkVerb.Implements, file, 3)];

        return new MarkedElement(CodeKind.FrontendRoute, address, null, file, 3, marks, InTests: false);
    }

    static void WriteProject(string root, string dictionary)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web"));
        Directory.CreateDirectory(Path.Combine(root, "docs"));
        File.WriteAllText(Path.Combine(root, "docs", "refusals.json"), dictionary);
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {
              "owners": ["lending-team"],
              "paths": {
                "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte", "refusalDictionary": "docs/refusals.json" }
                ]
              }
            }
            """);
    }

    static ProjectModel BaseModel(string root)
    {
        if (!File.Exists(Path.Combine(root, "litespec.project.json")))
        {
            Directory.CreateDirectory(Path.Combine(root, "docs"));
            File.WriteAllText(
                Path.Combine(root, "litespec.project.json"),
                """
                {
                  "owners": ["lending-team"],
                  "paths": {
                    "frontends": [
                      { "name": "web", "path": "apps/web", "platform": "svelte" }
                    ]
                  }
                }
                """);
        }

        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser"}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","reachedThrough":["entry-point/app"],"serves":["journey/walk#s1"]}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy", """{"kind":"command","reachedThrough":["entry-point/app"],"serves":["journey/walk#s1"]}""", root));
        model.Records.Add(Rec("journey", "journey/walk", """{"actor":["actor/employee"],"goal":"Borrow a copy.","steps":[{"id":"s1","useCase":"use-case/lending.list-loans"},{"id":"s2","useCase":"use-case/lending.list-loans"}]}""", root));
        model.Records.Add(Rec("result", "result/list-loans.listed", """{"outcome":"success","meaning":"The loans are listed."}""", root));
        model.Records.Add(Rec("result", "result/list-loans.refused", """{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused"}""", root));
        model.Records.Add(Rec("scenario", "scenario/list-loans.listing-succeeds", """{"given":"A loan.","when":"The employee lists loans.","then":"Both are listed.","result":"result/list-loans.listed"}""", root));

        return model;
    }

    static Record Screen(string id, string address, IReadOnlyList<string> offers, IReadOnlyList<string> serves) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            Owner = "lending-team",
            Status = "reviewed",
            File = "screen.md",
            Line = 1,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":\"entry-point/app\",\"address\":" + JsonSerializer.Serialize(address)
                + ",\"offers\":" + JsonSerializer.Serialize(offers)
                + ",\"serves\":" + JsonSerializer.Serialize(serves) + "}").RootElement.Clone(),
        };

    static Record Rec(string type, string id, string fields, string root)
    {
        using var document = JsonDocument.Parse(fields);

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            Owner = "lending-team",
            Status = type is "entry-point" or "use-case" or "journey" or "scenario" ? "reviewed" : "approved",
            File = Path.Combine(root, $"{type}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
