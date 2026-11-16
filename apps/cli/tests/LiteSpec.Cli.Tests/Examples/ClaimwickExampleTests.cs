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

    /// <summary>The Claimwick example passes the model check with no blocking finding.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact(DisplayName = "the Claimwick example passes the model check with no blocking finding")]
    public void PassesModelCheck()
    {
        var result = TestCli.Run("model", "check", "--root", Root);
        AssertPasses(result);
        // The covered findings sit under their waivers. Six use cases no screen calls yet are
        // advisory. The six UI quality requirements are proved by token and route runs, so no
        // Behavior record fulfills them and they report nothing. The reminder-preference step
        // has no screen yet, so its step finding sits under the unscreened-steps waiver.
        Assert.Contains("0 findings, 0 blocking, 21 covered.", result.Output);
    }

    static void AssertPasses(CliResult result) => Assert.True(result.Status == 0, result.Output);
}
