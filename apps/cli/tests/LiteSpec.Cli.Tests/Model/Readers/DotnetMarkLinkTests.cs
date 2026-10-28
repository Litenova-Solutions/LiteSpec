using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Readers;

/// <summary>Proves the backend reader links code to records through marks only, in either style.</summary>
public sealed class DotnetMarkLinkTests
{
    [Fact]
    public void Handler_Marked_Xml_Links_Its_Use_Case()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "BorrowCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
                public Task Handle(BorrowCopyCommand command) => Task.CompletedTask;
            }
            """);

        var handler = Assert.Single(Read(temp.Path).Handlers);

        Assert.Equal("use-case/lending.borrow-copy", handler.UseCaseId);
        Assert.Equal("lending", handler.Module);
        Assert.Equal("borrow-copy", handler.Name);
        Assert.Equal("BorrowCopy", handler.Operation);
        Assert.Equal("command", handler.Kind);
        Assert.Equal("BorrowCopyCommand", handler.MessageType);
    }

    [Fact]
    public void Handler_Marked_Attribute_Links_Its_Use_Case()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "ListLoans"), "ListLoansQueryHandler.cs", """
            namespace Library.Application.Lending.Loans.ListLoans;

            [Implements("use-case/lending.list-loans")]
            public sealed class ListLoansQueryHandler : IQueryHandler<ListLoansQuery>
            {
                public Task Handle(ListLoansQuery query) => Task.CompletedTask;
            }
            """);

        var handler = Assert.Single(Read(temp.Path).Handlers);

        Assert.Equal("use-case/lending.list-loans", handler.UseCaseId);
        Assert.Equal("lending", handler.Module);
        Assert.Equal("list-loans", handler.Name);
        Assert.Equal("ListLoans", handler.Operation);
        Assert.Equal("query", handler.Kind);
    }

    [Fact]
    public void Handler_Without_Mark_Has_Empty_Use_Case_Id_And_An_Unmarked_Element()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "BorrowCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
                public Task Handle(BorrowCopyCommand command) => Task.CompletedTask;
            }
            """);

        var code = Read(temp.Path);
        var handler = Assert.Single(code.Handlers);

        Assert.Equal("", handler.UseCaseId);

        var element = Assert.Single(code.Elements);

        Assert.Equal(CodeKind.Handler, element.Kind);
        Assert.Equal("BorrowCopyCommandHandler", element.Class);
        Assert.Empty(element.Marks);
    }

    [Fact]
    public void Handler_With_Two_Use_Case_Marks_Has_Empty_Use_Case_Id()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "BorrowCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            /// <implements>use-case/lending.return-copy</implements>
            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
                public Task Handle(BorrowCopyCommand command) => Task.CompletedTask;
            }
            """);

        var code = Read(temp.Path);
        var handler = Assert.Single(code.Handlers);

        Assert.Equal("", handler.UseCaseId);

        var element = Assert.Single(code.Elements);

        Assert.Equal(2, element.Marks.Count);
    }

    [Fact]
    public void Event_Id_Comes_From_Its_Mark_Not_Its_Class_Name()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "LoanRenewedEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            /// <implements>event/loan.loan-borrowed</implements>
            public sealed record LoanRenewedEvent(Guid LoanId) : IDomainEvent;
            """);

        var domainEvent = Assert.Single(Read(temp.Path).Events);

        Assert.Equal("event/loan.loan-borrowed", domainEvent.EventId);
    }

    [Fact]
    public void Event_Without_Mark_Has_Null_Event_Id()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "LoanBorrowedEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            public sealed record LoanBorrowedEvent(Guid LoanId) : IDomainEvent;
            """);

        var domainEvent = Assert.Single(Read(temp.Path).Events);

        Assert.Null(domainEvent.EventId);
    }

    [Fact]
    public void Aggregate_Method_Carries_Its_Use_Cases()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Loan.cs", """
            namespace Library.Domain.Lending.Loans;

            /// <implements>aggregate/lending.loan</implements>
            public sealed class Loan : AggregateRoot<Guid>
            {
                /// <implements>use-case/lending.borrow-copy</implements>
                public void Borrow()
                {
                    RaiseDomainEvent(new LoanBorrowedEvent());
                }
            }
            """);

        var method = Assert.Single(Read(temp.Path).AggregateMethods);

        Assert.Equal("Loan", method.Aggregate);
        Assert.Equal("Borrow", method.Method);
        Assert.Equal("LoanBorrowedEvent", Assert.Single(method.Events));
        Assert.Equal("use-case/lending.borrow-copy", Assert.Single(method.UseCases));
    }

    [Fact]
    public void Aggregate_Method_Without_Marks_Has_No_Use_Cases()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Loan.cs", """
            namespace Library.Domain.Lending.Loans;

            /// <implements>aggregate/lending.loan</implements>
            public sealed class Loan : AggregateRoot<Guid>
            {
                public void Touch()
                {
                }
            }
            """);

        var method = Assert.Single(Read(temp.Path).AggregateMethods);

        Assert.Equal("Touch", method.Method);
        Assert.Empty(method.UseCases);
    }

    [Fact]
    public void Classification_Lands_In_Runtime_Value()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "LoanBorrowedEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            /// <implements>event/loan.loan-borrowed</implements>
            public sealed record LoanBorrowedEvent(Guid LoanId) : IDomainEvent
            {
                public const string Classification = "event/loan.loan-borrowed";
            }
            """);

        var element = Assert.Single(Read(temp.Path).Elements);

        Assert.Equal(CodeKind.DomainEvent, element.Kind);
        Assert.Equal("event/loan.loan-borrowed", element.RuntimeValue);
    }

    [Fact]
    public void Event_Without_Classification_Has_Null_Runtime_Value()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "LoanBorrowedEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            /// <implements>event/loan.loan-borrowed</implements>
            public sealed record LoanBorrowedEvent(Guid LoanId) : IDomainEvent;
            """);

        var element = Assert.Single(Read(temp.Path).Elements);

        Assert.Null(element.RuntimeValue);
    }

    [Fact]
    public void Endpoint_Links_Its_Use_Case_From_Its_Mark()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "WebApi", "BorrowCopyEndpoint.cs", """
            namespace Library.WebApi.Lending.Loans.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyEndpoint : IEndpoint
            {
                public void Map(WebApplication app)
                {
                    app.MapPost("/api/borrow", Handle).WithName("BorrowCopy");
                }

                private static void Handle()
                {
                }
            }
            """);

        var endpoints = Read(temp.Path).Endpoints;

        Assert.Equal(2, endpoints.Count);
        Assert.All(endpoints, endpoint => Assert.Equal("use-case/lending.borrow-copy", endpoint.UseCaseId));
        Assert.Contains(endpoints, endpoint => endpoint is { Operation: "BorrowCopy", Method: "", Route: "" });
        Assert.Contains(endpoints, endpoint => endpoint is { Operation: "", Method: "POST", Route: "/api/borrow" });
    }

    [Fact]
    public void Endpoint_Without_Mark_Has_Empty_Use_Case_Id()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "WebApi", "BorrowCopyEndpoint.cs", """
            namespace Library.WebApi.Lending.Loans.BorrowCopy;

            public sealed class BorrowCopyEndpoint : IEndpoint
            {
                public void Map(WebApplication app)
                {
                    app.MapPost("/api/borrow", Handle).WithName("BorrowCopy");
                }

                private static void Handle()
                {
                }
            }
            """);

        var endpoints = Read(temp.Path).Endpoints;

        Assert.Equal(2, endpoints.Count);
        Assert.All(endpoints, endpoint => Assert.Equal("", endpoint.UseCaseId));
    }

    [Fact]
    public void Reaction_Links_Its_Record_From_Its_Mark()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "NotifyOnBorrowHandler.cs", """
            namespace Library.Application.Lending.Loans.OnLoanBorrowed;

            /// <implements>reaction/lending.notify-on-borrow</implements>
            public sealed class NotifyOnBorrowHandler : IEventHandler<LoanBorrowedEvent>
            {
                public Task Handle(LoanBorrowedEvent domainEvent) => Task.CompletedTask;
            }
            """);

        var reaction = Assert.Single(Read(temp.Path).Reactions);

        Assert.Equal("reaction", reaction.Kind);
        Assert.Equal("reaction/lending.notify-on-borrow", reaction.RecordId);
        Assert.Null(reaction.UseCaseId);
    }

    [Fact]
    public void Reaction_Without_Mark_Has_Null_Record_Id()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "NotifyOnBorrowHandler.cs", """
            namespace Library.Application.Lending.Loans.OnLoanBorrowed;

            public sealed class NotifyOnBorrowHandler : IEventHandler<LoanBorrowedEvent>
            {
                public Task Handle(LoanBorrowedEvent domainEvent) => Task.CompletedTask;
            }
            """);

        var reaction = Assert.Single(Read(temp.Path).Reactions);

        Assert.Equal("reaction", reaction.Kind);
        Assert.Null(reaction.RecordId);
    }

    [Fact]
    public void Job_Links_Its_Record_From_Its_Mark()
    {
        using var temp = Cli.Temp("reader-marks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "NightlyScanJob.cs", """
            namespace Library.Application.Lending.Loans.NightlyScan;

            /// <implements>job/lending.nightly-scan</implements>
            public sealed class NightlyScanJob : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);

        var job = Assert.Single(Read(temp.Path).Reactions);

        Assert.Equal("job", job.Kind);
        Assert.Equal("job/lending.nightly-scan", job.RecordId);
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
