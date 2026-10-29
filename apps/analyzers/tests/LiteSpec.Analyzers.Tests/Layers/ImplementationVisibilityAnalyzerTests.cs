using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2001, internal sealed implementation types.</summary>
public sealed class ImplementationVisibilityAnalyzerTests
{
    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 reports a public handler")]
    public Task ReportsAPublicHandler() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            public sealed class {|LS2001:PlaceOrderCommandHandler|} { }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 reports an internal unsealed validator")]
    public Task ReportsAnInternalUnsealedValidator() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            internal class {|LS2001:PlaceOrderValidator|} { }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 reports a public unsealed repository once")]
    public Task ReportsAPublicUnsealedRepositoryOnce() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            public class {|LS2001:OrderRepository|} { }
            """, assemblyName: "Shop.Infrastructure");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 accepts an internal sealed handler")]
    public Task AcceptsAnInternalSealedHandler() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            internal sealed class PlaceOrderCommandHandler { }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 accepts an internal static projection")]
    public Task AcceptsAnInternalStaticProjection() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            internal static class OrderSummaryProjection { }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 reports a public static projection")]
    public Task ReportsAPublicStaticProjection() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            public static class {|LS2001:OrderSummaryProjection|} { }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 accepts a class without an implementation name")]
    public Task AcceptsAClassWithoutAnImplementationName() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            public sealed class OrderService { }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 reports a nested public endpoint")]
    public Task ReportsANestedPublicEndpoint() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Api;

            public class Outer
            {
                public sealed class {|LS2001:GetOrderEndpoint|} { }
            }
            """, assemblyName: "Shop.WebApi");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 accepts a nested private endpoint")]
    public Task AcceptsANestedPrivateEndpoint() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Api;

            public class Outer
            {
                private sealed class GetOrderEndpoint { }
            }
            """, assemblyName: "Shop.WebApi");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 reports a class implementing IEndpoint without the suffix")]
    public Task ReportsAClassImplementingIEndpoint() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Api;

            public interface IEndpoint { }

            public sealed class {|LS2001:OrderRoutes|} : IEndpoint { }
            """, assemblyName: "Shop.WebApi");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 ignores an abstract base handler")]
    public Task IgnoresAnAbstractBaseHandler() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            public abstract class OrderHandler { }
            """, assemblyName: "Shop.Application");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 ignores an implementation type in Domain")]
    public Task IgnoresAnImplementationTypeInDomain() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            public sealed class PlaceOrderCommandHandler { }
            """);

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 ignores an implementation type in Application.Abstractions")]
    public Task IgnoresAnImplementationTypeInApplicationAbstractions() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Orders;

            public sealed class PlaceOrderCommandHandler { }
            """, assemblyName: "Shop.Application.Abstractions");

    /// <proves>litespec/backend/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "LS2001 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<ImplementationVisibilityAnalyzer>("""
            namespace Shop.Core;

            public sealed class {|LS2001:PlaceOrderCommandHandler|} { }
            """, assemblyName: "Shop.Core", layer: "Application");
}
