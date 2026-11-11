using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Screens;

/// <summary>Proves per-frontend records unions and the screen rename.</summary>
public sealed class ScreenRenameAndRecordsTests
{
    /// <summary>Records Unions Hold Only The Frontends Own Entry Points.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void RecordsUnionsHoldOnlyTheFrontendsOwnEntryPoints()
    {
        using var temp = TestCli.Temp("screen-records");
        var model = ModelWithTwoFrontends(temp.Path);

        var web = model.Config.Frontends.Single(frontend => frontend.Name == "web");
        var admin = model.Config.Frontends.Single(frontend => frontend.Name == "admin");

        Assert.Equal(["use-case/lending.list-loans"], FrontendRecords.UseCaseIds(model, web));
        Assert.Equal(["screen/app.loans"], FrontendRecords.ScreenIds(model, web));
        Assert.Equal(["use-case/lending.borrow-copy"], FrontendRecords.UseCaseIds(model, admin));
        Assert.Equal(["screen/admin.loans"], FrontendRecords.ScreenIds(model, admin));
    }

    /// <summary>A Frontend Naming Its Entry Point Is Read Through It Before Any Screen Exists.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void AFrontendNamingItsEntryPointIsReadThroughItBeforeAnyScreenExists()
    {
        using var temp = TestCli.Temp("screen-entry-point");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "admin", "path": "apps/admin", "platform": "react", "entryPoint": "entry-point/admin" }
                ], "paths": {
              }
            }
            """);
        var model = new ProjectModel { Config = ConsumerConfig.Load(temp.Path) };
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser"}""", temp.Path));
        model.Records.Add(Rec("entry-point", "entry-point/admin", """{"kind":"browser"}""", temp.Path));

        var admin = model.Config.Frontends.Single();

        Assert.Equal(["entry-point/admin"], FrontendRecords.EntryPointsOf(model, admin));
    }

    /// <summary>Rename Screen Rewrites Its Record Route Mark And Browser Annotation.</summary>
    /// <proves>litespec/core/maintaining#replace-every-citation-on-a-rename</proves>
    [Fact]
    public void RenameScreenRewritesItsRecordRouteMarkAndBrowserAnnotation()
    {
        using var fixture = ModelFixture.Prepare("large");
        var root = fixture.Path;
        var screens = Path.Combine(root, ".litespec", "behavior", "entry-points", "library-app", "screens");

        var route = Path.Combine(root, "apps", "catalog", "src", "routes", "loans", "+page.svelte");

        var spec = Path.Combine(root, "apps", "catalog", "tests", "loans.spec.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(spec)!);
        File.WriteAllText(spec, "test('loans', proves(\"scenario/list-loans.listing-succeeds\"), async () => {});\n");

        var result = TestCli.Run("rename", "screen/library-app.loans", "screen/library-app.loan-list", "--root", root);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(screens, "library-app.loan-list.yaml")));
        Assert.False(File.Exists(Path.Combine(screens, "library-app.loans.yaml")));
        Assert.Contains("screen/library-app.loan-list", File.ReadAllText(route));
        Assert.Contains("screen/library-app.loan-list", File.ReadAllText(Path.Combine(screens, "library-app.loan-list.yaml")));
    }

    /// <summary>Rename Scenario Rewrites A Browser Tests Proves Call.</summary>
    /// <proves>litespec/core/maintaining#replace-every-citation-on-a-rename</proves>
    [Fact]
    public void RenameScenarioRewritesABrowserTestsProvesCall()
    {
        using var fixture = ModelFixture.Prepare("large");
        var root = fixture.Path;
        var spec = Path.Combine(root, "apps", "catalog", "tests", "loans.spec.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(spec)!);
        File.WriteAllText(spec, "test('loans', proves(\"scenario/list-loans.listing-own-loans-succeeds\"), async () => {});\n");

        var result = TestCli.Run("rename", "scenario/list-loans.listing-own-loans-succeeds", "scenario/list-loans.listing-all-loans-succeeds", "--root", root);

        Assert.Equal(0, result.Status);
        Assert.Contains("scenario/list-loans.listing-all-loans-succeeds", File.ReadAllText(spec));
    }

    static ProjectModel ModelWithTwoFrontends(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" },
                  { "name": "admin", "path": "apps/admin", "platform": "react" }
                ], "paths": {
              }
            }
            """);

        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser"}""", root));
        model.Records.Add(Rec("entry-point", "entry-point/admin", """{"kind":"browser"}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","reachedThrough":["entry-point/app"]}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.borrow-copy", """{"kind":"command","reachedThrough":["entry-point/admin"]}""", root));
        model.Records.Add(ScreenRec("screen/app.loans", "entry-point/app", root));
        model.Records.Add(ScreenRec("screen/admin.loans", "entry-point/admin", root));
        model.Screens.Add(new CodeScreen("web", "/loans", "apps/web/src/routes/loans/+page.ts", [], ["use-case/lending.list-loans"], "screen/app.loans"));
        model.Screens.Add(new CodeScreen("admin", "/loans", "apps/admin/src/routes/loans.tsx", [], ["use-case/lending.borrow-copy"], "screen/admin.loans"));

        return model;
    }

    static Record ScreenRec(string id, string entryPoint, string root) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            File = Path.Combine(root, "screen.md"),
            Line = 1,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":" + JsonSerializer.Serialize(entryPoint) + ",\"address\":\"/loans\",\"offers\":[]}").RootElement.Clone(),
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
