using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec new departure</c>.</summary>
public sealed class DeparturePageTests
{
    /// <summary>New departure scaffolds the page under governance departures.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new departure scaffolds the page under governance departures")]
    public void NewDepartureScaffoldsItsPage()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "departure", "payment-retries-later", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            fixture.Path, ".litespec", "governance", "departures", "payment-retries-later.yaml");

        Assert.True(File.Exists(page), $"Expected the departure page at '{page}'.");
        Assert.Contains("departure/payment-retries-later", File.ReadAllText(page));
    }

    /// <summary>New exception is an unknown type, since the departure record replaces it.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new exception is an unknown type")]
    public void NewExceptionIsUnknown()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "exception", "payment-retries-later", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown type 'exception'.", result.Output);
    }

    /// <summary>New deviation is an unknown type.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new deviation is an unknown type")]
    public void NewDeviationIsUnknown()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "deviation", "payment-retries-later", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown type 'deviation'.", result.Output);
    }

    /// <summary>New topic is an unknown type.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new topic is an unknown type")]
    public void NewTopicIsUnknown()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "topic", "approval", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown type 'topic'.", result.Output);
    }

    /// <summary>New review report is an unknown type.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new review report is an unknown type")]
    public void NewReviewReportIsUnknown()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "review-report", "october-review", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown type 'review-report'.", result.Output);
    }
}
