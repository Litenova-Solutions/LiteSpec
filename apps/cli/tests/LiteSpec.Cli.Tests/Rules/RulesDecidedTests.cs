using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> prints the Decided group first on a code path.</summary>
public sealed class RulesDecidedTests
{
    /// <summary>A renderer prints its followed decision with its choice and reopen condition, and no stage or audit-adoption provision.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRendererPrintsDecisionUnderDecided()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Application/ReceiptPdfRenderer.cs");

        Assert.Equal(0, result.Status);
        Assert.Equal("Decided:", OutputLines(result.Output)[1]);
        Assert.Contains(
            "- decision/0005-render-receipts-as-pdf: choice PDF library; reopened when Receipts stop needing a downloadable file, or the library stops fitting.",
            result.Output);
        Assert.DoesNotContain("promote-through-a-recorded-decision", result.Output);
        Assert.DoesNotContain("record-the-audit-adoption-decision", result.Output);
    }

    /// <summary>A covering gap departure prints under Decided with its reason and expiry date.</summary>
    /// <proves>litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure</proves>
    [Fact]
    public void RulesCoveringDeparturePrintsUnderDecided()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Equal("Decided:", OutputLines(result.Output)[1]);
        Assert.Contains(
            "- departure/slow-review-departure: The billing handler calls two aggregates until the split ships.; expires 2030-01-01",
            result.Output);
        Assert.DoesNotContain("old-layout-departure", result.Output);
    }

    /// <summary>A lasting departure prints under Decided with its reason and no expiry date.</summary>
    /// <proves>litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure</proves>
    [Fact]
    public void RulesLastingDeparturePrintsWithoutExpiry()
    {
        using var root = CopyFixture();
        AddFile(
            root,
            ".litespec/governance/departures/lasting-layout-choice.yaml",
            """
            type: departure
            id: departure/lasting-layout-choice
            title: Lasting Layout Choice
            covers:
              - litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case
            subject:
              - use-case/billing.send-invoice
            reason: The demo keeps one folder per use case forever.
            consequences: The handler folders never split.
            """);
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        var line = OutputLines(result.Output).First(line => line.Contains("departure/lasting-layout-choice", StringComparison.Ordinal));
        Assert.Contains("The demo keeps one folder per use case forever.", line);
        Assert.DoesNotContain("expires", line);
    }

    /// <summary>Followed decisions print in the order the file marks them.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesDecisionsPrintInMarkedOrder()
    {
        using var root = CopyFixture();
        AddFile(
            root,
            ".litespec/implementation/decisions/0001-first-choice.yaml",
            """
            type: decision
            id: decision/0001-first-choice
            title: First Choice
            choice: First
            reopenedWhen: The first stops fitting.
            """);
        AddFile(
            root,
            ".litespec/implementation/decisions/0002-second-choice.yaml",
            """
            type: decision
            id: decision/0002-second-choice
            title: Second Choice
            choice: Second
            reopenedWhen: The second stops fitting.
            """);
        AddFile(
            root,
            "apps/api/src/Demo/Billing/Invoicing/Render/RenderService.cs",
            """
            namespace Demo.Billing.Invoicing.Render;

            /// <summary>Renders the invoice from both choices.</summary>
            /// <follows>decision/0002-second-choice</follows>
            /// <follows>decision/0001-first-choice</follows>
            internal sealed class RenderService;
            """);
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/Render/RenderService.cs");

        Assert.Equal(0, result.Status);
        var lines = OutputLines(result.Output);
        var second = lines.FindIndex(line => line.Contains("decision/0002-second-choice", StringComparison.Ordinal));
        var first = lines.FindIndex(line => line.Contains("decision/0001-first-choice", StringComparison.Ordinal));
        Assert.True(second >= 0 && first > second, "The decisions print in the order the file marks them.");
        Assert.Contains("- decision/0002-second-choice: choice Second; reopened when The second stops fitting.", result.Output);
    }

    /// <summary>A mark naming no decision record prints no Decided line and fails nothing.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesUnknownDecisionMarkPrintsNoDecidedLine()
    {
        using var root = CopyFixture();
        AddFile(
            root,
            "apps/api/src/Demo/Billing/Invoicing/Render/UnknownRenderService.cs",
            """
            namespace Demo.Billing.Invoicing.Render;

            /// <summary>Renders the invoice from a missing choice.</summary>
            /// <follows>decision/9999-no-such-choice</follows>
            internal sealed class UnknownRenderService;
            """);
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/Render/UnknownRenderService.cs");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("Decided:", result.Output);
        Assert.DoesNotContain("- decision/9999-no-such-choice", result.Output);
    }

    /// <summary>A record identifier prints no Decided group.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRecordIdPrintsNoDecided()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "use-case/spending.submit-expense");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("Decided:", result.Output);
    }

    /// <summary>Full output prints the Decided group first too.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesFullPrintsDecidedFirst()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Application/ReceiptPdfRenderer.cs",
            "--full");

        Assert.Equal(0, result.Status);
        Assert.Equal("Decided:", OutputLines(result.Output)[1]);
        Assert.Contains("- decision/0005-render-receipts-as-pdf: choice PDF library;", result.Output);
    }

    /// <summary>The non-empty output lines, trimmed.</summary>
    /// <param name="output">The command output.</param>
    /// <returns>The non-empty trimmed lines.</returns>
    static List<string> OutputLines(string output) =>
        output.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();

    /// <summary>Copies the rules-basic fixture to a temporary root.</summary>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyFixture()
    {
        var source = TestCli.Fixture("rules-basic");
        var temp = TestCli.Temp("rules-decided");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        return temp;
    }

    /// <summary>Adds one file to a temporary consumer root.</summary>
    /// <param name="root">The temporary consumer root.</param>
    /// <param name="relative">The file path, with forward-slash segments.</param>
    /// <param name="contents">The file contents.</param>
    static void AddFile(TempFolder root, string relative, string contents)
    {
        var file = Path.Combine(root.Path, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, contents);
    }
}
