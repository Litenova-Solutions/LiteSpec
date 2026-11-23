using LiteSpec.Cli.Explore;

namespace LiteSpec.Cli.Tests.Explore;

/// <summary>Tests for the walk deny list: the safe defaults, --allow-delete, and user entries.</summary>
public sealed class ExploreDenyTests
{
    /// <summary>The defaults deny deletes, sign-outs and erasures, and walk anything else.</summary>
    /// <param name="method">The operation method.</param>
    /// <param name="template">The operation path template.</param>
    /// <param name="denied">Whether the defaults deny the operation.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "the defaults deny deletes, sign-outs and erasures, and walk anything else")]
    [InlineData("DELETE", "/expenses/e-1", true)]
    [InlineData("POST", "/expenses/delete", true)]
    [InlineData("POST", "/session/sign-out", true)]
    [InlineData("POST", "/session/signout", true)]
    [InlineData("POST", "/auth/logout", true)]
    [InlineData("POST", "/auth/log-out", true)]
    [InlineData("POST", "/records/erase", true)]
    [InlineData("POST", "/records/erasure", true)]
    [InlineData("GET", "/expenses", false)]
    [InlineData("POST", "/expenses", false)]
    [InlineData("GET", "/expenses/e-1", false)]
    public void DefaultsDenyDeletesSignoutsAndErasures(string method, string template, bool denied)
    {
        var matches = ExploreDeny.Defaults(allowDelete: false).Any(entry => entry.Deny.Matches(method, template));

        Assert.Equal(denied, matches);
    }

    /// <summary>--allow-delete lifts the delete entries and keeps sign-outs and erasures denied.</summary>
    /// <param name="method">The operation method.</param>
    /// <param name="template">The operation path template.</param>
    /// <param name="denied">Whether the defaults still deny the operation.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "--allow-delete lifts the delete entries and keeps sign-outs and erasures denied")]
    [InlineData("DELETE", "/expenses/e-1", false)]
    [InlineData("POST", "/expenses/delete", false)]
    [InlineData("POST", "/session/sign-out", true)]
    [InlineData("POST", "/records/erasure", true)]
    public void AllowDeleteLiftsDeletesOnly(string method, string template, bool denied)
    {
        var matches = ExploreDeny.Defaults(allowDelete: true).Any(entry => entry.Deny.Matches(method, template));

        Assert.Equal(denied, matches);
    }

    /// <summary>A deny entry matches its method and its path substring.</summary>
    /// <param name="method">The operation method.</param>
    /// <param name="template">The operation path template.</param>
    /// <param name="denied">Whether the entry denies the operation.</param>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Theory(DisplayName = "a deny entry matches its method and its path substring")]
    [InlineData("POST", "/session/sign-out", true)]
    [InlineData("GET", "/session/sign-out", false)]
    [InlineData("POST", "/expenses", false)]
    public void EntryMatchesMethodAndPattern(string method, string template, bool denied)
    {
        Assert.Equal(denied, new ExploreDeny("POST", "sign-out").Matches(method, template));
    }

    /// <summary>A star method matches every method and a star pattern matches every path.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a star method matches every method and a star pattern matches every path")]
    public void StarsMatchEverything()
    {
        Assert.True(new ExploreDeny("*", "export").Matches("GET", "/reports/export"));
        Assert.False(new ExploreDeny("*", "export").Matches("GET", "/expenses"));
        Assert.True(new ExploreDeny("DELETE", "*").Matches("DELETE", "/expenses/e-1"));
        Assert.False(new ExploreDeny("DELETE", "*").Matches("GET", "/expenses/e-1"));
    }

    /// <summary>Matching ignores case in the method and the path.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "matching ignores case in the method and the path")]
    public void MatchingIgnoresCase()
    {
        Assert.True(new ExploreDeny("POST", "sign-out").Matches("post", "/Session/Sign-Out"));
    }
}
