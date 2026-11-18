using LiteSpec.Analyzers.Rules.Layers.Naming;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2031, event kind suffixes.</summary>
public sealed class EventNamingAnalyzerTests
{
    /// <summary>LS2031 reports a domain event without its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 reports a domain event without its suffix")]
    public Task ReportsADomainEventWithoutItsSuffix() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed record {|LS2031:OrderPlacedEvent|}(int OrderId) : IDomainEvent;
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS2031 accepts a domain event with its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 accepts a domain event with its suffix")]
    public Task AcceptsADomainEventWithItsSuffix() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed record OrderPlacedDomainEvent(int OrderId) : IDomainEvent;
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS2031 reports a module event without its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 reports a module event without its suffix")]
    public Task ReportsAModuleEventWithoutItsSuffix() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders.Contracts.Orders.Events;

            /// <implements>module-event/orders.order-placed</implements>
            public sealed record {|LS2031:OrderPlaced|}(int OrderId);
            """, path: "Shop.Orders/Contracts/Orders/Events/OrderPlaced.cs");

    /// <summary>LS2031 accepts a module event with its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 accepts a module event with its suffix")]
    public Task AcceptsAModuleEventWithItsSuffix() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders.Contracts.Orders.Events;

            /// <implements>module-event/orders.order-placed</implements>
            public sealed record OrderPlacedModuleEvent(int OrderId);
            """, path: "Shop.Orders/Contracts/Orders/Events/OrderPlacedModuleEvent.cs");

    /// <summary>LS2031 reports an integration event without its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 reports an integration event without its suffix")]
    public Task ReportsAnIntegrationEventWithoutItsSuffix() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders.Contracts.Orders.Events;

            /// <implements>integration-event/order-placed</implements>
            public sealed record {|LS2031:OrderPlaced|}(int OrderId);
            """, path: "Shop.Orders/Contracts/Orders/Events/OrderPlaced.cs");

    /// <summary>LS2031 accepts an integration event with its suffix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 accepts an integration event with its suffix")]
    public Task AcceptsAnIntegrationEventWithItsSuffix() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders.Contracts.Orders.Events;

            /// <implements>integration-event/order-placed</implements>
            public sealed record OrderPlacedIntegrationEvent(int OrderId);
            """, path: "Shop.Orders/Contracts/Orders/Events/OrderPlacedIntegrationEvent.cs");

    /// <summary>LS2031 reports a module event in a sibling Contracts project.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 reports a module event in a sibling Contracts project")]
    public Task ReportsAModuleEventInASiblingContractsProject() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders.Contracts.Orders.Events;

            /// <implements>module-event/orders.order-placed</implements>
            public sealed record {|LS2031:OrderPlaced|}(int OrderId);
            """, assemblyName: "Shop.Orders.Contracts", path: "Shop.Orders.Contracts/Orders/Events/OrderPlaced.cs");

    /// <summary>LS2031 ignores a Contracts type with no event mark.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 ignores a Contracts type with no event mark")]
    public Task IgnoresAContractsTypeWithNoEventMark() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders.Contracts.Orders;

            public sealed record OrderPlaced(int OrderId);
            """, path: "Shop.Orders/Contracts/Orders/OrderPlaced.cs");

    /// <summary>LS2031 ignores a domain event marker outside Domain and Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 ignores a domain event marker outside Domain and Contracts")]
    public Task IgnoresADomainEventMarkerOutsideDomainAndContracts() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public sealed record OrderPlacedEvent(int OrderId) : IDomainEvent;
            """, path: "Shop.Spending/Application/Source.cs");

    /// <summary>LS2031 ignores an interface extending the domain event marker.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/naming#name-every-event-type-by-its-kind</proves>
    [Fact(DisplayName = "LS2031 ignores an interface extending the domain event marker")]
    public Task IgnoresAnInterface() =>
        Verify.AnalyzerAsync<EventNamingAnalyzer>("""
            namespace Shop.Orders;

            public interface IDomainEvent;

            public interface IOrderEvent : IDomainEvent;
            """, path: "Shop.Spending/Domain/Source.cs");
}
