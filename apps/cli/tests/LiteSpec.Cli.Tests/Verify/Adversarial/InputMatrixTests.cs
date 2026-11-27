using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Verify.Probes;

namespace LiteSpec.Cli.Tests.Verify.Adversarial;

/// <summary>Proves the input matrix replays swapped-type and oversized bodies.</summary>
public sealed class InputMatrixTests
{
    /// <summary>A swapped type that fails with a server error is reported as input-handling-failed.</summary>
    [Fact]
    public void SwappedTypeThatFailsReportsInputCrash()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-input-crash", null, "POST", "/api/loans", "amount: 42.5", 200);

        using var server = TestProbeServer.Start((request, body) =>
        {
            if (body is not null && body.Contains("\"amount\":\"x\"", StringComparison.Ordinal))
            {
                return (500, """{"title":"Broken"}""");
            }

            return body is not null && body.Length > 5000 ? (400, """{"title":"Too large"}""") : (200, """{"state":"lent"}""");
        });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        Assert.Contains("[input-handling-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.Equal(["input-handling-failed"], AdversarialFixture.ReportCodes(temp.Path));
        Assert.Equal(2, server.Paths.Count);
    }

    /// <summary>A swapped type that succeeds is reported as input-validation-failed, for every top-level type and the oversized body.</summary>
    [Fact]
    public void SwappedTypesThatSucceedReportInputUnguarded()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(
            temp.Path,
            "borrow-copy",
            "scenario/borrow-copy.adversarial-input-unguarded",
            null,
            "POST",
            "/api/loans",
            "name: ada\ncount: 3\nactive: true\ntags: [a]",
            200);

        using var server = TestProbeServer.Start((request, body) => (200, """{"state":"lent"}"""));
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(1, result.Status);
        var codes = AdversarialFixture.ReportCodes(temp.Path);
        Assert.Equal(5, codes.Count);
        Assert.All(codes, code => Assert.Equal("input-validation-failed", code));
        Assert.Equal(5, server.Paths.Count);
    }

    /// <summary>Swapped types that are refused report nothing.</summary>
    [Fact]
    public void SwappedTypesThatAreRefusedReportNothing()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-input-refused", null, "POST", "/api/loans", "amount: 42.5", 200);

        using var server = TestProbeServer.Start((request, body) =>
        {
            if (body is not null && (body.Contains("\"amount\":\"x\"", StringComparison.Ordinal) || body.Length > 5000))
            {
                return (422, """{"code":"request.validation-failed"}""");
            }

            return (200, """{"state":"lent"}""");
        });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("[input-handling-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("[input-validation-failed]", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(AdversarialFixture.ReportCodes(temp.Path));
        Assert.Equal(2, server.Paths.Count);
    }

    /// <summary>A probe whose body is no JSON object is skipped by the input matrix.</summary>
    [Fact]
    public void ProbeWithNonObjectBodySkipsTheInputMatrix()
    {
        using var temp = ModelFixture.Prepare("small");
        AdversarialFixture.AddProbe(temp.Path, "borrow-copy", "scenario/borrow-copy.adversarial-input-skipped", null, "POST", "/api/loans", "[1, 2]", 200);

        var hits = 0;
        using var server = TestProbeServer.Start((request, body) => { hits++; return (200, """{"state":"lent"}"""); });
        var result = AdversarialFixture.RunAdversarial(temp.Path, server.BaseUrl);

        Assert.Equal(0, result.Status);
        Assert.Equal(0, hits);
        Assert.Empty(AdversarialFixture.ReportCodes(temp.Path));
    }
}
