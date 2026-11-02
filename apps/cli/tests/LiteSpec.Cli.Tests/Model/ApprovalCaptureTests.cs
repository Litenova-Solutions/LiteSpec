using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Model.Review;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the approval table names each record's approver and the builder records screen captures.</summary>
/// <follows>litespec/core/model#approve-each-risk-class-by-the-named-approver</follows>
/// <follows>litespec/core/model#record-each-screens-captures-from-the-gallery</follows>
public sealed class ApprovalCaptureTests
{
    /// <summary>The model writes the approval table and each record's approval from it.</summary>
    /// <proves>litespec/core/model#approve-each-risk-class-by-the-named-approver</proves>
    [Fact]
    public void Model_Writes_Approval_Table_And_Record_Approval()
    {
        using var fixture = ModelFixture.Prepare("small");

        WriteProject(fixture.Path, project =>
        {
            project["approvals"] = new JsonObject
            {
                ["money"] = "lending-team",
                ["personal-data"] = "second-team",
                ["default"] = "agent-batch",
            };
            ((JsonArray)project["owners"]!).Add("second-team");
        });

        ReviewPages.DraftPage(ReviewPages.Page(
            fixture.Path, ".litespec", "behavior", "modules", "lending", "loans", "extend-loan.yaml"));
        ReviewPages.AddPageRisk(ReviewPages.Page(
            fixture.Path, ".litespec", "behavior", "modules", "lending", "loans", "extend-loan.yaml"));

        var build = Cli.Run("model", "build", "--root", fixture.Path);

        Assert.True(build.Status == 0, build.Output);

        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(
            Path.Combine(fixture.Path, ".litespec/model", "model.json")));
        var root = document.RootElement;

        Assert.Equal("lending-team", root.GetProperty("approvals").GetProperty("money").GetString());
        Assert.Equal("agent-batch", root.GetProperty("approvals").GetProperty("default").GetString());

        var record = root.GetProperty("records").EnumerateArray()
            .First(entry => entry.GetProperty("id").GetString() == "use-case/lending.extend-loan");

        Assert.Equal("lending-team", record.GetProperty("approval").GetProperty("by").GetString());
        Assert.Contains("money", record.GetProperty("approval").GetProperty("rule").GetString(), StringComparison.Ordinal);

        var plain = root.GetProperty("records").EnumerateArray()
            .First(entry => entry.GetProperty("id").GetString() == "use-case/lending.borrow-copy");

        Assert.False(plain.TryGetProperty("approval", out _));
    }

    /// <summary>The approval for a class the table does not name falls back to the record owner.</summary>
    /// <proves>litespec/core/model#approve-each-risk-class-by-the-named-approver</proves>
    [Fact]
    public void Approval_For_Unnamed_Class_Falls_Back_To_Owner()
    {
        using var temp = Cli.Temp("approval");
        var config = TestModel.Config(temp.Path);
        var record = TestModel.Record(
            "requirement", "requirement/borrowing.max-loans",
            Path.Combine(temp.Path, "max.yaml"),
            """{"status": "approved", "owner": "lending-team", "risk": ["access"]}""");
        var model = TestModel.Model(config, record);

        var approval = ApprovalTable.ApprovalOf(model, record);

        Assert.NotNull(approval);
        Assert.Equal("lending-team", approval.By);
        Assert.Contains("access", approval.Rule, StringComparison.Ordinal);
        Assert.Null(ApprovalTable.ApprovalOf(model,
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"status": "approved"}""")));
    }

    /// <summary>The route slug reduces parameters and names the root route home.</summary>
    /// <proves>litespec/core/model#record-each-screens-captures-from-the-gallery</proves>
    [Fact]
    public void Route_Slug_Reduces_Parameters()
    {
        Assert.Equal("events-id", ScreenshotCaptures.RouteSlug("/events/[id]"));
        Assert.Equal("events-id", ScreenshotCaptures.RouteSlug("/events/{id:int}"));
        Assert.Equal("home", ScreenshotCaptures.RouteSlug("/"));
        Assert.Equal("settings-profile", ScreenshotCaptures.RouteSlug("settings/profile"));
    }

    /// <summary>A screen with a capture is captured, with its theme, width and capture date.</summary>
    /// <proves>litespec/core/model#record-each-screens-captures-from-the-gallery</proves>
    [Fact]
    public void Screen_Captures_Come_From_The_Gallery()
    {
        using var temp = Cli.Temp("captures");
        var gallery = Path.Combine(temp.Path, "gallery");
        Directory.CreateDirectory(gallery);
        var capture = Path.Combine(gallery, "loans--light--1440.png");
        File.WriteAllText(capture, "png");
        File.SetLastWriteTimeUtc(capture, new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc));
        File.WriteAllText(Path.Combine(temp.Path, "other--dark--390.png"), "png");

        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.json"), """
            {
              "project": { "name": "library" },
              "size": "small",
              "shape": "server",
              "packs": ["lite"],
              "stacks": ["dotnet"],
              "reviewedLiteSpecVersion": "3.1.0",
              "owners": ["lending-team"],
              "screenshots": { "galleries": ["gallery"], "pattern": "{route}--{theme}--{width}.png" }
            }
            """);

        var config = ConsumerConfig.Load(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("screen", "screen/borrowing.list", Path.Combine(temp.Path, "list.yaml"), """{"status": "approved", "address": "/loans", "entryPoint": "browser"}"""));

        ScreenshotCaptures.Compute(model);

        Assert.Equal("captured", model.CaptureState["screen/borrowing.list"]);
        var found = Assert.Single(model.Captures["screen/borrowing.list"]);

        Assert.Equal("gallery/loans--light--1440.png", found.Path);
        Assert.Equal("light", found.Theme);
        Assert.Equal(1440, found.Width);
        Assert.Equal("2026-11-01", found.CapturedOn);
    }

    /// <summary>A missing gallery folder captures nothing and fails nothing.</summary>
    /// <proves>litespec/core/model#record-each-screens-captures-from-the-gallery</proves>
    [Fact]
    public void Missing_Gallery_Captures_Nothing()
    {
        using var temp = Cli.Temp("captures");
        var config = TestModel.Config(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("screen", "screen/borrowing.list", Path.Combine(temp.Path, "list.yaml"), """{"status": "approved", "address": "/loans", "entryPoint": "browser"}"""),
            TestModel.Record("screen", "screen/borrowing.overlay", Path.Combine(temp.Path, "overlay.yaml"), """{"status": "approved", "host": "screen/borrowing.list"}"""));

        ScreenshotCaptures.Compute(model);

        Assert.Equal("route-not-captured", model.CaptureState["screen/borrowing.list"]);
        Assert.Empty(model.Captures["screen/borrowing.list"]);
        Assert.Equal("no-route", model.CaptureState["screen/borrowing.overlay"]);
    }

    /// <summary>Writes a change into the fixture project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="mutate">The change to apply to the project object.</param>
    static void WriteProject(string root, Action<JsonObject> mutate)
    {
        var path = Path.Combine(root, ".litespec/project.json");
        var project = (JsonObject)JsonNode.Parse(File.ReadAllText(path))!;

        mutate(project);
        File.WriteAllText(path, project.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
}
