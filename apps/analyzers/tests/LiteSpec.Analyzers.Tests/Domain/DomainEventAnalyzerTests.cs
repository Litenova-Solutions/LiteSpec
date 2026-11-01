using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1007, domain events.</summary>
public sealed class DomainEventAnalyzerTests
{
    /// <summary>LS1007 accepts an immutable record event.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 accepts an immutable record event")]
    public Task AcceptsImmutableRecordEvent() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            using System;

            namespace Shop.Orders;

            public interface IDomainEvent;

            public readonly record struct OrderId(Guid Value);

            public sealed record OrderPlacedEvent(OrderId OrderId) : IDomainEvent;
            """);

    /// <summary>LS1007 reports a class event.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 reports a class event")]
    public Task ReportsClassEvent() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed class {|LS1007:OrderPlacedEvent|} : IDomainEvent
            {
            }
            """);

    /// <summary>LS1007 reports an event with a settable property.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 reports an event with a settable property")]
    public Task ReportsSettableProperty() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed record {|LS1007:OrderPlacedEvent|} : IDomainEvent
            {
                public string Note { get; set; } = string.Empty;
            }
            """);

    /// <summary>LS1007 reports an event with a name lacking the suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 reports an event with a name lacking the suffix")]
    public Task ReportsMisnamedEvent() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed record {|LS1007:OrderPlaced|}(string Note) : IDomainEvent;
            """);

    /// <summary>LS1007 accepts an event with init, readonly and const members.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 accepts an event with init, readonly and const members")]
    public Task AcceptsInitReadonlyAndConst() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed record OrderPlacedEvent : IDomainEvent
            {
                public const int Version = 1;

                private readonly int _version = Version;

                public string Note { get; init; } = string.Empty;
            }
            """);

    /// <summary>LS1007 ignores an interface deriving IDomainEvent.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 ignores an interface deriving IDomainEvent")]
    public Task IgnoresDerivedInterface() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public interface IOrderEvent : IDomainEvent;
            """);

    /// <summary>LS1007 reports an event with a mutable field.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 reports an event with a mutable field")]
    public Task ReportsMutableField() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed record {|LS1007:OrderPlacedEvent|} : IDomainEvent
            {
                private int _attempts;
            }
            """);

    /// <summary>LS1007 ignores an event outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 ignores an event outside Domain")]
    public Task IgnoresEventOutsideDomain() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed class OrderPlacedEvent : IDomainEvent
            {
                public string Note { get; set; } = string.Empty;
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS1007 follows the LiteSpecLayer property over the assembly name.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Core;

            public interface IDomainEvent;

            public sealed class {|LS1007:OrderPlacedEvent|} : IDomainEvent
            {
            }
            """, assemblyName: "Shop.Core", layer: "Domain");

    /// <summary>LS1007 reports a multiply breaching event once.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 reports a multiply breaching event once")]
    public Task ReportsMultipleBreachesOnce() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed class {|LS1007:OrderPlaced|} : IDomainEvent
            {
                public string Note { get; set; } = string.Empty;
            }
            """);

    /// <summary>LS1007 reports a record struct event.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#raise-immutable-domain-facts</proves>
    [Fact(DisplayName = "LS1007 reports a record struct event")]
    public Task ReportsRecordStructEvent() =>
        Verify.AnalyzerAsync<DomainEventAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public readonly record struct {|LS1007:OrderPlacedEvent|}(string Note) : IDomainEvent;
            """);
}
