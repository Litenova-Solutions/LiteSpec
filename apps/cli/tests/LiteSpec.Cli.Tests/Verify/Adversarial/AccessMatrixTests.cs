using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Verify.Probes;

namespace LiteSpec.Cli.Tests.Verify.Adversarial;

/// <summary>Proves the access matrix replays signed-in probes with no session.</summary>
public sealed class AccessMatrixTests
{
    /// <summary>An anonymous replay that succeeds is reported as access-limit-failed.</summary>
    [Fact]
    public void AnonymousReplayThatSucceedsReportsAccessOpen()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddCast(temp.Path);
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-access", "person/ava", "GET", "/api/loans", null, 200);

        string? cookie = "unset";
        using var server = TestProbeServer.Start((request, body) =>
        {
            cookie = request.Headers["Cookie"];
            return (200, """{"state":"lent"}""");
        });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[access-limit-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrEmpty(cookie), "the access replay sends with no session");
        Assert.Equal(["access-limit-failed"], AdversarialFixture.ReportCodes(temp.Path));
    }

    /// <summary>An anonymous replay that is refused reports nothing.</summary>
    [Fact]
    public void AnonymousReplayThatIsRefusedReportsNothing()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddCast(temp.Path);
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-closed", "person/ava", "GET", "/api/loans", null, 200);

        using var server = TestProbeServer.Start((request, body) => (403, """{"code":"authorization.forbidden"}"""));
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("[access-limit-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(AdversarialFixture.ReportCodes(temp.Path));
    }

    /// <summary>A probe that expects failure is skipped by the access matrix, even when the host would answer success.</summary>
    [Fact]
    public void ProbeThatExpectsFailureSkipsTheAccessMatrix()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddCast(temp.Path);
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-expected-refusal", "person/ava", "GET", "/api/loans", null, 403);

        var hits = 0;
        using var server = TestProbeServer.Start((request, body) => { hits++; return (200, """{"state":"lent"}"""); });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.Equal(0, hits);
        Assert.Empty(AdversarialFixture.ReportCodes(temp.Path));
    }
}
