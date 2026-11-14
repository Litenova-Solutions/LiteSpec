using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Examples;

/// <summary>
///     The Claimwick example in <c>examples/claimwick/</c> is the product the Example
///     docs follow and quote, so it must keep passing <c>model check</c>, which runs
///     the project and page checks, the controlled UI, and the model in one run.
/// </summary>
public sealed class ClaimwickExampleTests
{
    static readonly string Root = Path.Combine(TestCli.Repository, "examples", "claimwick");

    /// <summary>The Claimwick example passes the model check with only covered findings.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact(DisplayName = "the Claimwick example passes the model check with only covered findings")]
    public void PassesModelCheck()
    {
        var result = TestCli.Run("model", "check", "--root", Root);
        AssertPasses(result);
        // 16 long-standing covered findings, the use-case-unverified findings the no-test-run
        // departure holds for every use case, and the reporting query that no screen and no
        // journey step reaches yet: the example ships code and proofs but no run.
        Assert.Contains("0 findings, 0 blocking, 28 covered.", result.Output);
    }

    static void AssertPasses(CliResult result) => Assert.True(result.Status == 0, result.Output);
}
