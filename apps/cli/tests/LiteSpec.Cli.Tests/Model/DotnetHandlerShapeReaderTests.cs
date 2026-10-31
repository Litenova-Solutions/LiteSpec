using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the backend reader collects the handler shape facts: aggregate calls and methods, message types, dependency counts, collaborators, loop reads, memory filters and audit actions.</summary>
public sealed class DotnetHandlerShapeReaderTests
{
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Handler_Call_On_An_Aggregate_Local_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Handler_Call_On_A_Loaded_Var_Resolves_Through_The_Repository()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Static_Calls_And_Non_Root_Types_Are_Not_Aggregate_Calls()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Aggregate_Methods_Carry_The_Events_They_Raise()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Methods_Outside_An_Aggregate_Root_Are_Ignored()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Handler_Message_Type_And_Dependency_Count_Are_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Event_Handler_Dependency_Count_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            public sealed class NotifyReaderOnCopyBorrowedHandler(IOrderRepository orders, IClock clock)
                : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        Assert.Equal(2, Read(temp.Path).Automations.Single().DependencyCount);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Collaborator_Built_With_New_And_Holding_A_Repository_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Collaborator_Without_A_Repository_Is_Ignored()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Repository_Read_Inside_A_Loop_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Query_Filter_On_A_Repository_List_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Query_Filter_On_A_Session_Query_Is_Ignored()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Command_Filter_On_A_Repository_List_Is_Ignored()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Audited_Action_Literal_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    static CodeModel Read(string root) => new DotnetBackendReader().Read(ConsumerConfig.Load(root));

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
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "application": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
