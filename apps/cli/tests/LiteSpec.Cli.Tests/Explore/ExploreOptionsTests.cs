using LiteSpec.Cli.Explore;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Tests.Explore;

/// <summary>Tests for explore option parsing: repeatable options, defaults, and the loopback guard.</summary>
public sealed class ExploreOptionsTests
{
    /// <summary>The split collects repeated headers and denies and passes the rest through.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "the split collects repeated headers and denies and passes the rest through")]
    public void SplitCollectsRepeatedOptions()
    {
        var (headers, denies, rest, error) = ExploreOptions.SplitRepeatable(
        [
            "--base-url", "http://localhost:5000",
            "--header", "Authorization: Bearer abc",
            "--header=X-Walk: 1",
            "--deny", "POST", "sign-out",
            "--deny=DELETE", "*",
        ]);

        Assert.Null(error);
        Assert.Equal(["Authorization: Bearer abc", "X-Walk: 1"], headers);
        Assert.Equal([("POST", "sign-out"), ("DELETE", "*")], denies);
        Assert.Equal(["--base-url", "http://localhost:5000"], rest);
    }

    /// <summary>The split passes a value option with its following value untouched.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "the split passes a value option with its following value untouched")]
    public void SplitKeepsValueOptionValues()
    {
        var (headers, denies, rest, error) = ExploreOptions.SplitRepeatable(["--out", "--deny"]);

        Assert.Null(error);
        Assert.Empty(headers);
        Assert.Empty(denies);
        Assert.Equal(["--out", "--deny"], rest);
    }

    /// <summary>Parsing applies the defaults and trims the base URL.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "parsing applies the defaults and trims the base URL")]
    public void ParseAppliesDefaults()
    {
        var (headers, denies, rest, splitError) = ExploreOptions.SplitRepeatable(["--base-url", "http://localhost:5000/"]);
        Assert.Null(splitError);
        var parsed = CliArgs.Parse(
            [.. rest],
            ["--allow-delete", "--allow-remote"],
            ["--base-url", "--openapi", "--steps", "--seed", "--cookie-file", "--out"]);

        var (options, error) = ExploreOptions.Parse(parsed, headers, denies);

        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal("http://localhost:5000", options.BaseUrl);
        Assert.Equal("/openapi/v1.json", options.OpenApi);
        Assert.Equal(200, options.Steps);
        Assert.Equal(1, options.Seed);
        Assert.Empty(options.Headers);
        Assert.Null(options.CookieFile);
        Assert.Empty(options.Denies);
        Assert.Equal(".litespec/explore", options.Out);
        Assert.False(options.AllowDelete);
        Assert.False(options.AllowRemote);
    }

    /// <summary>Parsing reads the flags, the headers and the denies.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "parsing reads the flags, the headers and the denies")]
    public void ParseReadsFlagsHeadersAndDenies()
    {
        var (headers, denies, rest, splitError) = ExploreOptions.SplitRepeatable(
        [
            "--base-url", "http://localhost:5000",
            "--steps", "50",
            "--seed", "7",
            "--header", "Authorization: Bearer abc",
            "--deny", "post", "export",
            "--allow-delete",
            "--allow-remote",
        ]);
        Assert.Null(splitError);
        var parsed = CliArgs.Parse(
            [.. rest],
            ["--allow-delete", "--allow-remote"],
            ["--base-url", "--openapi", "--steps", "--seed", "--cookie-file", "--out"]);

        var (options, error) = ExploreOptions.Parse(parsed, headers, denies);

        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal(50, options.Steps);
        Assert.Equal(7, options.Seed);
        Assert.Equal([("Authorization", "Bearer abc")], options.Headers);
        Assert.Equal([new ExploreDeny("POST", "export")], options.Denies);
        Assert.True(options.AllowDelete);
        Assert.True(options.AllowRemote);
    }

    /// <summary>Localhost, 127.x.x.x and ::1 are loopback.</summary>
    /// <param name="host">The loopback host.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "localhost, 127.x.x.x and ::1 are loopback")]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.2")]
    [InlineData("::1")]
    public void LoopbackHostsPass(string host)
    {
        Assert.True(ExploreOptions.IsLoopbackHost(host));
    }

    /// <summary>Any other host or address is not loopback.</summary>
    /// <param name="host">The remote host.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "any other host or address is not loopback")]
    [InlineData("example.com")]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("::2")]
    public void RemoteHostsFail(string host)
    {
        Assert.False(ExploreOptions.IsLoopbackHost(host));
    }
}
