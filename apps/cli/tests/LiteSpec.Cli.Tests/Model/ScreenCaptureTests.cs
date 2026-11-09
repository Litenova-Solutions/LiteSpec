using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the builder records each screen's captures from the gallery.</summary>
/// <follows>litespec/core/model#record-each-screens-captures-from-the-gallery</follows>
public sealed class ScreenCaptureTests
{
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
              "conformance": "full",
              "reviewedLiteSpecVersion": "4.1.0",
              "screenshots": { "galleries": ["gallery"], "pattern": "{route}--{theme}--{width}.png" }
            }
            """);

        var config = ConsumerConfig.Load(temp.Path);
        var model = TestModel.Model(
            config,
            TestModel.Record("screen", "screen/borrowing.list", Path.Combine(temp.Path, "list.yaml"), """{"address": "/loans", "entryPoint": "browser"}"""));

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
            TestModel.Record("screen", "screen/borrowing.list", Path.Combine(temp.Path, "list.yaml"), """{"address": "/loans", "entryPoint": "browser"}"""),
            TestModel.Record("screen", "screen/borrowing.overlay", Path.Combine(temp.Path, "overlay.yaml"), """{"host": "screen/borrowing.list"}"""));

        ScreenshotCaptures.Compute(model);

        Assert.Equal("route-not-captured", model.CaptureState["screen/borrowing.list"]);
        Assert.Empty(model.Captures["screen/borrowing.list"]);
        Assert.Equal("no-route", model.CaptureState["screen/borrowing.overlay"]);
    }
}
