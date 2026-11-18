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
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/PlaceOrder/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders.Application.PlaceOrder;

                    internal sealed class PlaceOrderCommandHandler({|LS2021:ICommandMediator|} mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 reports a query handler taking the query mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a query handler taking the query mediator")]
    public Task ReportsAQueryHandlerTakingTheQueryMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/ListOrders/ListOrdersQueryHandler.cs", """
                    using LiteBus.Queries.Abstractions;

                    namespace Shop.Orders.Application.ListOrders;

                    internal sealed class ListOrdersQueryHandler({|LS2021:IQueryMediator|} queries)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 reports a validator taking the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a validator taking the command mediator")]
    public Task ReportsAValidatorTakingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/PlaceOrder/PlaceOrderValidator.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders.Application.PlaceOrder;

                    internal sealed class PlaceOrderValidator({|LS2021:ICommandMediator|} mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 reports a command handler holding the event mediator in a field.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a command handler holding the event mediator in a field")]
    public Task ReportsAFieldHoldingTheEventMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/PlaceOrder/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders.Application.PlaceOrder;

                    internal sealed class PlaceOrderCommandHandler
                    {
                        private readonly {|LS2021:IEventMediator|} _events = null!;
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 reports a property of a non-entry type holding the query mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 reports a property of a non-entry type holding the query mediator")]
    public Task ReportsAPropertyHoldingTheQueryMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/ListOrders/ListOrdersQueryHandler.cs", """
                    using LiteBus.Queries.Abstractions;

                    namespace Shop.Orders.Application.ListOrders;

                    internal sealed class ListOrdersQueryHandler
                    {
                        public {|LS2021:IQueryMediator|} Queries { get; } = null!;
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 accepts a reaction taking the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a reaction taking the command mediator")]
    public Task AcceptsAReactionTakingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/SubmitPayment/SubmitPaymentOnOrderPlacedHandler.cs", """
                    using LiteBus.Commands.Abstractions;
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders.Application.SubmitPayment;

                    public sealed record OrderPlacedEvent;

                    internal sealed class SubmitPaymentOnOrderPlacedHandler(ICommandMediator mediator) : IEventHandler<OrderPlacedEvent>
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 accepts an endpoint taking the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts an endpoint taking the command mediator")]
    public Task AcceptsAnEndpointTakingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/PlaceOrder/PlaceOrderEndpoint.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders.Application.PlaceOrder;

                    internal sealed class PlaceOrderEndpoint(ICommandMediator mediator)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 accepts a job deriving from BackgroundService that holds the command mediator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a job deriving from BackgroundService that holds the command mediator")]
    public Task AcceptsAJobHoldingTheCommandMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/ExpireOrders/ExpiredOrderSweeper.cs", """
                    using LiteBus.Commands.Abstractions;
                    using Microsoft.Extensions.Hosting;

                    namespace Shop.Orders.Application.ExpireOrders;

                    internal sealed class ExpiredOrderSweeper : BackgroundService
                    {
                        private readonly ICommandMediator _mediator = null!;
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    /// <summary>LS2021 accepts a method parameter that is not a constructor parameter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/application#keep-command-handlers-to-load-call-and-stage</proves>
    [Fact(DisplayName = "LS2021 accepts a method parameter that is not a constructor parameter")]
    public Task AcceptsAMethodParameterThatIsNotAConstructorParameter() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Application/PlaceOrder/PlaceOrderCommandHandler.cs", """
                    using LiteBus.Commands.Abstractions;

                    namespace Shop.Orders.Application.PlaceOrder;

                    internal sealed class PlaceOrderCommandHandler
                    {
                        public void Handle(ICommandMediator mediator)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

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

    [Fact(DisplayName = "LS2021 accepts a provider adapter holding the query and command mediators")]
    public Task AcceptsAProviderAdapterHoldingTheQueryAndCommandMediators() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Billing.Contracts/Invoices/ReadInvoice/ReadInvoiceQuery.cs", """
                    namespace Shop.Billing.Contracts.Invoices.ReadInvoice;

                    public sealed record ReadInvoiceQuery;
                    """),
                ("Shop.Orders/Infrastructure/Billing/BillingAdapter.cs", """
                    using LiteBus.Commands.Abstractions;
                    using LiteBus.Queries.Abstractions;

                    namespace Shop.Orders.Infrastructure.Billing;

                    internal sealed class BillingAdapter(IQueryMediator queries, ICommandMediator commands)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    [Fact(DisplayName = "LS2021 reports a port adapter holding the query mediator")]
    public Task ReportsAPortAdapterHoldingTheQueryMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Orders/Infrastructure/PaymentGateway/PaymentGatewayAdapter.cs", """
                    using LiteBus.Queries.Abstractions;

                    namespace Shop.Orders.Infrastructure.PaymentGateway;

                    internal sealed class PaymentGatewayAdapter({|LS2021:IQueryMediator|} queries)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");

    [Fact(DisplayName = "LS2021 reports a provider adapter holding the event mediator")]
    public Task ReportsAProviderAdapterHoldingTheEventMediator() =>
        Verify.AnalyzerAsync<MessageSentOutsideEntryPointAnalyzer>(
            [
                ("Shop.Orders.Contracts/Stubs.cs", Stubs),
                ("Shop.Billing.Contracts/Invoices/ReadInvoice/ReadInvoiceQuery.cs", """
                    namespace Shop.Billing.Contracts.Invoices.ReadInvoice;

                    public sealed record ReadInvoiceQuery;
                    """),
                ("Shop.Orders/Infrastructure/Billing/BillingAdapter.cs", """
                    using LiteBus.Events.Abstractions;

                    namespace Shop.Orders.Infrastructure.Billing;

                    internal sealed class BillingAdapter({|LS2021:IEventMediator|} events)
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Orders");
}
