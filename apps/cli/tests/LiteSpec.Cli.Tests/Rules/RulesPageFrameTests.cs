using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> prints the Page frame block on a route file.</summary>
public sealed class RulesPageFrameTests
{
    /// <summary>A route file serving a screen prints the screen's page-frame pattern in full with its task, primary action and offers.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRouteFileWithScreenPrintsPageFrameInFull()
    {
        using var root = CopyFixtureWithScreen(
            ".litespec/behavior/screens/invoice-list.yaml",
            """
            type: screen
            id: screen/billing-app.invoice-list
            title: Invoice List
            route: /invoices
            task: The clerk scans the invoices for one to open.
            pageFrame:
              pattern: list-page
              option: search-and-a-state-filter-only
            offers:
            - use-case/billing.send-invoice
            primaryAction: use-case/billing.send-invoice
            displayStates:
            - name: Empty
              shows: The list says it is empty.
            """);
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/web/src/routes/invoices/+page.svelte");

        Assert.Equal(0, result.Status);
        Assert.Contains("Page frame: litespec/frontend/patterns/page-frames#list-page (List Page)", result.Output);
        Assert.Contains("Screen: screen/billing-app.invoice-list", result.Output);
        Assert.Contains("Question: How to find one record among many.", result.Output);
        Assert.Contains("Default: search-and-a-state-filter-only", result.Output);
        Assert.Contains("Chosen option: search-and-a-state-filter-only", result.Output);
        Assert.Contains("Wireframe (search-and-a-state-filter-only):", result.Output);
        Assert.Contains("field \"Expense number, name or email\" search", result.Output);
        Assert.Contains("Task: The clerk scans the invoices for one to open.", result.Output);
        Assert.Contains("Primary action: use-case/billing.send-invoice", result.Output);
        Assert.Contains("Offers: use-case/billing.send-invoice", result.Output);
    }

    /// <summary>A route file no screen serves prints one line saying so.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRouteFileWithoutScreenPrintsOneLine()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/web/src/routes/invoices/+page.svelte");

        Assert.Equal(0, result.Status);
        Assert.Contains("No screen record serves this route.", result.Output);
        Assert.DoesNotContain("Page frame:", result.Output);
    }

    /// <summary>A standard with artifact route prints as a statement on a route file.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRouteFilePrintsRouteArtifactStandardsAsStatements()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/web/src/routes/invoices/+page.svelte");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "- litespec/frontend/structure#keep-a-route-entry-thin: A route entry MUST hold its input, its load, and its component, with no reusable logic and no reference to another route entry. [path match (artifact route)]",
            result.Output);
    }

    /// <summary>Copies the rules-basic fixture to a temporary root and adds one screen record.</summary>
    /// <param name="relative">The screen file path, with forward-slash segments.</param>
    /// <param name="contents">The screen file contents.</param>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyFixtureWithScreen(string relative, string contents)
    {
        var source = TestCli.Fixture("rules-basic");
        var temp = TestCli.Temp("rules-pageframe");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        var added = Path.Combine(temp.Path, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(added)!);
        File.WriteAllText(added, contents);
        return temp;
    }
}
