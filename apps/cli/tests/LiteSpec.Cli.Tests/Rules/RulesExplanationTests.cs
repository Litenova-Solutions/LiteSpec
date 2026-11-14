using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> lists the writing provisions for explanation files.</summary>
public sealed class RulesExplanationTests
{
    /// <summary>An explanation file lists the writing provisions with its record type as artifact.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesExplanationListsWritingProvisions()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            ".litespec/intent/problems/expenses-wait-weeks/expenses-wait-weeks.md");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "Rules for .litespec/intent/problems/expenses-wait-weeks/expenses-wait-weeks.md (concern: records, artifact: problem)",
            result.Output);
        Assert.Contains("litespec/core/writing#give-each-explanation-file-its-fixed-sections", result.Output);
        Assert.Contains("litespec/core/writing#write-for-the-reader-who-arrives-cold", result.Output);
    }

    /// <summary>A decision explanation lists the decision writing provision through its sibling record type.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesDecisionExplanationListsDecisionWritingProvision()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            ".litespec/intent/decisions/0001-receipt-threshold/0001-receipt-threshold.md");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "Rules for .litespec/intent/decisions/0001-receipt-threshold/0001-receipt-threshold.md (concern: records, artifact: decision)",
            result.Output);
        Assert.Contains("litespec/core/writing#compare-real-options-in-each-decision-record", result.Output);
    }
}
