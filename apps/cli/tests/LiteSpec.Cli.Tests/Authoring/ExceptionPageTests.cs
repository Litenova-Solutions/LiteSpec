using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Tests for <c>litespec new exception</c>.</summary>
public sealed class ExceptionPageTests
{
    /// <summary>New exception scaffolds the page under governance exceptions.</summary>
    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new exception scaffolds the page under governance exceptions")]
    public void NewExceptionScaffoldsItsPage()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = Cli.Run(
            "new", "exception", "payment-retries-later", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            fixture.Path, ".litespec", "governance", "exceptions", "payment-retries-later.yaml");

        Assert.True(File.Exists(page), $"Expected the exception page at '{page}'.");
        Assert.Contains("exception/payment-retries-later", File.ReadAllText(page));
    }

    /// <summary>New deviation is an unknown type.</summary>
    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new deviation is an unknown type")]
    public void NewDeviationIsUnknown()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = Cli.Run(
            "new", "deviation", "payment-retries-later", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown type 'deviation'.", result.Output);
    }

    /// <summary>New topic is an unknown type.</summary>
    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new topic is an unknown type")]
    public void NewTopicIsUnknown()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = Cli.Run(
            "new", "topic", "approval", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown type 'topic'.", result.Output);
    }

    /// <summary>New review report is an unknown type.</summary>
    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new review report is an unknown type")]
    public void NewReviewReportIsUnknown()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = Cli.Run(
            "new", "review-report", "october-review", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown type 'review-report'.", result.Output);
    }
}
