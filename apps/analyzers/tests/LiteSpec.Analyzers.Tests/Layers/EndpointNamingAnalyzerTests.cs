using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2006, the endpoint suffix.</summary>
public sealed class EndpointNamingAnalyzerTests
{
    /// <summary>LS2006 reports an endpoint without its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2006 reports an endpoint without its suffix")]
    public Task ReportsAnEndpointWithoutItsSuffix() =>
        Verify.AnalyzerAsync<EndpointNamingAnalyzer>("""
            namespace Shop.Api;

            public interface IEndpoint { }

            internal sealed class {|LS2006:CreateDraftRoute|} : IEndpoint
            {
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS2006 accepts an endpoint with its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2006 accepts an endpoint with its suffix")]
    public Task AcceptsAnEndpointWithItsSuffix() =>
        Verify.AnalyzerAsync<EndpointNamingAnalyzer>("""
            namespace Shop.Api;

            public interface IEndpoint { }

            internal sealed class CreateDraftEndpoint : IEndpoint
            {
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS2006 ignores a class that implements no endpoint contract.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2006 ignores a class that implements no endpoint contract")]
    public Task IgnoresANonEndpoint() =>
        Verify.AnalyzerAsync<EndpointNamingAnalyzer>("""
            namespace Shop.Api;

            internal sealed class CreateDraftRoute
            {
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS2006 ignores an endpoint outside WebApi.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2006 ignores an endpoint outside WebApi")]
    public Task IgnoresEndpointOutsideWebApi() =>
        Verify.AnalyzerAsync<EndpointNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IEndpoint { }

            internal sealed class CreateDraftRoute : IEndpoint
            {
            }
            """, assemblyName: "Shop.Application");

    /// <summary>LS2006 follows the LiteSpecLayer property over the assembly name.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#use-architectural-suffixes</proves>
    [Fact(DisplayName = "LS2006 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<EndpointNamingAnalyzer>("""
            namespace Shop.Core;

            public interface IEndpoint { }

            internal sealed class {|LS2006:CreateDraftRoute|} : IEndpoint
            {
            }
            """, assemblyName: "Shop.Core", layer: "WebApi");
}
