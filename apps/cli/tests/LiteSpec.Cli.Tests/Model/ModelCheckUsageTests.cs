using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the cut model subcommands are rejected as unknown commands.</summary>
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
}
