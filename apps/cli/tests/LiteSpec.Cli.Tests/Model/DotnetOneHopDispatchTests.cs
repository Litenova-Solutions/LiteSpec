using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves a receiver passed to a helper attributes the helper's dispatches one hop deep.</summary>
public sealed class DotnetOneHopDispatchTests
{
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Helper_Taking_The_Mediator_Attributes_Its_Dispatch_To_The_Handler()
    {
        using var temp = Cli.Temp("onehop");
        WriteProject(temp.Path);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs"), MediatorHandler);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "LoanFanout.cs"), MediatorHelper);

        var handler = Assert.Single(
            new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Handlers,
            found => found.UseCaseId == "use-case/lending.borrow-copy");
        var dispatch = Assert.Single(handler.Dispatches!);

        Assert.Equal("runs", dispatch.Verb);
        Assert.Equal("PurgeLoansCommand", dispatch.Target);
        Assert.Equal("LoanFanout.RetryAsync", dispatch.Via);
        Assert.EndsWith("BorrowCopyCommandHandler.cs", dispatch.File);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Helper_Taking_A_Publisher_Attributes_Only_The_Passed_Receiver()
    {
        using var temp = Cli.Temp("onehop");
        WriteProject(temp.Path);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "OnLoanBorrowed", "PublishEventOnLoanBorrowedHandler.cs"), PublisherAutomation);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "OnLoanBorrowed", "LoanEventRelay.cs"), PublisherHelper);

        var automation = Assert.Single(
            new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Automations,
            found => found.Class == "PublishEventOnLoanBorrowedHandler");
        var dispatch = Assert.Single(automation.Dispatches!);

        Assert.Equal("emits", dispatch.Verb);
        Assert.Equal("LoanBorrowedEvent", dispatch.Target);
        Assert.Equal("LoanEventRelay.RelayAsync", dispatch.Via);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Helper_Called_Without_A_Receiver_Attributes_Nothing()
    {
        using var temp = Cli.Temp("onehop");
        WriteProject(temp.Path);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs"), SilentHandler);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "LoanFanout.cs"), MediatorHelper);

        var handler = Assert.Single(
            new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Handlers,
            found => found.UseCaseId == "use-case/lending.borrow-copy");

        Assert.Empty(handler.Dispatches!);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Helper_Dispatch_Through_An_Unpassed_Receiver_Is_Not_Attributed()
    {
        using var temp = Cli.Temp("onehop");
        WriteProject(temp.Path);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs"), MediatorOnlyHandler);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "LoanFanout.cs"), TwoReceiverHelper);

        var handler = Assert.Single(
            new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Handlers,
            found => found.UseCaseId == "use-case/lending.borrow-copy");
        var dispatch = Assert.Single(handler.Dispatches!);

        Assert.Equal("runs", dispatch.Verb);
        Assert.Equal("PurgeLoansCommand", dispatch.Target);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Attribution_Stops_After_One_Hop()
    {
        using var temp = Cli.Temp("onehop");
        WriteProject(temp.Path);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs"), ChainedHandler);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "FirstHop.cs"), FirstHop);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "SecondHop.cs"), SecondHop);

        var handler = Assert.Single(
            new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Handlers,
            found => found.UseCaseId == "use-case/lending.borrow-copy");
        var dispatch = Assert.Single(handler.Dispatches!);

        Assert.Equal("PurgeLoansCommand", dispatch.Target);
        Assert.Equal("FirstHop.RunAsync", dispatch.Via);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Instance_Helper_Through_A_Held_Interface_Attributes_Its_Send()
    {
        using var temp = Cli.Temp("onehop");
        WriteProject(temp.Path);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs"), InstanceHandler);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "LoanFanout.cs"), InstanceHelper);

        var handler = Assert.Single(
            new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Handlers,
            found => found.UseCaseId == "use-case/lending.borrow-copy");
        var dispatch = Assert.Single(handler.Dispatches!);

        Assert.Equal("sends", dispatch.Verb);
        Assert.Equal("MemberInvitedNotification", dispatch.Target);
        Assert.Equal("LoanFanout.NotifyAsync", dispatch.Via);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Send_Of_A_Message_Built_From_A_Rendered_Template_Names_The_Template()
    {
        using var temp = Cli.Temp("onehop");
        WriteProject(temp.Path);
        WriteApp(temp.Path, Path.Combine("Lending", "Loans", "BorrowCopy", "BorrowCopyCommandHandler.cs"), RenderingHandler);

        var handler = Assert.Single(
            new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Handlers,
            found => found.UseCaseId == "use-case/lending.borrow-copy");
        var dispatch = Assert.Single(handler.Dispatches!);

        Assert.Equal("sends", dispatch.Verb);
        Assert.Equal("loan-borrowed-notice", dispatch.Target);
        Assert.Equal("template", dispatch.Detail);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "application": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static void WriteApp(string root, string relative, string text)
    {
        var path = Path.Combine(root, "src", "App", relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string RenderingHandler = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        /// <implements>use-case/lending.borrow-copy</implements>
        public sealed class BorrowCopyCommandHandler(IOrderMessenger messenger, ITemplateRenderer templates) : ICommandHandler<BorrowCopyCommand>
        {
            public async Task Handle(BorrowCopyCommand command, CancellationToken cancellationToken)
            {
                var rendered = templates.Render("loan-borrowed-notice", command.CopyId);
                await messenger.SendAsync(new OutboundMessage { Body = rendered.Body }, cancellationToken);
            }
        }
        """;

    const string MediatorHandler = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        /// <implements>use-case/lending.borrow-copy</implements>
        public sealed class BorrowCopyCommandHandler(ICommandMediator commands) : ICommandHandler<BorrowCopyCommand>
        {
            public Task Handle(BorrowCopyCommand command, CancellationToken cancellationToken)
            {
                return LoanFanout.RetryAsync(command.CopyId, commands, cancellationToken);
            }
        }
        """;

    const string MediatorHelper = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        internal static class LoanFanout
        {
            public static async Task RetryAsync(Guid copyId, ICommandMediator commands, CancellationToken cancellationToken)
            {
                await commands.SendAsync(new PurgeLoansCommand(), cancellationToken);
            }
        }
        """;

    const string PublisherAutomation = """
        namespace Library.Application.Lending.Loans.OnLoanBorrowed;

        public sealed class PublishEventOnLoanBorrowedHandler(IEventPublisher events) : IEventHandler<LoanBorrowedEvent>
        {
            public Task HandleAsync(LoanBorrowedEvent message, IOrderMessenger messenger)
            {
                return LoanEventRelay.RelayAsync(message, events);
            }
        }
        """;

    const string PublisherHelper = """
        namespace Library.Application.Lending.Loans.OnLoanBorrowed;

        internal static class LoanEventRelay
        {
            public static async Task RelayAsync(LoanBorrowedEvent message, IEventPublisher events, IOrderMessenger messenger)
            {
                await events.PublishAsync(new LoanBorrowedEvent());
                await messenger.SendAsync(new MemberInvitedNotification());
            }
        }
        """;

    const string SilentHandler = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        /// <implements>use-case/lending.borrow-copy</implements>
        public sealed class BorrowCopyCommandHandler(ICommandMediator commands) : ICommandHandler<BorrowCopyCommand>
        {
            public Task Handle(BorrowCopyCommand command)
            {
                return LoanFanout.RetryAsync(command.CopyId);
            }
        }
        """;

    const string MediatorOnlyHandler = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        /// <implements>use-case/lending.borrow-copy</implements>
        public sealed class BorrowCopyCommandHandler(ICommandMediator commands) : ICommandHandler<BorrowCopyCommand>
        {
            public Task Handle(BorrowCopyCommand command)
            {
                return LoanFanout.RetryAsync(command.CopyId, commands);
            }
        }
        """;

    const string TwoReceiverHelper = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        internal static class LoanFanout
        {
            public static async Task RetryAsync(Guid copyId, ICommandMediator commands, IEventPublisher events)
            {
                await commands.SendAsync(new PurgeLoansCommand());
                await events.PublishAsync(new LoanBorrowedEvent());
            }
        }
        """;

    const string ChainedHandler = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        /// <implements>use-case/lending.borrow-copy</implements>
        public sealed class BorrowCopyCommandHandler(ICommandMediator commands) : ICommandHandler<BorrowCopyCommand>
        {
            public Task Handle(BorrowCopyCommand command)
            {
                return FirstHop.RunAsync(command.CopyId, commands);
            }
        }
        """;

    const string FirstHop = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        internal static class FirstHop
        {
            public static async Task RunAsync(Guid copyId, ICommandMediator commands)
            {
                await commands.SendAsync(new PurgeLoansCommand());
                await SecondHop.RunAsync(copyId, commands);
            }
        }
        """;

    const string SecondHop = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        internal static class SecondHop
        {
            public static async Task RunAsync(Guid copyId, ICommandMediator commands)
            {
                await commands.SendAsync(new RetireCopyCommand());
                await FirstHop.RunAsync(copyId, commands);
            }
        }
        """;

    const string InstanceHandler = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        /// <implements>use-case/lending.borrow-copy</implements>
        public sealed class BorrowCopyCommandHandler(IOrderMessenger messenger, ILoanFanout fanout) : ICommandHandler<BorrowCopyCommand>
        {
            public Task Handle(BorrowCopyCommand command)
            {
                return fanout.NotifyAsync(command.CopyId, messenger);
            }
        }
        """;

    const string InstanceHelper = """
        namespace Library.Application.Lending.Loans.BorrowCopy;

        public interface ILoanFanout
        {
            Task NotifyAsync(Guid copyId, IOrderMessenger messenger);
        }

        internal sealed class LoanFanout : ILoanFanout
        {
            public async Task NotifyAsync(Guid copyId, IOrderMessenger messenger)
            {
                await messenger.SendAsync(new MemberInvitedNotification());
            }
        }
        """;
}
