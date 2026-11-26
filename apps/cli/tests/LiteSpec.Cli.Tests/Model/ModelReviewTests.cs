using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Consumer;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model review</c> lists the level R findings the check sets aside, never blocks, and omits every blocking finding.</summary>
public sealed class ModelReviewTests
{
    /// <summary>Review lists each level R finding with its counts and totals, and exits zero.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void ReviewListsReviewFindingsAndExitsZero()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            ".litespec/implementation/add-a-pack.md",
            raw => raw.Replace("## Check", "## Notes", StringComparison.Ordinal));

        var result = TestCli.Run("model", "review", "--root", folder.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("[R] [page-section-missing]", result.Output, StringComparison.Ordinal);
        Assert.Contains("requires an H2 'Check'", result.Output, StringComparison.Ordinal);
        Assert.Contains("Findings by code:", result.Output, StringComparison.Ordinal);
        Assert.Contains("page-section-missing (level R)", result.Output, StringComparison.Ordinal);
        Assert.Contains("0 blocking, 0 covered.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Review omits every blocking finding, which the check reports instead.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void ReviewOmitsBlockingFindings()
    {
        using var fixture = ModelFixture.Prepare("large", "paging-undeclared");

        var reviewed = TestCli.Run("model", "review", "--root", fixture.Path);

        Assert.Equal(0, reviewed.Status);
        Assert.DoesNotContain("[paging-undeclared]", reviewed.Output, StringComparison.Ordinal);

        var check = TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("[paging-undeclared]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Check omits every level R finding, which review reports instead.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void CheckOmitsReviewFindings()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            ".litespec/implementation/add-a-pack.md",
            raw => raw.Replace("## Check", "## Notes", StringComparison.Ordinal));

        var check = TestCli.Run("model", "check", "--root", folder.Path);

        Assert.DoesNotContain("[page-section-missing]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("page-section-missing (level R)", check.Output, StringComparison.Ordinal);

        var reviewed = TestCli.Run("model", "review", "--root", folder.Path);

        Assert.Contains("[page-section-missing]", reviewed.Output, StringComparison.Ordinal);
    }

    /// <summary>Review with json writes the findings and covered arrays in the check shape.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void ReviewJsonWritesFindingsInTheCheckShape()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            ".litespec/implementation/add-a-pack.md",
            raw => raw.Replace("## Check", "## Notes", StringComparison.Ordinal));

        var result = TestCli.Run("model", "review", "--root", folder.Path, "--format", "json");

        Assert.Equal(0, result.Status);
        var parsed = (JsonObject)JsonNode.Parse(result.Stdout)!;
        var findings = parsed["findings"]!.AsArray()
            .Select(finding => (JsonObject)finding!)
            .Where(finding => (string?)finding["code"] == "page-section-missing")
            .ToList();
        Assert.NotEmpty(findings);
        Assert.All(findings, finding =>
        {
            Assert.Equal("R", (string?)finding["level"]);
            Assert.Equal("reported", (string?)finding["outcome"]);
        });
        Assert.NotNull(parsed["covered"]);
    }

    /// <summary>Review names itself in the usage text and rejects an option it does not take.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void ReviewUsageNamesReviewAndRejectsUnknownOptions()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "review", "--root", fixture.Path, "--all");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--all'.", result.Output, StringComparison.Ordinal);
        Assert.Contains("litespec model review", LiteSpec.Cli.Model.ModelCommand.Usage, StringComparison.Ordinal);
    }
}
