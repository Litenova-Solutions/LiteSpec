using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the cut model subcommands are rejected as unknown commands and <c>--all</c> is accepted.</summary>
public sealed class ModelCheckUsageTests
{
    /// <summary>Cut model subcommand '<paramref name="command"/>' exits 2 as an unknown command.</summary>
    /// <param name="command">The cut subcommand to run.</param>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Theory]
    [InlineData("approve")]
    [InlineData("review")]
    [InlineData("names")]
    [InlineData("diff")]
    public void CutModelSubcommandIsRejected(string command)
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", command, "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Unknown command '{command}'.", result.Output);
    }

    /// <summary>The check accepts <c>--all</c> and names it in the usage text.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void CheckAllOptionIsAccepted()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.Equal(0, result.Status);
        Assert.Contains("[--all]", LiteSpec.Model.ModelCommand.Usage, StringComparison.Ordinal);
    }
}
