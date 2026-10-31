using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Tests for <c>litespec new exception</c>.</summary>
public sealed class ExceptionPageTests
{
    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new exception scaffolds the page under governance exceptions")]
    public void NewExceptionScaffoldsItsPage()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = Cli.Run(
            "new", "exception", "payment-retries-later", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            fixture.Path, "docs", "governance", "exceptions", "payment-retries-later.md");

        Assert.True(File.Exists(page), $"Expected the exception page at '{page}'.");
        Assert.Contains("exception/payment-retries-later", File.ReadAllText(page));
    }

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
}
