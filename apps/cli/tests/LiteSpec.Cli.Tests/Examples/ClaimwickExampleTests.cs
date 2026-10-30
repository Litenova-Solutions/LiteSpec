using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Examples;

/// <summary>
///     The Claimwick example in <c>examples/claimwick/</c> is the product the Example
///     docs follow and quote, so it must keep passing every consumer check the docs
///     show: the project and page checks, the controlled UI, and the model.
/// </summary>
public sealed class ClaimwickExampleTests
{
    static readonly string Root = Path.Combine(Cli.Repository, "examples", "claimwick");

    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "the Claimwick example passes the consumer check")]
    public void PassesConsumer() => AssertPasses(Cli.Run("consumer", "--root", Root));

    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact(DisplayName = "the Claimwick example passes the UI check")]
    public void PassesUi() => AssertPasses(Cli.Run("ui", "--root", Root));

    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact(DisplayName = "the Claimwick example passes the model check with only known findings")]
    public void PassesModelCheck()
    {
        var result = Cli.Run("model", "check", "--root", Root);
        AssertPasses(result);
        Assert.Contains("0 findings, 17 known.", result.Output);
    }

    static void AssertPasses(CliResult result) => Assert.True(result.Status == 0, result.Output);
}
