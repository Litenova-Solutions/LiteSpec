using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.CodeFixes;

/// <summary>Tests the seal fix: the before and after, and the Fix All policy.</summary>
public sealed class SealTypeCodeFixTests
{
    /// <summary>Seal fix seals an unsealed leaf class.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "Seal fix seals an unsealed leaf class")]
    public Task SealsAnUnsealedLeafClass() =>
        Verify.CodeFixAsync<SealedClassAnalyzer, SealTypeCodeFixProvider>(
            """
            namespace Shop.Orders;

            public class Order { }
            """,
            """
            namespace Shop.Orders;

            public sealed class Order { }
            """,
            "Declare type sealed",
            path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Seal fix seals an unsealed state case.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "Seal fix seals an unsealed state case")]
    public Task SealsAnUnsealedStateCase() =>
        Verify.CodeFixAsync<AggregateStateAnalyzer, SealTypeCodeFixProvider>(
            """
            using System;

            namespace Shop.Orders;

            public abstract class AggregateRoot<TId>
            {
                protected AggregateRoot(TId id) { Id = id; }
                public TId Id { get; }
            }

            public readonly record struct OrderId(Guid Value);

            public sealed class Order : AggregateRoot<OrderId>
            {
                public Order(OrderId id, OrderState state) : base(id) { State = state; }
                public OrderState State { get; }
            }

            public abstract record OrderState;

            public record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;
            """,
            """
            using System;

            namespace Shop.Orders;

            public abstract class AggregateRoot<TId>
            {
                protected AggregateRoot(TId id) { Id = id; }
                public TId Id { get; }
            }

            public readonly record struct OrderId(Guid Value);

            public sealed class Order : AggregateRoot<OrderId>
            {
                public Order(OrderId id, OrderState state) : base(id) { State = state; }
                public OrderState State { get; }
            }

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;
            """,
            "Declare type sealed",
            path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Fix All seals two classes.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "Fix All seals two classes")]
    public Task FixAllSealsTwoClasses() =>
        Verify.CodeFixAllAsync<SealedClassAnalyzer, SealTypeCodeFixProvider>(
            [
                ("Shop.Spending/Domain/Order.cs", """
                    namespace Shop.Orders;

                    public class Order { }
                    """),
                ("Shop.Spending/Domain/Customer.cs", """
                    namespace Shop.Orders;

                    public class Customer { }
                    """),
            ],
            [
                ("Shop.Spending/Domain/Order.cs", """
                    namespace Shop.Orders;

                    public sealed class Order { }
                    """),
                ("Shop.Spending/Domain/Customer.cs", """
                    namespace Shop.Orders;

                    public sealed class Customer { }
                    """),
            ],
            [],
            "SealType",
            "Shop.Spending");

    /// <summary>Seal fix leaves an abstract class without sealed.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "Seal fix leaves an abstract class without sealed")]
    public Task LeavesAnAbstractClassWithoutSealed() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>(
            """
            namespace Shop.Orders;

            public abstract class OrderBase { }
            """,
            path: "Shop.Spending/Domain/Source.cs");

    /// <summary>Seal fix offers Fix All.</summary>
    /// <proves>litespec/packs/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "Seal fix offers Fix All")]
    public void OffersFixAll()
    {
        Assert.NotNull(new SealTypeCodeFixProvider().GetFixAllProvider());
    }
}
