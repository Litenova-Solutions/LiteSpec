using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.CodeFixes;

/// <summary>Tests the internal sealed fix: the before and after, and the Fix All policy.</summary>
public sealed class MakeInternalSealedCodeFixTests
{
    /// <summary>Internal sealed fix declares a public handler internal sealed.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "Internal sealed fix declares a public handler internal sealed")]
    public Task DeclaresAPublicHandlerInternalSealed() =>
        Verify.CodeFixAsync<ImplementationVisibilityAnalyzer, MakeInternalSealedCodeFixProvider>(
            """
            namespace Shop.Orders;

            public class PlaceOrderCommandHandler { }
            """,
            """
            namespace Shop.Orders;

            internal sealed class PlaceOrderCommandHandler { }
            """,
            "Declare type internal sealed",
            path: "Shop.Spending/Application/Source.cs");

    /// <summary>Internal sealed fix seals an internal unsealed validator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "Internal sealed fix seals an internal unsealed validator")]
    public Task SealsAnInternalUnsealedValidator() =>
        Verify.CodeFixAsync<ImplementationVisibilityAnalyzer, MakeInternalSealedCodeFixProvider>(
            """
            namespace Shop.Orders;

            internal class PlaceOrderValidator { }
            """,
            """
            namespace Shop.Orders;

            internal sealed class PlaceOrderValidator { }
            """,
            "Declare type internal sealed",
            path: "Shop.Spending/Application/Source.cs");

    /// <summary>Fix All declares two handlers internal sealed.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "Fix All declares two handlers internal sealed")]
    public Task FixAllDeclaresTwoHandlersInternalSealed() =>
        Verify.CodeFixAllAsync<ImplementationVisibilityAnalyzer, MakeInternalSealedCodeFixProvider>(
            [
                ("Shop.Spending/Application/PlaceOrder.cs", """
                    namespace Shop.Orders;

                    public class PlaceOrderCommandHandler { }
                    """),
                ("Shop.Spending/Application/ApproveOrder.cs", """
                    namespace Shop.Orders;

                    public class ApproveOrderCommandHandler { }
                    """),
            ],
            [
                ("Shop.Spending/Application/PlaceOrder.cs", """
                    namespace Shop.Orders;

                    internal sealed class PlaceOrderCommandHandler { }
                    """),
                ("Shop.Spending/Application/ApproveOrder.cs", """
                    namespace Shop.Orders;

                    internal sealed class ApproveOrderCommandHandler { }
                    """),
            ],
            [],
            "MakeInternalSealed",
            "Shop.Spending");

    /// <summary>Internal sealed fix leaves a static class without sealed.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "Internal sealed fix leaves a static class without sealed")]
    public Task LeavesAStaticClassWithoutSealed() =>
        Verify.CodeFixAsync<ImplementationVisibilityAnalyzer, MakeInternalSealedCodeFixProvider>(
            """
            namespace Shop.Orders;

            public static class OrderSummaryProjection { }
            """,
            """
            namespace Shop.Orders;

            internal static class OrderSummaryProjection { }
            """,
            "Declare type internal sealed",
            path: "Shop.Spending/Application/Source.cs");

    /// <summary>Internal sealed fix offers Fix All.</summary>
    /// <proves>litespec/packs/dotnet/architecture#keep-implementation-types-internal</proves>
    [Fact(DisplayName = "Internal sealed fix offers Fix All")]
    public void OffersFixAll()
    {
        Assert.NotNull(new MakeInternalSealedCodeFixProvider().GetFixAllProvider());
    }
}
