using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>
///     Proves the act check sees static factories: a factory marked with the use
///     case satisfies it, and an unmarked one reports use-case-act-uncalled.
/// </summary>
public sealed class FactoryActTests
{
    [Fact]
    public void Static_Factory_Marked_With_The_Use_Case_Satisfies_The_Act_Check()
    {
        using var temp = WriteConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-uncalled");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.submit-order");
    }

    [Fact]
    public void Static_Factory_Without_The_Mark_Reports_Use_Case_Act_Uncalled()
    {
        using var temp = WriteConsumer();
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "use-case-act-uncalled");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.renew-order", finding.Subject);
        Assert.Contains("use-case/lending.renew-order", finding.Message);
    }

    /// <summary>Writes a consumer where one handler calls a marked static factory and one calls an unmarked one.</summary>
    static TempFolder WriteConsumer()
    {
        var temp = Cli.Temp("marks");

        try
        {
            Write(temp.Path, "litespec.project.json", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "reviewedLiteSpecVersion": "3.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": "docs",
                    "behaviorDocs": "docs/behavior",
                    "glossary": "docs/glossary.json",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "application": "apps/api/src/Library.Application",
                      "domain": "apps/api/src/Library.Domain",
                      "webapi": "apps/api/src/Library.WebApi",
                      "infrastructure": "apps/api/src/Library.Infrastructure"
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
            Write(temp.Path, "docs/glossary.json", """
                {
                  "schemaVersion": 2,
                  "terms": [],
                  "mannered": []
                }
                """);
            Write(temp.Path, "docs/behavior/modules/lending/orders/README.md", """
                ---
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
                  ],
                  "records": [
                    {
                      "type": "event",
                      "id": "event/order.order-submitted",
                      "title": "Order submitted",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "meaning": "An order was submitted.",
                      "carries": [
                        "orderId"
                      ]
                    }
                  ]
                }
                ---

                # Order

                An order is one purchase a reader placed.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/orders/submit-order.md", """
                ---
                {
                  "type": "use-case",
                  "id": "use-case/lending.submit-order",
                  "title": "Submit order",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "raises": [
                    "event/order.order-submitted"
                  ],
                  "changes": [
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
                ---

                # Submit order

                A reader submits an order.
                """);
            Write(temp.Path, "docs/behavior/modules/lending/orders/renew-order.md", """
                ---
                {
                  "type": "use-case",
                  "id": "use-case/lending.renew-order",
                  "title": "Renew order",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "kind": "command",
                  "changes": [
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
                ---

                # Renew order

                A reader renews an order.
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Orders/Order.cs", """
                namespace Library.Domain.Lending.Orders;

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
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Orders/OrderSubmittedEvent.cs", """
                namespace Library.Domain.Lending.Orders;

                /// <implements>event/order.order-submitted</implements>
                public sealed record OrderSubmittedEvent(Guid OrderId) : IDomainEvent;
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Orders/SubmitOrder/SubmitOrderCommand.cs", """
                namespace Library.Application.Lending.Orders.SubmitOrder;

                public sealed record SubmitOrderCommand(Guid OrderId);

                public sealed record SubmitOrderCommandResult(Guid OrderId);
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Orders/SubmitOrder/SubmitOrderCommandHandler.cs", """
                namespace Library.Application.Lending.Orders.SubmitOrder;

                /// <implements>use-case/lending.submit-order</implements>
                public sealed class SubmitOrderCommandHandler(IOrderRepository orders)
                    : ICommandHandler<SubmitOrderCommand, SubmitOrderCommandResult>
                {
                    public async Task<SubmitOrderCommandResult> HandleAsync(SubmitOrderCommand command, CancellationToken cancellationToken)
                    {
                        var order = Order.Submit(command.OrderId);
                        orders.Store(order);
                        return new SubmitOrderCommandResult(order.Id);
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Orders/RenewOrder/RenewOrderCommand.cs", """
                namespace Library.Application.Lending.Orders.RenewOrder;

                public sealed record RenewOrderCommand(Guid OrderId);

                public sealed record RenewOrderCommandResult(Guid OrderId);
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Orders/RenewOrder/RenewOrderCommandHandler.cs", """
                namespace Library.Application.Lending.Orders.RenewOrder;

                /// <implements>use-case/lending.renew-order</implements>
                public sealed class RenewOrderCommandHandler(IOrderRepository orders)
                    : ICommandHandler<RenewOrderCommand, RenewOrderCommandResult>
                {
                    public async Task<RenewOrderCommandResult> HandleAsync(RenewOrderCommand command, CancellationToken cancellationToken)
                    {
                        var order = Order.Renew(command.OrderId);
                        orders.Store(order);
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
