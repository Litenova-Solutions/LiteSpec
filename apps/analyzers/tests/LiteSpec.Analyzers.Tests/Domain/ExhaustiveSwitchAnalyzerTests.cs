using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1013, switches that leave a sealed case to the discard arm.</summary>
public sealed class ExhaustiveSwitchAnalyzerTests
{
    /// <summary>LS1013 reports a state case left to the discard arm.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 reports a state case left to the discard arm")]
    public Task ReportsAStateCaseLeftToTheDiscardArm() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed record OrderCancelledState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderDraftState draft => "draft",
                    OrderPlacedState placed => "placed",
                    {|LS1013:_|} => "other",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 reports every case left to the discard arm as one finding.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 reports every case left to the discard arm as one finding")]
    public Task ReportsEveryCaseLeftToTheDiscardArmOnce() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed record OrderCancelledState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderDraftState draft => "draft",
                    {|LS1013:_|} => "other",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 accepts a switch that handles every sealed case.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 accepts a switch that handles every sealed case")]
    public Task AcceptsASwitchThatHandlesEverySealedCase() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed record OrderCancelledState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderDraftState draft => "draft",
                    OrderPlacedState placed => "placed",
                    OrderCancelledState cancelled => "cancelled",
                    _ => "other",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 reports a closed set case left to the discard arm.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS1013 reports a closed set case left to the discard arm")]
    public Task ReportsAClosedSetCaseLeftToTheDiscardArm() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record RefundOutcome;

            public sealed record RefundPendingOutcome(string Reason) : RefundOutcome;

            public sealed record RefundSucceededOutcome : RefundOutcome;

            public sealed record RefundFailedOutcome : RefundOutcome;

            public sealed class RefundRouter
            {
                public string Describe(RefundOutcome outcome) => outcome switch
                {
                    RefundPendingOutcome pending => pending.Reason,
                    RefundSucceededOutcome succeeded => "succeeded",
                    {|LS1013:_|} => "other",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 accepts nested cases named by type plus a discard arm.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS1013 accepts nested cases named by type plus a discard arm")]
    public Task AcceptsNestedCasesNamedByTypePlusADiscard() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record EntryKind
            {
                public sealed record Asset : EntryKind;

                public sealed record Closing : EntryKind;

                public sealed record Cost : EntryKind;
            }

            public sealed class EntryRouter
            {
                public string Describe(EntryKind kind) => kind switch
                {
                    EntryKind.Asset => "asset",
                    EntryKind.Closing => "closing",
                    EntryKind.Cost => "cost",
                    _ => throw new System.InvalidOperationException(),
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 reports a nested case missing from type-only arms.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS1013 reports a nested case missing from type-only arms")]
    public Task ReportsANestedCaseMissingFromTypeOnlyArms() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record EntryKind
            {
                public sealed record Asset : EntryKind;

                public sealed record Closing : EntryKind;

                public sealed record Cost : EntryKind;
            }

            public sealed class EntryRouter
            {
                public string Describe(EntryKind kind) => kind switch
                {
                    EntryKind.Asset => "asset",
                    EntryKind.Closing => "closing",
                    {|LS1013:_|} => throw new System.InvalidOperationException(),
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 accepts nested cases grouped with or.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS1013 accepts nested cases grouped with or")]
    public Task AcceptsNestedCasesGroupedWithOr() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record EntryKind
            {
                public sealed record Asset : EntryKind;

                public sealed record Closing : EntryKind;

                public sealed record Cost : EntryKind;
            }

            public sealed class EntryRouter
            {
                public string Describe(EntryKind kind) => kind switch
                {
                    EntryKind.Asset or EntryKind.Closing => "grouped",
                    EntryKind.Cost => "cost",
                    _ => throw new System.InvalidOperationException(),
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 accepts a case handled through an abstract intermediate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 accepts a case handled through an abstract intermediate")]
    public Task AcceptsACaseHandledThroughAnAbstractIntermediate() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public abstract record OrderOpenState : OrderState;

            public sealed record OrderDraftState : OrderOpenState;

            public sealed record OrderPlacedState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderOpenState open => "open",
                    OrderPlacedState placed => "placed",
                    _ => "other",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 reports a case handled only by a guarded arm.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 reports a case handled only by a guarded arm")]
    public Task ReportsACaseHandledOnlyByAGuardedArm() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState(string Note) : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderDraftState draft when draft.Note == "urgent" => "urgent",
                    OrderPlacedState placed => "placed",
                    {|LS1013:_|} => "other",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 reports a state case left to the default label.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 reports a state case left to the default label")]
    public Task ReportsAStateCaseLeftToTheDefaultLabel() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed record OrderCancelledState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state)
                {
                    switch (state)
                    {
                        case OrderDraftState draft:
                            return "draft";
                        case OrderPlacedState placed:
                            return "placed";
                        {|LS1013:default:|}
                            return "other";
                    }

                    return "other";
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 accepts a switch statement that handles every sealed case.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 accepts a switch statement that handles every sealed case")]
    public Task AcceptsASwitchStatementThatHandlesEverySealedCase() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed record OrderCancelledState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state)
                {
                    switch (state)
                    {
                        case OrderDraftState draft:
                            return "draft";
                        case OrderPlacedState placed:
                            return "placed";
                        case OrderCancelledState cancelled:
                            return "cancelled";
                        default:
                            return "other";
                    }

                    return "other";
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 ignores a switch without a discard arm.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 ignores a switch without a discard arm")]
    public Task IgnoresASwitchWithoutADiscardArm() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed record OrderCancelledState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderDraftState draft => "draft",
                    OrderPlacedState placed => "placed",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 ignores a switch over a type that is not a record hierarchy.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS1013 ignores a switch over a type that is not a record hierarchy")]
    public Task IgnoresASwitchOverATypeThatIsNotARecordHierarchy() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public sealed class OrderRouter
            {
                public int Rank(string value) => value switch
                {
                    "draft" => 1,
                    _ => 0,
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 ignores an open hierarchy with an unsealed case.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 ignores an open hierarchy with an unsealed case")]
    public Task IgnoresAnOpenHierarchyWithAnUnsealedCase() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderPlacedState placed => "placed",
                    _ => "other",
                };
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1013 ignores a switch outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</proves>
    [Fact(DisplayName = "LS1013 ignores a switch outside Domain")]
    public Task IgnoresASwitchOutsideDomain() =>
        Verify.AnalyzerAsync<ExhaustiveSwitchAnalyzer>("""
            namespace Shop.Orders;

            public abstract record OrderState;

            public sealed record OrderDraftState : OrderState;

            public sealed record OrderPlacedState : OrderState;

            public sealed class OrderRouter
            {
                public string Describe(OrderState state) => state switch
                {
                    OrderDraftState draft => "draft",
                    _ => "other",
                };
            }
            """, assemblyName: "Shop.WebApi");

}
