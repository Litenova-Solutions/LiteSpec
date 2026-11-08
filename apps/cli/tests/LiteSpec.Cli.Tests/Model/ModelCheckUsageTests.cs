using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the cut model subcommands are rejected as unknown commands and <c>--all</c> is accepted.</summary>
public sealed class ModelCheckUsageTests
{
    /// <summary>Cut model subcommand '<paramref name="command"/>' exits 2 as an unknown command.</summary>
    /// <param name="command">The cut subcommand to run.</param>
    [Theory]
    [InlineData("approve")]
    [InlineData("review")]
    [InlineData("names")]
    [InlineData("diff")]
    public void Cut_Model_Subcommand_Is_Rejected(string command)
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = Cli.Run("model", command, "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Unknown command '{command}'.", result.Output);
    }

    /// <summary>The check accepts <c>--all</c> and names it in the usage text.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact]
    public void Check_All_Option_Is_Accepted()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.Equal(0, result.Status);
        Assert.Contains("[--all]", LiteSpec.Model.ModelCommand.Usage, StringComparison.Ordinal);
    }
}
