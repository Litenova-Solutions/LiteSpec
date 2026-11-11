using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the backend reader collects the handler shape facts: aggregate calls and methods, message types, dependency counts, collaborators, loop reads, memory filters and audit actions.</summary>
public sealed class DotnetHandlerShapeReaderTests
{
    /// <summary>Handler Call On An Aggregate Local Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerCallOnAnAggregateLocalIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteDomainRoot(temp.Path);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            /// <implements>use-case/lending.void-order</implements>
            public sealed class VoidOrderCommandHandler(IOrderRepository orders) : ICommandHandler<VoidOrderCommand>
            {
                public async Task HandleAsync(VoidOrderCommand command)
                {
                    Order order = await orders.GetAsync(command.OrderId);
                    order.Abandon(command.Reason);
                }
            }
            """);

        var call = Assert.Single(Read(temp.Path).Handlers.Single().AggregateCalls!);

        Assert.Equal("order", call.Receiver);
        Assert.Equal("Abandon", call.Method);
        Assert.Equal(9, call.Line);
    }

    /// <summary>Handler Call On A Loaded Var Resolves Through The Repository.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerCallOnALoadedVarResolvesThroughTheRepository()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteDomainRoot(temp.Path);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            /// <implements>use-case/lending.void-order</implements>
            public sealed class VoidOrderCommandHandler(IOrderRepository orders) : ICommandHandler<VoidOrderCommand>
            {
                public async Task HandleAsync(VoidOrderCommand command)
                {
                    var order = await orders.GetAsync(command.OrderId);
                    order.Abandon(command.Reason);
                }
            }
            """);

        var call = Assert.Single(Read(temp.Path).Handlers.Single().AggregateCalls!);

        Assert.Equal("Abandon", call.Method);
    }

    /// <summary>Static Calls And Non Root Types Are Not Aggregate Calls.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void StaticCallsAndNonRootTypesAreNotAggregateCalls()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Order.cs", """
            namespace Library.Domain.Lending.Loans;

            public sealed class Order
            {
                public static Order Submit() => new();

                public void Abandon(string reason)
                {
                }
            }
            """);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            /// <implements>use-case/lending.void-order</implements>
            public sealed class VoidOrderCommandHandler : ICommandHandler<VoidOrderCommand>
            {
                public Task HandleAsync(VoidOrderCommand command)
                {
                    var order = Order.Submit();
                    order.Abandon(command.Reason);
                    return Task.CompletedTask;
                }
            }
            """);

        Assert.Empty(Read(temp.Path).Handlers.Single().AggregateCalls!);
    }

    /// <summary>Aggregate Methods Carry The Events They Raise.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void AggregateMethodsCarryTheEventsTheyRaise()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Order.cs", """
            namespace Library.Domain.Lending.Loans;

            public sealed class Order : AggregateRoot<OrderId>
            {
                public void Abandon(string reason)
                {
                    RaiseDomainEvent(new OrderAbandonedEvent(reason));
                }

                public void Note(string text)
                {
                    Raise(new OrderNotedEvent(text));
                }

                public void Touch()
                {
                }

                private void Wipe()
                {
                    RaiseDomainEvent(new OrderWipedEvent());
                }
            }
            """);

        var methods = Read(temp.Path).AggregateMethods;

        Assert.Equal("OrderAbandonedEvent", Assert.Single(methods.Single(method => method.Method == "Abandon").Events));
        Assert.Empty(methods.Single(method => method.Method == "Note").Events);
        Assert.Empty(methods.Single(method => method.Method == "Touch").Events);
        Assert.DoesNotContain(methods, method => method.Method == "Wipe");
    }

    /// <summary>Methods Outside An Aggregate Root Are Ignored.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void MethodsOutsideAnAggregateRootAreIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Ledger.cs", """
            namespace Library.Domain.Lending.Loans;

            public sealed class Ledger
            {
                public void Post()
                {
                    RaiseDomainEvent(new LedgerPostedEvent());
                }
            }
            """);

        Assert.Empty(Read(temp.Path).AggregateMethods);
    }

    /// <summary>Handler Message Type And Dependency Count Are Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerMessageTypeAndDependencyCountAreCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            /// <implements>use-case/lending.void-order</implements>
            public sealed class VoidOrderCommandHandler(
                IOrderRepository orders,
                IClock clock,
                IValidator validator) : ICommandHandler<VoidOrderCommand>
            {
                public Task HandleAsync(VoidOrderCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        var handler = Read(temp.Path).Handlers.Single();

        Assert.Equal("VoidOrderCommand", handler.MessageType);
        Assert.Equal(3, handler.DependencyCount);
    }

    /// <summary>Event Handler Dependency Count Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void EventHandlerDependencyCountIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            public sealed class NotifyReaderOnCopyBorrowedHandler(IOrderRepository orders, IClock clock)
                : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        Assert.Equal(2, Read(temp.Path).Reactions.Single().DependencyCount);
    }

    /// <summary>Collaborator Built With New And Holding A Repository Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void CollaboratorBuiltWithNewAndHoldingARepositoryIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "OrderEraser.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            public sealed class OrderEraser(IOrderRepository orders)
            {
                public void Erase(Order order)
                {
                }
            }
            """);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            /// <implements>use-case/lending.void-order</implements>
            public sealed class VoidOrderCommandHandler(IOrderRepository orders) : ICommandHandler<VoidOrderCommand>
            {
                public Task HandleAsync(VoidOrderCommand command)
                {
                    var eraser = new OrderEraser(orders);
                    eraser.Erase(null!);
                    return Task.CompletedTask;
                }
            }
            """);

        var collaborator = Assert.Single(Read(temp.Path).Handlers.Single().Collaborators!);

        Assert.Equal("OrderEraser", collaborator.Type);
        Assert.Equal(8, collaborator.Line);
    }

    /// <summary>Collaborator Without A Repository Is Ignored.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void CollaboratorWithoutARepositoryIsIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            /// <implements>use-case/lending.void-order</implements>
            public sealed class VoidOrderCommandHandler : ICommandHandler<VoidOrderCommand>
            {
                public Task HandleAsync(VoidOrderCommand command)
                {
                    return Task.FromResult(new VoidOrderCommandResult(command.OrderId));
                }
            }
            """);

        Assert.Empty(Read(temp.Path).Handlers.Single().Collaborators!);
    }

    /// <summary>Repository Read Inside A Loop Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void RepositoryReadInsideALoopIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("RemindBorrowers"), "RemindBorrowersCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.RemindBorrowers;

            /// <implements>use-case/lending.remind-borrowers</implements>
            public sealed class RemindBorrowersCommandHandler(IOrderRepository orders) : ICommandHandler<RemindBorrowersCommand>
            {
                public async Task HandleAsync(RemindBorrowersCommand command)
                {
                    var first = await orders.GetAsync(command.FirstId);
                    foreach (var id in command.RestIds)
                    {
                        var order = await orders.FindByIdAsync(id);
                    }
                }
            }
            """);

        var read = Assert.Single(Read(temp.Path).Handlers.Single().LoopReads!);

        Assert.Equal("IOrderRepository", read.Repository);
        Assert.Equal("FindByIdAsync", read.Method);
        Assert.Equal(11, read.Line);
    }

    /// <summary>Query Filter On A Repository List Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void QueryFilterOnARepositoryListIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("ListLoans"), "ListLoansQueryHandler.cs", """
            namespace Library.Application.Lending.Loans.ListLoans;

            /// <implements>use-case/lending.list-loans</implements>
            public sealed class ListLoansQueryHandler(ILoanRepository loans) : IQueryHandler<ListLoansQuery>
            {
                public async Task HandleAsync(ListLoansQuery query)
                {
                    var found = await loans.ListAsync();
                    var open = found.Where(loan => loan.Open).Take(10).ToList();
                }
            }
            """);

        Assert.Single(Read(temp.Path).Handlers.Single().MemoryFilters!);
    }

    /// <summary>Query Filter On A Session Query Is Ignored.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void QueryFilterOnASessionQueryIsIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("ListLoans"), "ListLoansQueryHandler.cs", """
            namespace Library.Application.Lending.Loans.ListLoans;

            /// <implements>use-case/lending.list-loans</implements>
            public sealed class ListLoansQueryHandler(IQuerySession session) : IQueryHandler<ListLoansQuery>
            {
                public async Task HandleAsync(ListLoansQuery query)
                {
                    var found = await session.Query<LoanRow>().Where(row => row.Open).ToListAsync();
                }
            }
            """);

        Assert.Empty(Read(temp.Path).Handlers.Single().MemoryFilters!);
    }

    /// <summary>Command Filter On A Repository List Is Ignored.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void CommandFilterOnARepositoryListIsIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            /// <implements>use-case/lending.void-order</implements>
            public sealed class VoidOrderCommandHandler(IOrderRepository orders) : ICommandHandler<VoidOrderCommand>
            {
                public async Task HandleAsync(VoidOrderCommand command)
                {
                    var found = await orders.ListAsync();
                    var open = found.Where(order => order.Open).ToList();
                }
            }
            """);

        Assert.Empty(Read(temp.Path).Handlers.Single().MemoryFilters!);
    }

    /// <summary>Audited Action Literal Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void AuditedActionLiteralIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, HandlerFolder("VoidOrder"), "VoidOrderCommand.cs", """
            namespace Library.Application.Lending.Loans.VoidOrder;

            [Audited("void-order")]
            public sealed record VoidOrderCommand(Guid OrderId);
            """);

        var action = Assert.Single(Read(temp.Path).AuditActions);

        Assert.Equal("VoidOrder", action.Operation);
        Assert.Equal("void-order", action.Action);
    }

    /// <summary>State Changing Method Without An Event Is An Aggregate Method Element.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void StateChangingMethodWithoutAnEventIsAnAggregateMethodElement()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Order.cs", """
            namespace Library.Domain.Lending.Loans;

            public sealed class Order : AggregateRoot<OrderId>
            {
                private bool erased;

                public void Erase()
                {
                    erased = true;
                }

                public void Touch()
                {
                }
            }
            """);

        var element = Assert.Single(Read(temp.Path).Elements, found => found.Kind == CodeKind.AggregateMethod);

        Assert.Equal("Order", element.Class);
        Assert.Equal("Erase", element.Member);
        Assert.Empty(element.Marks);
    }

    static CodeFacts Read(string root) => new DotnetBackendReader().Read(ConsumerConfig.Load(root));

    static void WriteDomainRoot(string root) =>
        WriteSource(root, "Domain", "Order.cs", """
            namespace Library.Domain.Lending.Loans;

            public sealed class Order : AggregateRoot<OrderId>
            {
                public void Abandon(string reason)
                {
                }
            }
            """);

    static string HandlerFolder(string operation) => Path.Combine("App", "Lending", "Loans", operation);

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
