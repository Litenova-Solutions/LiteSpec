using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Explore;

/// <summary>Tests for <c>litespec api explore</c> option parsing: usage, option errors, and the unreadable run.</summary>
public sealed class ExploreCommandTests
{
    /// <summary>Api --help prints the usage text and exits zero.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "api --help prints the usage text and exits zero")]
    public void ApiHelpPrintsUsage()
    {
        var result = TestCli.Run("api", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec api explore", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Api explore --help prints the usage text and exits zero without a running API.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "api explore --help prints the usage text and exits zero without a running API")]
    public void ExploreHelpPrintsUsage()
    {
        var result = TestCli.Run("api", "explore", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage: litespec api explore", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Api with no subcommand prints the usage text and exits 2.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "api with no subcommand prints the usage text and exits 2")]
    public void ApiWithNoSubcommandPrintsUsage()
    {
        var result = TestCli.Run("api");

        Assert.Equal(2, result.Status);
        Assert.Contains("Usage: litespec api explore", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>An unknown subcommand is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "an unknown subcommand is a usage error")]
    public void UnknownSubcommandIsAUsageError()
    {
        var result = TestCli.Run("api", "update");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown command 'update'.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A missing --base-url is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a missing --base-url is a usage error")]
    public void MissingBaseUrlIsAUsageError()
    {
        var result = TestCli.Run("api", "explore");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --base-url needs a value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A base URL that is not absolute http or https is a usage error.</summary>
    /// <param name="baseUrl">The invalid base URL.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "a base URL that is not absolute http or https is a usage error")]
    [InlineData("expenses")]
    [InlineData("ftp://files/expenses")]
    public void InvalidBaseUrlIsAUsageError(string baseUrl)
    {
        var result = TestCli.Run("api", "explore", "--base-url", baseUrl);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Invalid base URL '{baseUrl}': use an absolute http or https URL.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A base URL outside loopback without --allow-remote is a usage error that says why.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a base URL outside loopback without --allow-remote is a usage error that says why")]
    public void RemoteBaseUrlWithoutAllowRemoteIsAUsageError()
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://example.com");

        Assert.Equal(2, result.Status);
        Assert.Contains("is not loopback", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("--allow-remote", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A step count that is not a positive number is a usage error.</summary>
    /// <param name="steps">The invalid steps value.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "a step count that is not a positive number is a usage error")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("many")]
    public void InvalidStepsIsAUsageError(string steps)
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--steps", steps);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Invalid steps '{steps}': use a positive number.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A seed that is not a whole number is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a seed that is not a whole number is a usage error")]
    public void InvalidSeedIsAUsageError()
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--seed", "many");

        Assert.Equal(2, result.Status);
        Assert.Contains("Invalid seed 'many': use a whole number.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A header without a colon is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a header without a colon is a usage error")]
    public void HeaderWithoutColonIsAUsageError()
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--header", "Authorization");

        Assert.Equal(2, result.Status);
        Assert.Contains("Invalid header 'Authorization': use Name: value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A Cookie header belongs in --cookie-file, not in --header.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a Cookie header belongs in --cookie-file, not in --header")]
    public void CookieHeaderIsAUsageError()
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--header", "Cookie: session=abc");

        Assert.Equal(2, result.Status);
        Assert.Contains("comes from --cookie-file", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A --deny without a method and a pattern is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a --deny without a method and a pattern is a usage error")]
    public void DenyWithoutPatternIsAUsageError()
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--deny", "POST");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --deny needs a method and a path pattern.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A deny method outside HTTP methods and * is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a deny method outside HTTP methods and * is a usage error")]
    public void InvalidDenyMethodIsAUsageError()
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--deny", "123", "/expenses");

        Assert.Equal(2, result.Status);
        Assert.Contains("Invalid deny method '123': use an HTTP method or *.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A missing cookie file is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a missing cookie file is a usage error")]
    public void MissingCookieFileIsAUsageError()
    {
        using var folder = TestCli.Temp("explore-bare");
        var missing = Path.Combine(folder.Path, "cookies.txt");

        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--cookie-file", missing);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Cookie file '{missing}' does not exist.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A cookie line without name=value is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a cookie line without name=value is a usage error")]
    public void InvalidCookieFileIsAUsageError()
    {
        using var folder = TestCli.Temp("explore-bare");
        var cookies = Path.Combine(folder.Path, "cookies.txt");
        File.WriteAllText(cookies, "session=abc\nnonsense\n");

        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--cookie-file", cookies);

        Assert.Equal(2, result.Status);
        Assert.Contains("line 2 needs name=value.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>An unknown option is a usage error.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "an unknown option is a usage error")]
    public void UnknownOptionIsAUsageError()
    {
        var result = TestCli.Run("api", "explore", "--base-url", "http://localhost:5000", "--headed");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--headed'.", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>An unreadable OpenAPI document fails the run with exit 1.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "an unreadable OpenAPI document fails the run with exit 1")]
    public void UnreadableOpenApiFailsTheRun()
    {
        using var folder = TestCli.Temp("explore-bare");

        var result = TestCli.Run(
            ["api", "explore", "--base-url", "http://127.0.0.1:44999", "--out", Path.Combine(folder.Path, "explore")],
            folder.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("Cannot read the OpenAPI document at 'http://127.0.0.1:44999/openapi/v1.json'", result.Stderr, StringComparison.Ordinal);
    }
}
