using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2022, the event published by user code rule.</summary>
public sealed class EventPublishedByUserCodeAnalyzerTests
{
    /// <summary>The mediator and entry point base types the test sources name, declared in one file of each project.</summary>
    private const string Stubs = """
        namespace LiteBus.Commands.Abstractions
        {
            public interface ICommandMediator { }
        }

        namespace LiteBus.Events.Abstractions
        {
            public interface IEventMediator { }

            public interface IEventHandler<TEvent> { }
        }

        namespace Microsoft.Extensions.Hosting
        {
            public abstract class BackgroundService { }
        }
        """;

    /// <summary>LS2022 reports a reaction holding the event mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 reports a reaction holding the event mediator")]
    public Task ReportsAReactionHoldingTheEventMediator() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/SubmitPayment/Application/SubmitPaymentOnOrderPlacedHandler.cs", """
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders;

                    public sealed record OrderPlacedEvent;

                    internal sealed class SubmitPaymentOnOrderPlacedHandler({|LS2022:IEventMediator|} events) : IEventHandler<OrderPlacedEvent>
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2022 reports a command handler holding the event mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 reports a command handler holding the event mediator")]
    public Task ReportsACommandHandlerHoldingTheEventMediator() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderCommandHandler({|LS2022:IEventMediator|} events)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2022 reports a job publishing through the event mediator, entry points included.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 reports a job publishing through the event mediator")]
    public Task ReportsAJobHoldingTheEventMediator() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/ExpireOrders/Application/ExpiredOrderSweeper.cs", """
                    using LiteBus.Events.Abstractions;
                    using Microsoft.Extensions.Hosting;

                    namespace Shop.Orders;

                    internal sealed class ExpiredOrderSweeper : BackgroundService
                    {
                        private readonly {|LS2022:IEventMediator|} _events = null!;
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2022 reports a named event publisher type that user code takes.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 reports a named event publisher type that user code takes")]
    public Task ReportsAnEventPublisherTakenByUserCode() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    namespace Shop.Orders;

                    internal interface ExpenseEventPublisher
                    {
                    }

                    internal sealed class PlaceOrderCommandHandler({|LS2022:ExpenseEventPublisher|} publisher)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2022 accepts a use case that raises a domain event and holds no event mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 accepts a use case that raises a domain event and holds no event mediator")]
    public Task AcceptsAUseCaseHoldingNoEventMediator() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders;

                    public interface IOrderRepository
                    {
                        void Store(object order);
                    }

                    internal sealed class PlaceOrderCommandHandler(IOrderRepository orders)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2022 accepts the event mediator in the shared kernel, which holds the publishing pipeline.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 accepts the event mediator in the shared kernel")]
    public Task AcceptsTheEventMediatorInTheSharedKernel() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.SharedKernel/Stubs.cs", Stubs),
                ("Shop.SharedKernel/Application/Pipeline/PublishOutbox.cs", """
                    using LiteBus.Events.Abstractions;

                    namespace Shop.SharedKernel.Pipeline;

                    internal sealed class PublishOutbox(IEventMediator events)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.SharedKernel");

    /// <summary>LS2022 stays quiet for a name written only in a documentation comment.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 stays quiet for a name written only in a documentation comment")]
    public Task StaysQuietInADocumentationComment() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    namespace Shop.Orders;

                    /// <summary>Raises the order placed event; <see cref="LiteBus.Events.Abstractions.IEventMediator"/> publishes it.</summary>
                    internal sealed class PlaceOrderCommandHandler
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2022 stays quiet in test projects.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</proves>
    [Fact(DisplayName = "LS2022 stays quiet in test projects")]
    public Task StaysQuietInTestProjects() =>
        Verify.AnalyzerAsync<EventPublishedByUserCodeAnalyzer>(
            [
                ("Shop.Tests/Stubs.cs", Stubs),
                ("Shop.Tests/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderCommandHandler(IEventMediator events)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Tests");
}
