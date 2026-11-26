using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>
///     Proves the act check sees static factories: a factory marked with the use
///     case satisfies it, and an unmarked one reports use-case-act-missing.
/// </summary>
public sealed class FactoryActTests
{
    /// <summary>Static Factory Marked With The Use Case Satisfies The Act Check.</summary>
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void StaticFactoryMarkedWithTheUseCaseSatisfiesTheActCheck()
    {
        using var temp = WriteConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.submit-order");
    }

    /// <summary>Static Factory Without The Mark Reports Use Case Act Uncalled.</summary>
    /// <proves>litespec/core/backend-domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void StaticFactoryWithoutTheMarkReportsUseCaseActUncalled()
    {
        using var temp = WriteConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-missing");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.renew-order", finding.Subject);
        Assert.Contains("use-case/lending.renew-order", finding.Message);
    }

    /// <summary>Writes a consumer where one handler calls a marked static factory and one calls an unmarked one.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
                  "reviewedLiteSpecVersion": "7.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": ".litespec",
                    "behaviorDocs": ".litespec/behavior",
                    "glossary": ".litespec/glossary.yaml",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "modules": "apps/api/src/Modules/Library.Lending",
                      "webapi": "apps/api/src/Library.WebApi",
                    }
                  },
                  "vocabulary": {
                    "verbs": [
                      "submit",
                      "renew"
                    ]
                  }
                }
                """);
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/orders/aggregate.yaml", """
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.order",
                  "title": "Order",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "states": [
                    {
                      "name": "Placed",
                      "meaning": "The order is placed."
                    }
                  ]
                }
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/orders/domain-events/order-submitted/order-submitted.yaml", """
                {
                  "type": "domain-event",
                  "id": "domain-event/order.order-submitted",
                  "title": "Order submitted",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "meaning": "An order was submitted.",
                  "carries": [
                    "orderId"
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/orders/aggregate.md", """
                # Order

                An order is one purchase a reader placed.
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/orders/submit-order.yaml", """
                {
                  "type": "use-case",
                  "id": "use-case/lending.submit-order",
                  "title": "Submit order",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "raises": [
                    "domain-event/order.order-submitted"
                  ],
                  "writes": [
                    "aggregate/lending.order"
                  ],
                  "records": [
                    {
                      "type": "result",
                      "id": "result/submit-order.submitted",
                      "title": "Submitted",
                      "owner": "lending-team",
                      "outcome": "success",
                      "meaning": "The order is submitted."
                    }
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/orders/submit-order.md", """
                # Submit order

                A reader submits an order.
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/orders/renew-order.yaml", """
                {
                  "type": "use-case",
                  "id": "use-case/lending.renew-order",
                  "title": "Renew order",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "writes": [
                    "aggregate/lending.order"
                  ],
                  "records": [
                    {
                      "type": "result",
                      "id": "result/renew-order.renewed",
                      "title": "Renewed",
                      "owner": "lending-team",
                      "outcome": "success",
                      "meaning": "The order is renewed."
                    }
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/orders/renew-order.md", """
                # Renew order

                A reader renews an order.
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Orders/Order.cs", """
                namespace Library.Lending.Domain.Orders;

                /// <implements>aggregate/lending.order</implements>
                public sealed class Order : AggregateRoot<OrderId>
                {
                    public Guid Id { get; init; }

                    /// <implements>use-case/lending.submit-order</implements>
                    public static Order Submit(Guid orderId)
                    {
                        var order = new Order { Id = orderId };
                        order.RaiseDomainEvent(new OrderSubmittedEvent(orderId));
                        return order;
                    }

                    public static Order Renew(Guid orderId)
                    {
                        return new Order { Id = orderId };
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Domain/Orders/OrderSubmittedEvent.cs", """
                namespace Library.Lending.Domain.Orders;

                /// <implements>domain-event/order.order-submitted</implements>
                public sealed record OrderSubmittedEvent(Guid OrderId) : IDomainEvent;
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/SubmitOrder/SubmitOrderCommand.cs", """
                namespace Library.Lending.Application.SubmitOrder;

                public sealed record SubmitOrderCommand(Guid OrderId);

                public sealed record SubmitOrderCommandResult(Guid OrderId);
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/SubmitOrder/SubmitOrderCommandHandler.cs", """
                namespace Library.Lending.Application.SubmitOrder;

                /// <implements>use-case/lending.submit-order</implements>
                public sealed class SubmitOrderCommandHandler(IOrderRepository orders)
                    : ICommandHandler<SubmitOrderCommand, SubmitOrderCommandResult>
                {
                    public async Task<SubmitOrderCommandResult> HandleAsync(SubmitOrderCommand command, CancellationToken cancellationToken)
                    {
                        var order = Order.Submit(command.OrderId);
                        orders.Save(order);
                        return new SubmitOrderCommandResult(order.Id);
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/RenewOrder/RenewOrderCommand.cs", """
                namespace Library.Lending.Application.RenewOrder;

                public sealed record RenewOrderCommand(Guid OrderId);

                public sealed record RenewOrderCommandResult(Guid OrderId);
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/Application/RenewOrder/RenewOrderCommandHandler.cs", """
                namespace Library.Lending.Application.RenewOrder;

                /// <implements>use-case/lending.renew-order</implements>
                public sealed class RenewOrderCommandHandler(IOrderRepository orders)
                    : ICommandHandler<RenewOrderCommand, RenewOrderCommandResult>
                {
                    public async Task<RenewOrderCommandResult> HandleAsync(RenewOrderCommand command, CancellationToken cancellationToken)
                    {
                        var order = Order.Renew(command.OrderId);
                        orders.Save(order);
                        return new RenewOrderCommandResult(order.Id);
                    }
                }
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }
}
