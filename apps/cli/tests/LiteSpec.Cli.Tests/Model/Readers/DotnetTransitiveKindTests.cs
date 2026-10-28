using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Readers;

/// <summary>Proves the backend reader recognizes handlers, jobs and events through a base class or an intermediate interface.</summary>
public sealed class DotnetTransitiveKindTests
{
    [Fact]
    public void Handler_Through_Base_Class_Links_Its_Use_Case()
    {
        using var temp = Cli.Temp("reader-transitive");
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

    [Fact]
    public void Query_Handler_Through_Base_Class_Reads_Query_Kind()
    {
        using var temp = Cli.Temp("reader-transitive");
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

    [Fact]
    public void Event_Handler_Through_Base_Class_Links_Its_Reaction()
    {
        using var temp = Cli.Temp("reader-transitive");
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

        Assert.Equal("reaction", reaction.Kind);
        Assert.Equal("reaction/lending.notify-on-borrow", reaction.RecordId);
        Assert.Null(reaction.UseCaseId);
    }

    [Fact]
    public void Job_Through_BackgroundService_Base_Links_Its_Job()
    {
        using var temp = Cli.Temp("reader-transitive");
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

        var job = Assert.Single(Read(temp.Path).Reactions);

        Assert.Equal("NightlyScanJob", job.Class);
        Assert.Equal("job", job.Kind);
        Assert.Equal("job/lending.nightly-scan", job.RecordId);
    }

    [Fact]
    public void Event_Through_Intermediate_Interface_Links_Its_Event()
    {
        using var temp = Cli.Temp("reader-transitive");
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

    static CodeModel Read(string root) => new DotnetBackendReader().Read(ConsumerConfig.Load(root));

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
