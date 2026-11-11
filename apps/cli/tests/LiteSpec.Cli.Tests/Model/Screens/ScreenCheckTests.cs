using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Screens;

/// <summary>Proves the screen checks over hand-built models: each rule with a passing and a failing case.</summary>
public sealed class ScreenCheckTests
{
    /// <summary>Screen With No Route Reports Screen Unimplemented.</summary>
    /// <proves>litespec/core/edges#give-every-page-its-screen-record</proves>
    [Fact]
    public void ScreenWithNoRouteReportsScreenUnimplemented()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));

        Assert.Single(WithCode(model, "screen-unimplemented"));
    }

    /// <summary>Screen With A Marked Route Reports No Screen Unimplemented.</summary>
    /// <proves>litespec/core/edges#give-every-page-its-screen-record</proves>
    [Fact]
    public void ScreenWithAMarkedRouteReportsNoScreenUnimplemented()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Empty(WithCode(model, "screen-unimplemented"));
        Assert.Empty(WithCode(model, "behavior-unrecorded"));
    }

    /// <summary>Route With No Mark Reports Behavior Unrecorded.</summary>
    /// <proves>litespec/core/edges#give-every-page-its-screen-record</proves>
    [Fact]
    public void RouteWithNoMarkReportsBehaviorUnrecorded()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Code.Elements.Add(RouteElement(null, "/loans", temp.Path));

        var finding = Assert.Single(WithCode(model, "behavior-unrecorded"), found => found.Message.Contains("no screen mark"));
        Assert.Equal(Finding.TypeScriptReader, finding.Reader);
    }

    /// <summary>Route Marking No Screen Reports Behavior Unrecorded.</summary>
    /// <proves>litespec/core/edges#give-every-page-its-screen-record</proves>
    [Fact]
    public void RouteMarkingNoScreenReportsBehaviorUnrecorded()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Code.Elements.Add(RouteElement("screen/app.ghost", "/loans", temp.Path));

        Assert.Single(WithCode(model, "behavior-unrecorded"), found => found.Message.Contains("names no screen"));
    }

    /// <summary>Offers Differing From Calls Report Screen Calls Mismatch.</summary>
    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void OffersDifferingFromCallsReportScreenCallsMismatch()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.borrow-copy"]);

        var finding = Assert.Single(WithCode(model, "screen-offers-drift"));
        Assert.Contains("use-case/lending.list-loans", finding.Message);
        Assert.Contains("use-case/lending.borrow-copy", finding.Message);
    }

    /// <summary>Offers Equal To Calls Report No Screen Calls Mismatch.</summary>
    /// <proves>litespec/core/edges#match-a-screens-offers-to-its-calls</proves>
    [Fact]
    public void OffersEqualToCallsReportNoScreenCallsMismatch()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Empty(WithCode(model, "screen-offers-drift"));
    }

    /// <summary>Screen Serving A Step Its Offers Do Not Serve Reports Mismatch.</summary>
    /// <proves>litespec/core/edges#serve-only-the-steps-a-screens-use-cases-serve</proves>
    [Fact]
    public void ScreenServingAStepItsOffersDoNotServeReportsMismatch()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], ["journey/walk#s2"]));

        Assert.Single(WithCode(model, "screen-serves-drift"));
    }

    /// <summary>Screen Serving A Step Its Offers Serve Reports No Mismatch.</summary>
    /// <proves>litespec/core/edges#serve-only-the-steps-a-screens-use-cases-serve</proves>
    [Fact]
    public void ScreenServingAStepItsOffersServeReportsNoMismatch()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], ["journey/walk#s1"]));

        Assert.Empty(WithCode(model, "screen-serves-drift"));
    }

    /// <summary>Browser Step With No Screen Reports Step Unscreened.</summary>
    /// <proves>litespec/core/edges#give-every-browser-step-a-screen</proves>
    [Fact]
    public void BrowserStepWithNoScreenReportsStepUnscreened()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);

        Assert.Single(WithCode(model, "step-screen-missing"));
    }

    /// <summary>Browser Step With A Screen Reports No Step Unscreened.</summary>
    /// <proves>litespec/core/edges#give-every-browser-step-a-screen</proves>
    [Fact]
    public void BrowserStepWithAScreenReportsNoStepUnscreened()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], ["journey/walk#s1"]));

        Assert.Empty(WithCode(model, "step-screen-missing"));
    }

    /// <summary>Reachable Refusal With No Sentence Or Assert Reports Result Unshown.</summary>
    /// <proves>litespec/core/edges#show-every-refusal-a-screen-can-reach</proves>
    [Fact]
    public void ReachableRefusalWithNoSentenceOrAssertReportsResultUnshown()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Single(WithCode(model, "refusal-sentence-missing"));
    }

    /// <summary>Reachable Refusal With A Dictionary Sentence Reports No Result Unshown.</summary>
    /// <proves>litespec/core/edges#show-every-refusal-a-screen-can-reach</proves>
    [Fact]
    public void ReachableRefusalWithADictionarySentenceReportsNoResultUnshown()
    {
        using var temp = TestCli.Temp("screens");
        WriteProject(temp.Path, "{ \"lending.refused\": \"That copy is not available.\" }");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        Assert.Empty(WithCode(model, "refusal-sentence-missing"));
    }

    /// <summary>Reachable Refusal With An Asserting Browser Test Reports No Result Unshown.</summary>
    /// <proves>litespec/core/edges#show-every-refusal-a-screen-can-reach</proves>
    [Fact]
    public void ReachableRefusalWithAnAssertingBrowserTestReportsNoResultUnshown()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "shows the refusal", Path.Combine(temp.Path, "tests", "loans.spec.ts"), 3,
            [], null, [], ["lending.refused"], []));

        Assert.Empty(WithCode(model, "refusal-sentence-missing"));
    }

    /// <summary>Scenario Proved By Two Browser Tests Reports Proves Duplicated.</summary>
    /// <proves>litespec/frontend/testing#prove-each-scenario-at-one-boundary</proves>
    [Fact]
    public void ScenarioProvedByTwoBrowserTestsReportsProvesDuplicated()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(BrowserProof(temp.Path, "first", "/loans"));
        model.BrowserTests.Add(BrowserProof(temp.Path, "second", "/loans"));

        Assert.Single(WithCode(model, "proves-duplicate"));
    }

    /// <summary>Scenario Proved By One Browser Test Reports No Proves Duplicated.</summary>
    /// <proves>litespec/frontend/testing#prove-each-scenario-at-one-boundary</proves>
    [Fact]
    public void ScenarioProvedByOneBrowserTestReportsNoProvesDuplicated()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(BrowserProof(temp.Path, "only", "/loans"));

        Assert.Empty(WithCode(model, "proves-duplicate"));
    }

    /// <summary>Browser Test Reaching No Offering Screen Reports Proves Unconfirmed.</summary>
    /// <proves>litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case</proves>
    [Fact]
    public void BrowserTestReachingNoOfferingScreenReportsProvesUnconfirmed()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        model.BrowserTests.Add(BrowserProof(temp.Path, "stray", "/elsewhere"));

        Assert.Single(WithCode(model, "proves-unreached"));
    }

    /// <summary>Browser Test Reaching The Screen And Asserting Text Reports No Proves Unconfirmed.</summary>
    /// <proves>litespec/frontend/testing#assert-the-result-a-scenario-ends-in</proves>
    [Fact]
    public void BrowserTestReachingTheScreenAndAssertingTextReportsNoProvesUnconfirmed()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        model.BrowserTests.Add(BrowserProof(temp.Path, "proof", "/loans"));

        Assert.Empty(WithCode(model, "proves-unreached"));
    }

    /// <summary>Route Suite Test With No Proves Reports Mark Missing.</summary>
    /// <proves>litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove</proves>
    [Fact]
    public void RouteSuiteTestWithNoProvesReportsMarkMissing()
    {
        using var temp = TestCli.Temp("screens");
        WriteRouteSuiteProject(temp.Path);
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "every route", Path.Combine(temp.Path, "apps", "web", "tests", "routes", "every-route.spec.ts"), 3,
            [], null, [], [], []));

        Assert.Single(WithCode(model, "mark-missing"), found => found.Message.Contains("route suite"));
    }

    /// <summary>Route Suite Test Proving A Provision Reports No Mark Finding.</summary>
    /// <proves>litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove</proves>
    [Fact]
    public void RouteSuiteTestProvingAProvisionReportsNoMarkFinding()
    {
        using var temp = TestCli.Temp("screens");
        WriteRouteSuiteProject(temp.Path);
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "every route", Path.Combine(temp.Path, "apps", "web", "tests", "routes", "every-route.spec.ts"), 3,
            ["litespec/frontend/ui#check-every-route"], null, [], [], []));

        Assert.Empty(WithCode(model, "mark-missing"));
        Assert.Empty(WithCode(model, "mark-unknown"));
    }

    /// <summary>Route Suite Test Proving A Scenario Reports Mark Unresolved.</summary>
    /// <proves>litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove</proves>
    [Fact]
    public void RouteSuiteTestProvingAScenarioReportsMarkUnresolved()
    {
        using var temp = TestCli.Temp("screens");
        WriteRouteSuiteProject(temp.Path);
        var model = BaseModel(temp.Path);
        model.BrowserTests.Add(new BrowserTestInfo(
            "web", "every route", Path.Combine(temp.Path, "apps", "web", "tests", "routes", "every-route.spec.ts"), 3,
            ["scenario/list-loans.listing-succeeds"], null, [], [], []));

        Assert.Single(WithCode(model, "mark-unknown"), found => found.Message.Contains("which is a scenario"));
    }

    static void WriteRouteSuiteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web", "tests", "routes"));
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte", "routeSuite": "apps/web/tests/routes" }
                ], "paths": {
              }
            }
            """);
    }

    /// <summary>Module Mark Outside The Frontends Entry Points Reports Mark Unresolved.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void ModuleMarkOutsideTheFrontendsEntryPointsReportsMarkUnresolved()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Rec("entry-point", "entry-point/admin", """{"kind":"browser"}""", temp.Path));
        model.Records.Add(Rec("use-case", "use-case/lending.other", """{"kind":"query","reachedThrough":["entry-point/admin"]}""", temp.Path));
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        var file = Path.Combine(temp.Path, "apps", "web", "src", "lib", "modules", "lending", "loans", "other", "litespec.ts");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, "lending/loans/other", null, file, 3,
            [new CodeMark("use-case/lending.other", MarkStyle.Xml, MarkVerb.Implements, file, 3)], InTests: false));

        Assert.Single(WithCode(model, "mark-unknown"), found => found.Message.Contains("outside frontend 'web's entry points"));
    }

    /// <summary>Module Mark Inside The Frontends Entry Points Reports No Scope Finding.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void ModuleMarkInsideTheFrontendsEntryPointsReportsNoScopeFinding()
    {
        using var temp = TestCli.Temp("screens");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        AddRoute(model, "screen/app.loans", "/loans", ["use-case/lending.list-loans"]);

        var file = Path.Combine(temp.Path, "apps", "web", "src", "lib", "modules", "lending", "loans", "list-loans", "litespec.ts");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, "lending/loans/list-loans", null, file, 3,
            [new CodeMark("use-case/lending.list-loans", MarkStyle.Xml, MarkVerb.Implements, file, 3)], InTests: false));

        Assert.Empty(WithCode(model, "mark-unknown"));
    }

    /// <summary>Address Matching Treats Parameters As Any Segment.</summary>
    /// <proves>litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case</proves>
    [Fact]
    public void AddressMatchingTreatsParametersAsAnySegment()
    {
        Assert.True(LiteSpec.Cli.Model.Readers.FrontendFactsRules.AddressMatches("/loans/[id]", "/loans/42"));
        Assert.True(LiteSpec.Cli.Model.Readers.FrontendFactsRules.AddressMatches("/loans/{}", "/loans/42"));
        Assert.False(LiteSpec.Cli.Model.Readers.FrontendFactsRules.AddressMatches("/loans/[id]", "/loans/42/edit"));
        Assert.False(LiteSpec.Cli.Model.Readers.FrontendFactsRules.AddressMatches("/loans", "/cards"));
    }

    /// <summary>Severity Uses The Producing Readers Qualification.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void SeverityUsesTheProducingReadersQualification()
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

        Assert.Equal(Outcome.Reported, Severity.Of(unqualified, unqualified.ReaderIsQualified, continuousIntegration: false).Outcome);
        Assert.Equal(Outcome.Blocking, Severity.Of(qualified, qualified.ReaderIsQualified, continuousIntegration: false).Outcome);
    }

    static List<Finding> WithCode(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(finding => finding.Code == code).ToList();

    static BrowserTestInfo BrowserProof(string root, string title, string target) =>
        new(
            "web", title, Path.Combine(root, "tests", $"{title}.spec.ts"), 3,
            ["scenario/list-loans.listing-succeeds"], null, [target], [], ["The loans are listed."]);

    static void AddRoute(ProjectModel model, string screenId, string address, IReadOnlyList<string> useCases)
    {
        var file = Path.Combine(model.Config.Root, "apps", "web", "src", "routes", "loans", "+page.svelte");

        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendRoute, address, null, file, 3,
            [new CodeMark(screenId, MarkStyle.Xml, MarkVerb.Implements, file, 3)], InTests: false));
        model.Code.Endpoints.Add(new EndpointOperation(
            "ListLoans", "use-case/lending.list-loans", "", "",
            Path.Combine(model.Config.Root, "ListLoansEndpoint.cs"), 3));
        model.Screens.Add(new ScreenInfo(
            "web", address, PathText.Relative(model.Config.Root, file),
            [new GeneratedOperation("listLoans", "", "")], useCases, screenId));
    }

    static MarkedElement RouteElement(string? screenId, string address, string root)
    {
        var file = Path.Combine(root, "apps", "web", "src", "routes", "loans", "+page.svelte");
        var marks = screenId is null
            ? new List<CodeMark>()
            : [new CodeMark(screenId, MarkStyle.Xml, MarkVerb.Implements, file, 3)];

        return new MarkedElement(CodeKind.FrontendRoute, address, null, file, 3, marks, InTests: false);
    }

    static void WriteProject(string root, string dictionary)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "web"));
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(Path.Combine(root, ".litespec", "refusals.json"), dictionary);
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte", "refusalDictionary": ".litespec/refusals.json" }
                ], "paths": {
              }
            }
            """);
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
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","reachedThrough":["entry-point/app"],"serves":["journey/walk#s1"]}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy", """{"kind":"command","reachedThrough":["entry-point/app"],"serves":["journey/walk#s1"]}""", root));
        model.Records.Add(Rec("journey", "journey/walk", """{"actor":["actor/employee"],"steps":[{"id":"s1","useCase":"use-case/lending.list-loans"},{"id":"s2","useCase":"use-case/lending.list-loans"}]}""", root));
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
            File = Path.Combine(root, $"{type}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
