using Record = LiteSpec.Model.Record;
using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Screens;

/// <summary>Proves per-frontend records unions and the screen rename.</summary>
public sealed class ScreenRenameAndRecordsTests
{
    /// <summary>Records Unions Hold Only The Frontends Own Entry Points.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void Records_Unions_Hold_Only_The_Frontends_Own_Entry_Points()
    {
        using var temp = Cli.Temp("screen-records");
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
    public void A_Frontend_Naming_Its_Entry_Point_Is_Read_Through_It_Before_Any_Screen_Exists()
    {
        using var temp = Cli.Temp("screen-entry-point");
        Directory.CreateDirectory(Path.Combine(temp.Path, "docs"));
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """
            {
              "owners": ["lending-team"],
              "paths": {
                "frontends": [
                  { "name": "admin", "path": "apps/admin", "platform": "react", "entryPoint": "entry-point/admin" }
                ]
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
    public void Rename_Screen_Rewrites_Its_Record_Route_Mark_And_Browser_Annotation()
    {
        using var fixture = ModelFixture.Prepare("large");
        var root = fixture.Path;
        var screens = Path.Combine(root, "docs", "behavior", "screens");
        Directory.CreateDirectory(screens);
        File.WriteAllText(Path.Combine(screens, "library-app.loans.md"), """
            ---
            {
              "type": "screen",
              "id": "screen/library-app.loans",
              "title": "Loans",
              "owner": "lending-team",
              "status": "reviewed",
              "entryPoint": "entry-point/library-app",
              "address": "/loans",
              "offers": ["use-case/lending.list-loans"]
            }
            ---
            # Loans
            ## Summary
            An employee sees their loans.
            """);

        var route = Path.Combine(root, "apps", "catalog", "src", "routes", "loans", "+page.svelte");

        var spec = Path.Combine(root, "apps", "catalog", "tests", "loans.spec.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(spec)!);
        File.WriteAllText(spec, "test('loans', proves(\"scenario/list-loans.listing-succeeds\"), async () => {});\n");

        var result = Cli.Run("rename", "screen/library-app.loans", "screen/library-app.loan-list", "--root", root);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(screens, "library-app.loan-list.md")));
        Assert.False(File.Exists(Path.Combine(screens, "library-app.loans.md")));
        Assert.Contains("screen/library-app.loan-list", File.ReadAllText(route));
        Assert.Contains("screen/library-app.loan-list", File.ReadAllText(Path.Combine(screens, "library-app.loan-list.md")));
    }

    /// <summary>Rename Scenario Rewrites A Browser Tests Proves Call.</summary>
    /// <proves>litespec/core/maintaining#replace-every-citation-on-a-rename</proves>
    [Fact]
    public void Rename_Scenario_Rewrites_A_Browser_Tests_Proves_Call()
    {
        using var fixture = ModelFixture.Prepare("large");
        var root = fixture.Path;
        var spec = Path.Combine(root, "apps", "catalog", "tests", "loans.spec.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(spec)!);
        File.WriteAllText(spec, "test('loans', proves(\"scenario/list-loans.listing-own-loans-succeeds\"), async () => {});\n");

        var result = Cli.Run("rename", "scenario/list-loans.listing-own-loans-succeeds", "scenario/list-loans.listing-all-loans-succeeds", "--root", root);

        Assert.Equal(0, result.Status);
        Assert.Contains("scenario/list-loans.listing-all-loans-succeeds", File.ReadAllText(spec));
    }

    static ProjectModel ModelWithTwoFrontends(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "docs"));
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {
              "owners": ["lending-team"],
              "paths": {
                "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" },
                  { "name": "admin", "path": "apps/admin", "platform": "react" }
                ]
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
        model.Screens.Add(new ScreenInfo("web", "/loans", "apps/web/src/routes/loans/+page.ts", [], ["use-case/lending.list-loans"], "screen/app.loans"));
        model.Screens.Add(new ScreenInfo("admin", "/loans", "apps/admin/src/routes/loans.tsx", [], ["use-case/lending.borrow-copy"], "screen/admin.loans"));

        return model;
    }

    static Record ScreenRec(string id, string entryPoint, string root) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            Owner = "lending-team",
            Status = "reviewed",
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
            Owner = "lending-team",
            Status = "reviewed",
            File = Path.Combine(root, $"{type}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
