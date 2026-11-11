using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves the backend reader collects refusal codes from Code members only.</summary>
public sealed class DotnetBackendReaderTests
{
    /// <summary>Code Property Literal Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void CodePropertyLiteralIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "CopyNotAvailableException.cs", """
            namespace Library.Domain.Lending.Loans.Exceptions;

            public sealed class CopyNotAvailableException()
                : DomainException("The copy is not available.")
            {
                public override string Code => "lending.copy-not-available";
            }
            """);

        Assert.Contains("lending.copy-not-available", Literals(temp.Path));
    }

    /// <summary>Const Code Field Is Collected.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ConstCodeFieldIsCollected()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "BotCheckFailure.cs", """
            namespace Library.Domain.Requests;

            public static class BotCheckFailure
            {
                public const string Code = "request.bot-check-failed";
            }
            """);

        Assert.Contains("request.bot-check-failed", Literals(temp.Path));
    }

    /// <summary>Dotted Literals Outside Code Members Are Ignored.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void DottedLiteralsOutsideCodeMembersAreIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "BorrowCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            public sealed class BorrowCopyCommandHandler
            {
                private const string AuditAction = "forms.apply-form-template-to-event";
                private const string Table = "lending.loans";

                public Task Handle(BorrowCopyCommand command)
                {
                    if (command.CopyId == Guid.Empty)
                        Console.WriteLine("lending.copy-not-available");
                    var field = "form.fields";
                    var host = "api.example.com";
                    var sql = "SELECT id FROM lending.loans WHERE id = 1";
                    var file = "borrow-copy.json";
                    return Task.CompletedTask;
                }
            }
            """);

        Assert.Empty(Literals(temp.Path));
    }

    /// <summary>Exception Arguments And Named Codes Declare.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ExceptionArgumentsAndNamedCodesDeclare()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "ReturnCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.ReturnCopy;

            public sealed class ReturnCopyCommandHandler
            {
                public object Handle(ReturnCopyCommand command)
                {
                    if (command.Late)
                        throw new UseCaseUnprocessableException("lending.return-late", "The copy is late.");
                    if (command.Lost)
                        return new { code = "lending.copy-lost" };
                    return Verdict.Deny("Not yours.", "lending.not-borrower");
                }
            }
            """);

        var literals = Literals(temp.Path);

        Assert.Contains("lending.return-late", literals);
        Assert.Contains("lending.copy-lost", literals);
        Assert.DoesNotContain("lending.not-borrower", literals);
    }

    /// <summary>Every Dotted Literal Counts As Thrown.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void EveryDottedLiteralCountsAsThrown()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "Gate.cs", """
            namespace Library.Application.Pipeline;

            public sealed class Gate
            {
                public Verdict Check() => Verdict.Deny("Not allowed.", "authorization.forbidden");
            }
            """);

        var all = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Literals;

        Assert.Contains(all, literal => literal.Value == "authorization.forbidden" && !literal.Declares);
    }

    /// <summary>Plain Label Code Value Is Ignored.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void PlainLabelCodeValueIsIgnored()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "MerchantOwnerRole.cs", """
            namespace Library.Domain.Sales;

            public sealed record MerchantOwnerRole : MerchantRole
            {
                public override string Code => "owner";
            }
            """);

        Assert.Empty(Literals(temp.Path));
    }

    /// <summary>Repository Implementation Is Read From Infrastructure.</summary>
    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void RepositoryImplementationIsReadFromInfrastructure()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "ILoanRepository.cs", """
            namespace Library.Domain.Lending.Loans;

            public interface ILoanRepository
            {
                Loan Load(Guid id);
            }
            """);
        WriteSource(temp.Path, "Infrastructure", "LoanRepository.cs", """
            namespace Library.Infrastructure.Lending.Loans;

            public sealed class LoanRepository : ILoanRepository
            {
                public Loan Load(Guid id) => new Loan();
            }
            """);

        var implementation = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).RepositoryImplementations);

        Assert.Equal("ILoanRepository", implementation.Interface);
        Assert.Equal("LoanRepository", implementation.Class);
    }

    /// <summary>Repository Implementation Outside Infrastructure Is Not Read.</summary>
    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void RepositoryImplementationOutsideInfrastructureIsNotRead()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "ILoanRepository.cs", """
            namespace Library.Domain.Lending.Loans;

            public interface ILoanRepository
            {
                Loan Load(Guid id);
            }
            """);
        WriteSource(temp.Path, "App", "LoanRepository.cs", """
            namespace Library.Application.Lending.Loans;

            public sealed class LoanRepository : ILoanRepository
            {
                public Loan Load(Guid id) => new Loan();
            }
            """);

        Assert.Empty(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).RepositoryImplementations);
    }

    /// <summary>Domain Attribute Named Implements Is No Mark.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void DomainAttributeNamedImplementsIsNoMark()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "BorrowCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            [Spending.Implements("use-case/lending.borrow-copy")]
            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
            }
            """);

        var element = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Elements);

        Assert.Equal(CodeKind.Handler, element.Kind);
        Assert.Empty(element.Marks);
    }

    static List<string> Literals(string root) =>
        new DotnetBackendReader().Read(ConsumerConfig.Load(root))
            .Literals.Where(literal => literal.Declares).Select(literal => literal.Value).ToList();

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infrastructure"}}}
            """);

    /// <summary>A Code Inside A Message Declares Nothing.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ACodeInsideAMessageDeclaresNothing()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "Startup.cs", """
            namespace Library.Application;

            public static class Startup
            {
                public static void Check() =>
                    throw new InvalidOperationException("Set the edge to 10.0.0.7 in appsettings.json.");
            }
            """);

        Assert.Empty(Literals(temp.Path));
    }

    /// <summary>An Event States Its Record Id.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void AnEventStatesItsRecordId()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("Domain", "Lending", "Loans", "Events"), "LoanRenewedEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            /// <implements>event/loan.extended</implements>
            public sealed record LoanRenewedEvent : IDomainEvent
            {
            }
            """);

        var domainEvent = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Events);

        Assert.Equal("event/loan.extended", domainEvent.EventId);
    }

    /// <summary>On Handler With A Tag Links To Its Reaction.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void OnHandlerWithATagLinksToItsReaction()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            /// <implements>reaction/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var reaction = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Reactions);

        Assert.Equal("NotifyReaderOnCopyBorrowedHandler", reaction.Class);
        Assert.Equal("reaction/lending.notify-reader-on-borrow", reaction.RecordId);
    }

    /// <summary>On Handler Without A Tag Is Discovered Without A Record.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void OnHandlerWithoutATagIsDiscoveredWithoutARecord()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var reaction = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Reactions);

        Assert.Null(reaction.RecordId);
    }

    /// <summary>Reaction With The Handler Shape Is Discovered Without The Suffix.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void ReactionWithTheHandlerShapeIsDiscoveredWithoutTheSuffix()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "NotifyOnCopyBorrowedReaction.cs", """
            namespace Library.Application;

            public sealed class NotifyOnCopyBorrowedReaction : IDomainEventReaction<CopyBorrowedEvent>
            {
            }
            """);
        WriteSource(temp.Path, "App", "SweepReaction.cs", """
            namespace Library.Application;

            public sealed class SweepReaction
            {
            }
            """);

        var reaction = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Reactions);

        Assert.Equal("NotifyOnCopyBorrowedReaction", reaction.Class);
    }

    /// <summary>On Handler Outside Its Fact Folder Links Through Its Mark.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void OnHandlerOutsideItsFactFolderLinksThroughItsMark()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "BorrowCopy"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            /// <implements>reaction/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var reaction = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Reactions);

        Assert.Equal("reaction/lending.notify-reader-on-borrow", reaction.RecordId);
    }

    /// <summary>On Handler Without The Interface Is Not Discovered.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void OnHandlerWithoutTheInterfaceIsNotDiscovered()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            /// <implements>reaction/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler
            {
            }
            """);

        Assert.Empty(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Reactions);
    }

    /// <summary>On Handler Is Not Read As A Use Case Handler.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void OnHandlerIsNotReadAsAUseCaseHandler()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            /// <implements>reaction/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Single(code.Reactions);
        Assert.Empty(code.Handlers);
    }

    /// <summary>Job Host With A Tag Links To Its Job.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void JobHostWithATagLinksToItsJob()
    {
        using var temp = TestCli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "NightlyDueScanJob.cs", """
            namespace Library.Application.Lending.Loans;

            /// <implements>job/lending.nightly-due-scan</implements>
            public sealed class NightlyDueScanJob : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));
        var job = Assert.Single(code.Jobs);

        Assert.Equal("NightlyDueScanJob", job.Class);
        Assert.Equal("job/lending.nightly-due-scan", job.RecordId);
        Assert.Empty(code.Reactions);
    }

    /// <summary>Aggregate Root By Mark And State Record Is Read At Traced.</summary>
    /// <proves>litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record</proves>
    [Fact]
    public void AggregateRootByMarkAndStateRecordIsReadAtTraced()
    {
        using var temp = TestCli.Temp("reader");
        WriteTracedProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Loan.cs", """
            namespace Library.Domain.Lending.Loans;

            /// <implements>aggregate/lending.loan</implements>
            public sealed class Loan
            {
                public LoanState State { get; private set; } = new LoanActiveState();

                /// <implements>use-case/lending.borrow-copy</implements>
                public void Borrow()
                {
                    State = new LoanBorrowedState();
                }
            }

            public abstract record LoanState;

            public sealed record LoanActiveState : LoanState;

            public sealed record LoanBorrowedState : LoanState;
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));
        var element = Assert.Single(code.Elements, found => found.Kind == CodeKind.AggregateRoot);

        Assert.Equal("Loan", element.Class);
        var method = Assert.Single(code.AggregateMethods);
        Assert.Equal("Loan", method.Aggregate);
        Assert.Equal("Borrow", method.Method);
        Assert.Equal("use-case/lending.borrow-copy", Assert.Single(method.UseCases));
    }

    /// <summary>Aggregate With Mark But No State Record Is Not An Aggregate Root At Traced.</summary>
    /// <proves>litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record</proves>
    [Fact]
    public void AggregateWithMarkButNoStateRecordIsNotAnAggregateRootAtTraced()
    {
        using var temp = TestCli.Temp("reader");
        WriteTracedProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Loan.cs", """
            namespace Library.Domain.Lending.Loans;

            /// <implements>aggregate/lending.loan</implements>
            public sealed class Loan
            {
                public void Borrow()
                {
                }
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.DoesNotContain(code.Elements, element => element.Kind == CodeKind.AggregateRoot);
        Assert.Empty(code.AggregateMethods);
    }

    /// <summary>Aggregate With State Record But No Mark Is Not An Aggregate Root At Traced.</summary>
    /// <proves>litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record</proves>
    [Fact]
    public void AggregateWithStateRecordButNoMarkIsNotAnAggregateRootAtTraced()
    {
        using var temp = TestCli.Temp("reader");
        WriteTracedProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Loan.cs", """
            namespace Library.Domain.Lending.Loans;

            public sealed class Loan
            {
                public LoanState State { get; private set; } = new LoanActiveState();

                public void Borrow()
                {
                }
            }

            public abstract record LoanState;

            public sealed record LoanActiveState : LoanState;
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.DoesNotContain(code.Elements, element => element.Kind == CodeKind.AggregateRoot);
    }

    /// <summary>Aggregate Root On Its Own Base By Mark And State Record Is Read At Traced.</summary>
    /// <proves>litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record</proves>
    [Fact]
    public void AggregateRootOnItsOwnBaseByMarkAndStateRecordIsReadAtTraced()
    {
        using var temp = TestCli.Temp("reader");
        WriteTracedProject(temp.Path);
        WriteSource(temp.Path, "Domain", "Loan.cs", """
            namespace Library.Domain.Lending.Loans;

            public abstract class ProductAggregate
            {
            }

            /// <implements>aggregate/lending.loan</implements>
            public sealed class Loan : ProductAggregate
            {
                public LoanState State { get; private set; } = new LoanActiveState();

                public void Borrow()
                {
                }
            }

            public abstract record LoanState;

            public sealed record LoanActiveState : LoanState;
            """);

        var element = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Elements, found => found.Kind == CodeKind.AggregateRoot);

        Assert.Equal("Loan", element.Class);
    }

    static void WriteTracedProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "own", "proof": "own"}, "stackConfig": {"dotnet": {
              "modules": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infrastructure"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
