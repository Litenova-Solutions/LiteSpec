using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the cut model subcommands are rejected as unknown commands, <c>--all</c> is rejected, <c>--record-proof</c> is accepted, and <c>--record-proof</c> is rejected where it does not apply.</summary>
public sealed class ModelCheckUsageTests
{
    /// <summary>Cut model subcommand '<paramref name="command"/>' exits 2 as an unknown command.</summary>
    /// <param name="command">The cut subcommand to run.</param>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Theory]
    [InlineData("approve")]
    [InlineData("names")]
    [InlineData("diff")]
    public void CutModelSubcommandIsRejected(string command)
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", command, "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains($"Unknown command '{command}'.", result.Output);
    }

    /// <summary>The check rejects the cut <c>--all</c> option as an unknown option.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void CheckAllOptionIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--all");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--all'.", result.Output);
        Assert.DoesNotContain("[--all]", LiteSpec.Cli.Model.ModelCommand.Usage, StringComparison.Ordinal);
    }

    /// <summary>The check accepts <c>--record-proof</c> and names it in the usage text.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void CheckRecordProofOptionIsAccepted()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--record-proof");

        Assert.Equal(0, result.Status);
        Assert.Contains("[--record-proof]", LiteSpec.Cli.Model.ModelCommand.Usage, StringComparison.Ordinal);
    }

    /// <summary><c>--record-proof</c> on a subcommand that does not take it exits 2 as an unknown option.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void RecordProofOnAnotherSubcommandIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--record-proof", "use-case/lending.borrow-copy");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option '--record-proof'.", result.Output);
    }
}
