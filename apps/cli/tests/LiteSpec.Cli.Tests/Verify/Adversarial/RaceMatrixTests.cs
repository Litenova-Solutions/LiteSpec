using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Verify.Probes;

namespace LiteSpec.Cli.Tests.Verify.Adversarial;

/// <summary>Proves the race matrix sends idempotent probes five times at once.</summary>
public sealed class RaceMatrixTests
{
    /// <summary>A repeat that fails with a server error is reported as idempotency-failed.</summary>
    [Fact]
    public void RepeatThatFailsReportsIdempotencyRace()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.PatchIdempotencyKey(temp.Path, "borrow-copy");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-race-fails", null, "POST", "/api/loans", null, 200);

        using var server = TestProbeServer.Start((request, body) => (500, """{"title":"Broken"}"""));
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[idempotency-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.Equal(["idempotency-failed"], AdversarialFixture.ReportCodes(temp.Path));
        Assert.Equal(5, server.Paths.Count);
    }

    /// <summary>Repeats that share one success body report nothing.</summary>
    [Fact]
    public void RepeatsThatShareOneBodyReportNothing()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.PatchIdempotencyKey(temp.Path, "borrow-copy");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-race-shared", null, "POST", "/api/loans", null, 200);

        using var server = TestProbeServer.Start((request, body) => (200, """{"state":"lent"}"""));
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("[idempotency-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(AdversarialFixture.ReportCodes(temp.Path));
        Assert.Equal(5, server.Paths.Count);
    }

    /// <summary>Repeats that answer different success bodies are reported as idempotency-failed.</summary>
    [Fact]
    public void RepeatsWithDifferentBodiesReportIdempotencyRace()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.PatchIdempotencyKey(temp.Path, "borrow-copy");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-race-differs", null, "POST", "/api/loans", null, 200);

        var count = 0;
        using var server = TestProbeServer.Start((request, body) =>
            (200, $"{{\"loan\":{Interlocked.Increment(ref count)}}}"));
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[idempotency-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.Equal(["idempotency-failed"], AdversarialFixture.ReportCodes(temp.Path));
    }

    /// <summary>A command without key or state idempotency is skipped by the race matrix.</summary>
    [Fact]
    public void CommandWithoutKeyOrStateSkipsTheRaceMatrix()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-race-skipped", null, "POST", "/api/loans", null, 200);

        var hits = 0;
        using var server = TestProbeServer.Start((request, body) => { hits++; return (200, """{"state":"lent"}"""); });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.Equal(0, hits);
        Assert.Empty(AdversarialFixture.ReportCodes(temp.Path));
    }
}
