using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2021, the message sent outside an entry point rule.</summary>
public sealed class MessageSentOutsideEntryPointAnalyzerTests
{
    /// <summary>The mediator and entry point base types the test sources name, declared in one file of each project.</summary>
    private const string Stubs = """
        namespace LiteBus.Commands.Abstractions
        {
            public interface ICommandMediator { }
        }

        namespace LiteBus.Queries.Abstractions
        {
            public interface IQueryMediator { }
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

    /// <summary>LS2021 reports a command handler taking the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a command handler taking the command mediator")]
    public Task ReportsACommandHandlerTakingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderCommandHandler({|LS2021:ICommandMediator|} mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 reports a query handler taking the query mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a query handler taking the query mediator")]
    public Task ReportsAQueryHandlerTakingTheQueryMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/ListOrders/Application/ListOrdersQueryHandler.cs", """
                    using LiteBus.Queries.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class ListOrdersQueryHandler({|LS2021:IQueryMediator|} queries)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 reports a validator taking the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a validator taking the command mediator")]
    public Task ReportsAValidatorTakingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderValidator.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderValidator({|LS2021:ICommandMediator|} mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 reports a command handler holding the event mediator in a field.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a command handler holding the event mediator in a field")]
    public Task ReportsAFieldHoldingTheEventMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderCommandHandler
                    {
                        private readonly {|LS2021:IEventMediator|} _events = null!;
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 reports a property of a non-entry type holding the query mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a property of a non-entry type holding the query mediator")]
    public Task ReportsAPropertyHoldingTheQueryMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/ListOrders/Application/ListOrdersQueryHandler.cs", """
                    using LiteBus.Queries.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class ListOrdersQueryHandler
                    {
                        public {|LS2021:IQueryMediator|} Queries { get; } = null!;
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 accepts a reaction taking the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a reaction taking the command mediator")]
    public Task AcceptsAReactionTakingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/SubmitPayment/Application/SubmitPaymentOnOrderPlacedHandler.cs", """
                    using LiteBus.Commands.Abstractions;
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders;

                    public sealed record OrderPlacedEvent;

                    internal sealed class SubmitPaymentOnOrderPlacedHandler(ICommandMediator mediator) : IEventHandler<OrderPlacedEvent>
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 accepts an endpoint taking the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts an endpoint taking the command mediator")]
    public Task AcceptsAnEndpointTakingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderEndpoint.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderEndpoint(ICommandMediator mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 accepts a job deriving from BackgroundService that holds the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a job deriving from BackgroundService that holds the command mediator")]
    public Task AcceptsAJobHoldingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/ExpireOrders/Application/ExpiredOrderSweeper.cs", """
                    using LiteBus.Commands.Abstractions;
                    using Microsoft.Extensions.Hosting;

                    namespace Shop.Orders;

                    internal sealed class ExpiredOrderSweeper : BackgroundService
                    {
                        private readonly ICommandMediator _mediator = null!;
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 accepts a method parameter that is not a constructor parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a method parameter that is not a constructor parameter")]
    public Task AcceptsAMethodParameterThatIsNotAConstructorParameter() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Application/Stubs.cs", Stubs),
                ("Shop.Application/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderCommandHandler
                    {
                        public void Handle(ICommandMediator mediator)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Shop.Application");

    /// <summary>LS2021 accepts a mediator in the shared kernel, which composes the pipeline.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a mediator in the shared kernel")]
    public Task AcceptsAMediatorInTheSharedKernel() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.SharedKernel/Stubs.cs", Stubs),
                ("Shop.SharedKernel/Application/Pipeline/PublishOutbox.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.SharedKernel.Pipeline;

                    internal sealed class PublishOutbox(ICommandMediator mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.SharedKernel");

    /// <summary>LS2021 stays quiet in test projects.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 stays quiet in test projects")]
    public Task StaysQuietInTestProjects() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Tests/Stubs.cs", Stubs),
                ("Shop.Tests/Orders/PlaceOrder/Application/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders;

                    internal sealed class PlaceOrderCommandHandler(ICommandMediator mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Tests");

    /// <summary>LS2021 accepts a mediator in the platform, which composes the pipeline.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a mediator in the platform")]
    public Task AcceptsAMediatorInThePlatform() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Platform/Stubs.cs", Stubs),
                ("Shop.Platform/Messaging/PublishOutbox.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Platform.Messaging;

                    internal sealed class PublishOutbox
                    {
                        private readonly ICommandMediator _mediator;

                        public PublishOutbox(ICommandMediator mediator)
                        {
                            _mediator = mediator;
                        }
                    }
                    """),
            ],
            assemblyName: "Shop.Platform");
}
