using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves a marked endpoint in a host project exposes its use case like a module endpoint.</summary>
public sealed class HostEndpointTests
{
    /// <summary>Marked Host Endpoint Exposes Its Use Case.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-host-endpoints-in-hosts</proves>
    [Fact]
    public void MarkedHostEndpointExposesItsUseCase()
    {
        using var fixture = MarksFixture.Prepare("host");
        var config = ConsumerConfig.Load(fixture.Path);
        var model = ModelBuilder.Build(config);
        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "use-case-endpoint-missing");

        var operations = model.Code.Endpoints
            .Select(endpoint => $"{endpoint.Operation}|{endpoint.UseCaseId}|{endpoint.Method}|{endpoint.Route}")
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Contains("BorrowCopy|use-case/lending.borrow-copy||", operations);
        Assert.Contains("|use-case/lending.borrow-copy|POST|/api/lending/borrow-copy", operations);
    }

    /// <summary>Host Endpoint Outside Any Read Root Still Reports Missing.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-host-endpoints-in-hosts</proves>
    [Fact]
    public void HostEndpointOutsideAnyReadRootStillReportsMissing()
    {
        using var fixture = MarksFixture.Prepare("host");
        Directory.Move(
            Path.Combine(fixture.Path, "apps", "api", "src", "Hosts"),
            Path.Combine(fixture.Path, "apps", "api", "src", "Services"));

        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-endpoint-missing");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }
}
