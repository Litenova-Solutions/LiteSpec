using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves the backend reader recognizes handlers, reactions, jobs and events through a base class or an intermediate interface.</summary>
public sealed class DotnetTransitiveKindTests
{
    /// <summary>Handler Through Base Class Links Its Use Case.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void HandlerThroughBaseClassLinksItsUseCase()
    {
        using var temp = TestCli.Temp("reader-transitive");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "BorrowCopyHandlerBase.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            public abstract class BorrowCopyHandlerBase : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
            {
            }
            """);
        WriteSource(temp.Path, "App", "BorrowCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler : BorrowCopyHandlerBase
            {
                public Task Handle(BorrowCopyCommand command) => Task.CompletedTask;
            }
            """);

        var code = Read(temp.Path);
        var handler = code.Handlers.Single(found => found.Class == "BorrowCopyCommandHandler");

        Assert.Equal("use-case/lending.borrow-copy", handler.UseCaseId);
        Assert.Equal("lending", handler.Module);
        Assert.Equal("borrow-copy", handler.Name);
        Assert.Equal("BorrowCopy", handler.Operation);
        Assert.Equal("command", handler.Kind);
        Assert.Equal("BorrowCopyCommand", handler.MessageType);

        var element = code.Elements.Single(found => found.Class == "BorrowCopyCommandHandler" && found.Member is null);

        Assert.Equal(CodeKind.Handler, element.Kind);
        Assert.Single(element.Marks);
    }

    /// <summary>Query Handler Through Base Class Reads Query Kind.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void QueryHandlerThroughBaseClassReadsQueryKind()
    {
        using var temp = TestCli.Temp("reader-transitive");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "ListLoansHandlerBase.cs", """
            namespace Library.Application.Lending.Loans.ListLoans;

            public abstract class ListLoansHandlerBase : IQueryHandler<ListLoansQuery, ListLoansQueryResult>
            {
            }
            """);
        WriteSource(temp.Path, "App", "ListLoansQueryHandler.cs", """
            namespace Library.Application.Lending.Loans.ListLoans;

            /// <implements>use-case/lending.list-loans</implements>
            public sealed class ListLoansQueryHandler : ListLoansHandlerBase
            {
                public Task Handle(ListLoansQuery query) => Task.CompletedTask;
            }
            """);

        var handler = Read(temp.Path).Handlers.Single(found => found.Class == "ListLoansQueryHandler");

        Assert.Equal("use-case/lending.list-loans", handler.UseCaseId);
        Assert.Equal("query", handler.Kind);
        Assert.Equal("ListLoans", handler.Operation);
        Assert.Equal("ListLoansQuery", handler.MessageType);
    }

    /// <summary>Event Handler Through Base Class Links Its Reaction.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void EventHandlerThroughBaseClassLinksItsReaction()
    {
        using var temp = TestCli.Temp("reader-transitive");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "LoanBorrowedReactionBase.cs", """
            namespace Library.Application.Lending.Loans.OnLoanBorrowed;

            public abstract class LoanBorrowedReactionBase : IEventHandler<LoanBorrowedEvent>
            {
            }
            """);
        WriteSource(temp.Path, "App", "SendDueNoticeOnLoanBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnLoanBorrowed;

            /// <implements>reaction/lending.notify-on-borrow</implements>
            public sealed class SendDueNoticeOnLoanBorrowedHandler : LoanBorrowedReactionBase
            {
                public Task Handle(LoanBorrowedEvent domainEvent) => Task.CompletedTask;
            }
            """);

        var reaction = Read(temp.Path).Reactions.Single(found => found.Class == "SendDueNoticeOnLoanBorrowedHandler");

        Assert.Equal("reaction/lending.notify-on-borrow", reaction.RecordId);
        Assert.Null(reaction.UseCaseId);
    }

    /// <summary>Job Host Through BackgroundService Base Links Its Job.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void JobHostThroughBackgroundServiceBaseLinksItsJob()
    {
        using var temp = TestCli.Temp("reader-transitive");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "ScanServiceBase.cs", """
            namespace Library.Application.Lending.Loans.NightlyScan;

            public abstract class ScanServiceBase : BackgroundService
            {
            }
            """);
        WriteSource(temp.Path, "App", "NightlyScanJob.cs", """
            namespace Library.Application.Lending.Loans.NightlyScan;

            /// <implements>job/lending.nightly-scan</implements>
            public sealed class NightlyScanJob : ScanServiceBase
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);

        var job = Assert.Single(Read(temp.Path).Jobs);

        Assert.Equal("NightlyScanJob", job.Class);
        Assert.Equal("job/lending.nightly-scan", job.RecordId);
    }

    /// <summary>Event Through Intermediate Interface Links Its Event.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void EventThroughIntermediateInterfaceLinksItsEvent()
    {
        using var temp = TestCli.Temp("reader-transitive");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "ILoanEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            public interface ILoanEvent : IDomainEvent;
            """);
        WriteSource(temp.Path, "Domain", "LoanBorrowedEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            /// <implements>event/lending.loan-borrowed</implements>
            public sealed record LoanBorrowedEvent(Guid LoanId) : ILoanEvent;
            """);

        var code = Read(temp.Path);
        var domainEvent = Assert.Single(code.Events);

        Assert.Equal("LoanBorrowedEvent", domainEvent.Class);
        Assert.Equal("event/lending.loan-borrowed", domainEvent.EventId);

        var element = Assert.Single(code.Elements);

        Assert.Equal(CodeKind.DomainEvent, element.Kind);
        Assert.Equal("LoanBorrowedEvent", element.Class);
    }

    static CodeFacts Read(string root) => new DotnetBackendReader().Read(ConsumerConfig.Load(root));

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
