using LiteSpec.Cli.Tests.Support;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Verify.Probes;
using LiteSpec.Cli.Verify.Adversarial;

namespace LiteSpec.Cli.Tests.Verify.Adversarial;

/// <summary>Proves the adversarial command parses its options and guards its run.</summary>
public sealed class AdversarialCommandTests
{
    /// <summary>--help prints the usage text and exits 0.</summary>
    [Fact]
    public void HelpPrintsUsage()
    {
        var result = TestCli.Run(["verify", "--adversarial", "--help"]);
        var status = result.Status;
        var output = result.Stdout;

        Assert.Equal(0, status);
        Assert.Contains("adversarial", output, StringComparison.Ordinal);
    }

    /// <summary>An unknown option is a usage error.</summary>
    [Fact]
    public void UnknownOptionIsAUsageError()
    {
        var result = TestCli.Run(["verify", "--adversarial", "--bogus"]);
        var status = result.Status;
        var output = result.Stdout;

        Assert.Equal(2, status);
    }

    /// <summary>An unknown use case is a usage error reported before anything is sent.</summary>
    [Fact]
    public void UnknownUseCaseIsAUsageError()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-unknown-use-case", null, "GET", "/api/loans", null, 200);

        var hits = 0;
        using var server = TestProbeServer.Start((request, body) => { hits++; return (200, """{"state":"lent"}"""); });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl, "use-case/lending.unknown");

        Assert.Equal(2, result.Status);
        Assert.Equal(0, hits);
    }

    /// <summary>A probe naming no cast person is a usage error reported before anything is sent.</summary>
    [Fact]
    public void UnknownCastPersonIsAUsageError()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-unknown-person", "person/unknown", "GET", "/api/loans", null, 200);

        var hits = 0;
        using var server = TestProbeServer.Start((request, body) => { hits++; return (200, """{"state":"lent"}"""); });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(2, result.Status);
        Assert.Equal(0, hits);
        Assert.Contains("person/unknown", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>No base address is a usage error reported before anything is sent.</summary>
    [Fact]
    public void MissingBaseAddressIsAUsageError()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-no-base-url", null, "GET", "/api/loans", null, 200);

        var result = TestCli.Run(["verify", "--adversarial", "--root", temp.Path]);
        var status = result.Status;
        var output = result.Stdout;

        Assert.Equal(2, status);
    }
}
