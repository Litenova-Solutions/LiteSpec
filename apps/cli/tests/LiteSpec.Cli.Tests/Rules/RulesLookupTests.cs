using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> looks up one provision or page by its identifier.</summary>
public sealed class RulesLookupTests
{
    /// <summary>A provision identifier prints the provision in full.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesProvisionIdPrintsProvisionInFull()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "litespec/backend/application#keep-command-handlers-to-load-call-and-stage");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rules for litespec/backend/application#keep-command-handlers-to-load-call-and-stage", result.Output);
        Assert.Contains("Page: docs/backend/application.md", result.Output);
        Assert.Contains("Strength: required", result.Output);
        Assert.Contains("Statement: ", result.Output);
    }

    /// <summary>An unknown provision identifier exits 1.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesUnknownProvisionIdExits1()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "litespec/backend/application#no-such-provision");

        Assert.Equal(1, result.Status);
        Assert.Contains("No provision with ID 'litespec/backend/application#no-such-provision'", result.Output);
    }

    /// <summary>A page identifier prints the page's provision list.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesPageIdPrintsProvisionList()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "litespec/backend/application");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rules for litespec/backend/application (", result.Output);
        Assert.Contains("- litespec/backend/application#keep-command-handlers-to-load-call-and-stage:", result.Output);
    }

    /// <summary>An unknown page identifier exits 1.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesUnknownPageIdExits1()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "litespec/no-such-page");

        Assert.Equal(1, result.Status);
        Assert.Contains("No provisions on page 'litespec/no-such-page'", result.Output);
    }
}
