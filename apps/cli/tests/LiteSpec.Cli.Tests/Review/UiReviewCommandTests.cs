using LiteSpec.Cli.Review;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Review;

/// <summary>Proves <c>litespec ui review</c> lists the changed routes and writes the review packet.</summary>
public sealed class UiReviewCommandTests
{
    /// <summary>A changed route without a baseline exits 1 and the packet holds every section with the geometry checklist.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ChangedRouteWithoutBaselineExits1AndPacketHoldsEverySection()
    {
        using var folder = TestCli.Temp("ui-review-missing");
        ReviewTestGit.Init(folder.Path);
        Directory.CreateDirectory(Path.Combine(folder.Path, "screens"));
        File.WriteAllText(
            Path.Combine(folder.Path, "screens", "expense-list.yaml"),
            "screen: spending.expense-list\nroute: /expenses\ntitle: Expense list\n");
        ReviewTestGit.CommitAll(folder.Path, "Add screen");
        File.AppendAllText(Path.Combine(folder.Path, "screens", "expense-list.yaml"), "summary: Every waiting claim.\n");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = UiReviewCommand.Execute(folder.Path, "HEAD", output, error, out var packet);

        Assert.Equal(1, exit);
        Assert.NotEqual("", packet);
        var text = File.ReadAllText(packet);
        Assert.Contains("## Changed Routes", text);
        Assert.Contains("/expenses", text);
        Assert.Contains("## Baselines", text);
        Assert.Contains("no baseline PNG", text);
        Assert.Contains("## Geometry Checklist", text);
        Assert.Contains("Edge alignment", text);
        Assert.Contains("## Diffs", text);
        Assert.Contains("/expenses", output.ToString());
    }

    /// <summary>A changed route with a committed baseline exits 0.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ChangedRouteWithBaselineExits0()
    {
        using var folder = TestCli.Temp("ui-review-baseline");
        ReviewTestGit.Init(folder.Path);
        Directory.CreateDirectory(Path.Combine(folder.Path, "screens"));
        File.WriteAllText(
            Path.Combine(folder.Path, "screens", "expense-list.yaml"),
            "screen: spending.expense-list\nroute: /expenses\ntitle: Expense list\n");
        var baselineFolder = Path.Combine(folder.Path, ".agents", "ui-baseline");
        Directory.CreateDirectory(baselineFolder);
        File.WriteAllText(Path.Combine(baselineFolder, "expenses--desktop.png"), "baseline");
        ReviewTestGit.CommitAll(folder.Path, "Add screen and baseline");
        File.AppendAllText(Path.Combine(folder.Path, "screens", "expense-list.yaml"), "summary: Every waiting claim.\n");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = UiReviewCommand.Execute(folder.Path, "HEAD", output, error, out var packet);

        Assert.Equal(0, exit);
        Assert.Contains("expenses--desktop.png", File.ReadAllText(packet));
    }

    /// <summary>An unknown revision exits 2 and writes no packet.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void UnknownRevisionExits2()
    {
        using var folder = TestCli.Temp("ui-review-unknown");
        ReviewTestGit.Init(folder.Path);
        File.WriteAllText(Path.Combine(folder.Path, "screen.yaml"), "route: /expenses\n");
        ReviewTestGit.CommitAll(folder.Path, "Add screen");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = UiReviewCommand.Execute(folder.Path, "no-such-ref", output, error, out var packet);

        Assert.Equal(2, exit);
        Assert.Equal("", packet);
    }
}
