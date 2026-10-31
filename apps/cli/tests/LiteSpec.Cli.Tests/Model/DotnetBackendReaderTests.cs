using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the backend reader collects refusal codes from Code members only.</summary>
public sealed class DotnetBackendReaderTests
{
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Code_Property_Literal_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Const_Code_Field_Is_Collected()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Dotted_Literals_Outside_Code_Members_Are_Ignored()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Exception_Arguments_And_Named_Codes_Declare()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Every_Dotted_Literal_Counts_As_Thrown()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Plain_Label_Code_Value_Is_Ignored()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void Repository_Implementation_Is_Read_From_Infrastructure()
    {
        using var temp = Cli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "ILoanRepository.cs", """
            namespace Library.Domain.Lending.Loans;

            public interface ILoanRepository
            {
                Loan Load(Guid id);
            }
            """);
        WriteSource(temp.Path, "Infra", "LoanRepository.cs", """
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

    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void Repository_Implementation_Outside_Infrastructure_Is_Not_Read()
    {
        using var temp = Cli.Temp("reader");
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

    static List<string> Literals(string root) =>
        new DotnetBackendReader().Read(ConsumerConfig.Load(root))
            .Literals.Where(literal => literal.Declares).Select(literal => literal.Value).ToList();

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "application": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void A_Code_Inside_A_Message_Declares_Nothing()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void An_Event_States_Its_Record_Id()
    {
        using var temp = Cli.Temp("reader");
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

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void On_Handler_With_A_Tag_Links_To_Its_Automation()
    {
        using var temp = Cli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            /// <implements>automation/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var automation = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Automations);

        Assert.Equal("NotifyReaderOnCopyBorrowedHandler", automation.Class);
        Assert.Equal("automation/lending.notify-reader-on-borrow", automation.RecordId);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void On_Handler_Without_A_Tag_Is_Discovered_Without_A_Record()
    {
        using var temp = Cli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var automation = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Automations);

        Assert.Null(automation.RecordId);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Automation_With_The_Handler_Shape_Is_Discovered_Without_The_Suffix()
    {
        using var temp = Cli.Temp("reader");
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

        var automation = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Automations);

        Assert.Equal("NotifyOnCopyBorrowedReaction", automation.Class);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void On_Handler_Outside_Its_Fact_Folder_Links_Through_Its_Mark()
    {
        using var temp = Cli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "BorrowCopy"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            /// <implements>automation/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var automation = Assert.Single(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Automations);

        Assert.Equal("automation/lending.notify-reader-on-borrow", automation.RecordId);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void On_Handler_Without_The_Interface_Is_Not_Discovered()
    {
        using var temp = Cli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            /// <implements>automation/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler
            {
            }
            """);

        Assert.Empty(new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path)).Automations);
    }

    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void On_Handler_Is_Not_Read_As_A_Use_Case_Handler()
    {
        using var temp = Cli.Temp("reader");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Lending", "Loans", "OnCopyBorrowed"), "NotifyReaderOnCopyBorrowedHandler.cs", """
            namespace Library.Application.Lending.Loans.OnCopyBorrowed;

            /// <implements>automation/lending.notify-reader-on-borrow</implements>
            public sealed class NotifyReaderOnCopyBorrowedHandler : IEventHandler<CopyBorrowedEvent>
            {
            }
            """);

        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Single(code.Automations);
        Assert.Empty(code.Handlers);
    }

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
