using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>
///     Proves the builder reads React routes: each file under <c>src/routes</c> that declares
///     its address with <c>createFileRoute</c> is a screen, at the address a reader of the page sees.
/// </summary>
public sealed class ReactScreenTests
{
    /// <summary>React Route Files Become Screens At Their Visible Address.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void ReactRouteFilesBecomeScreensAtTheirVisibleAddress()
    {
        using var temp = ModelFixture.Prepare("large");
        AddReactFrontend(temp.Path);

        var screens = BuildScreens(temp.Path).Where(screen => screen.Frontend == "console").ToList();

        Assert.Equal(
            ["/loans", "/loans/$loanId"],
            screens.Select(screen => screen.Route).OrderBy(route => route, StringComparer.Ordinal).ToList());
        Assert.Contains(screens, screen => screen.File == "apps/console/src/routes/_console/loans/index.tsx");
    }

    /// <summary>Root Lazy And Ignored Route Files Declare No Screen Of Their Own.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void RootLazyAndIgnoredRouteFilesDeclareNoScreenOfTheirOwn()
    {
        using var temp = ModelFixture.Prepare("large");
        AddReactFrontend(temp.Path);

        var files = BuildScreens(temp.Path)
            .Where(screen => screen.Frontend == "console")
            .Select(screen => screen.File)
            .ToList();

        Assert.DoesNotContain("apps/console/src/routes/__root.tsx", files);
        Assert.DoesNotContain("apps/console/src/routes/_console/loans/index.lazy.tsx", files);
        Assert.DoesNotContain("apps/console/src/routes/-components/LoanRow.tsx", files);
    }

    static void AddReactFrontend(string root)
    {
        var routes = Path.Combine(root, "apps", "console", "src", "routes");
        Write(routes, "__root.tsx", "export const Route = createRootRoute({});\n");
        Write(routes, "_console/loans/index.tsx", "export const Route = createFileRoute(\"/_console/loans/\")({});\n");
        Write(routes, "_console/loans/index.lazy.tsx", "export const Route = createLazyFileRoute(\"/_console/loans/\")({});\n");
        Write(routes, "_console/loans/$loanId.tsx", "export const Route = createFileRoute(\"/_console/loans/$loanId\")({});\n");
        Write(routes, "-components/LoanRow.tsx", "export function LoanRow() { return null; }\n");

        var projectFile = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(projectFile)!.AsObject();
        project["frontends"]!.AsArray().Add(new JsonObject { ["name"] = "console", ["path"] = "apps/console", ["platform"] = "react" });
        File.WriteAllText(projectFile, project.ToJsonString());
    }

    static List<ScreenInfo> BuildScreens(string root) =>
        ModelBuilder.Build(ConsumerConfig.Load(root)).Screens.ToList();

    static void Write(string folder, string relative, string text)
    {
        var path = Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
