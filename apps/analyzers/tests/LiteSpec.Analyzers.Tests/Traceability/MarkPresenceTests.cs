using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5001, the missing mark.</summary>
public sealed class MarkPresenceTests
{
    [Fact(DisplayName = "LS5001 reports a handler without a mark")]
    public Task ReportsAHandlerWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class {|LS5001:SubmitExpenseCommandHandler|} : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 accepts a handler with a mark")]
    public Task AcceptsAHandlerWithAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 reports an aggregate root without a mark")]
    public Task ReportsAnAggregateRootWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>;

                public sealed class ExpenseState;

                public sealed class {|LS5001:Expense|} : AggregateRoot<ExpenseState>
                {
                }
                """)],
            [ProjectFile(), Aggregate()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5001 reports a state-changing method without a mark")]
    public Task ReportsAStateChangingMethodWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>
                {
                    protected void RaiseDomainEvent(IDomainEvent e) { }
                }

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent : IDomainEvent;

                /// <implements>aggregate/spending.expense</implements>
                public sealed class Expense : AggregateRoot<object>
                {
                    /// <summary>Submits the expense.</summary>
                    public void {|LS5001:Submit|}()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5001 ignores a method that raises nothing")]
    public Task IgnoresAMethodThatRaisesNothing() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>
                {
                    protected void RaiseDomainEvent(IDomainEvent e) { }
                }

                public interface IDomainEvent;

                /// <implements>aggregate/spending.expense</implements>
                public sealed class Expense : AggregateRoot<object>
                {
                    public bool IsSubmitted() => false;
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5001 reports a domain event without a mark")]
    public Task ReportsADomainEventWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IDomainEvent;

                public sealed record {|LS5001:ExpenseSubmittedEvent|} : IDomainEvent;
                """)],
            [ProjectFile(), Event()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5001 reports a rule exception without a mark")]
    public Task ReportsARuleExceptionWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public abstract class DomainException : Exception
                {
                    public virtual string Code => "base";
                }

                public sealed class {|LS5001:ReceiptRequiredException|} : DomainException
                {
                }
                """)],
            [ProjectFile(), Rule()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5001 reports an endpoint without a mark")]
    public Task ReportsAnEndpointWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IEndpoint;

                internal sealed class {|LS5001:SubmitExpenseEndpoint|} : IEndpoint
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.WebApi");

    [Fact(DisplayName = "LS5001 reports a job without a mark")]
    public Task ReportsAJobWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System.Threading;
                using System.Threading.Tasks;

                namespace Claimwick.Spending;

                public interface IHostedService
                {
                    Task StartAsync(CancellationToken cancellationToken);
                    Task StopAsync(CancellationToken cancellationToken);
                }

                internal sealed class {|LS5001:RemindApproversJob|} : IHostedService
                {
                    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """)],
            [ProjectFile(), Job()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 reports a test without a mark")]
    public Task ReportsATestWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    [Fact]
                    public void {|LS5001:Submitting_with_a_receipt_succeeds|}()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS5001 accepts a test with a mark")]
    public Task AcceptsATestWithAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>scenario/submit-expense.submitting-with-a-receipt-succeeds</proves>
                    [Fact]
                    public void Submitting_with_a_receipt_succeeds()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS5001 ignores an unmarked port, recognized only by its mark")]
    public Task IgnoresAnUnmarkedPort() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public interface IPaymentGateway
                {
                    void Charge(decimal amount);
                }
                """)],
            [ProjectFile(), ExternalSystem()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 accepts an attribute mark when the project declares attribute")]
    public Task AcceptsAnAttributeMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class ImplementsAttribute : Attribute
                {
                    public ImplementsAttribute(string id) { }
                }

                [Implements("use-case/spending.submit-expense")]
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(marks: "attribute"), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 stays silent with no project file")]
    public Task StaysSilentWithNoProjectFile() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 accepts a qualified attribute mark")]
    public Task AcceptsAQualifiedAttributeMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                [LiteSpec.Implements("use-case/spending.submit-expense")]
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """),
             ("Attributes.cs", """
                namespace LiteSpec;

                internal sealed class ImplementsAttribute : System.Attribute
                {
                    public ImplementsAttribute(string id) { }
                }
                """)],
            [ProjectFile(marks: "attribute"), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 reports a technical class without a mark")]
    public Task ReportsATechnicalClassWithoutAMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System.Threading;
                using System.Threading.Tasks;

                namespace Shop.Spending;

                internal sealed class {|LS5001:CommitStep|}
                {
                    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 accepts a technical class with a follows mark")]
    public Task AcceptsATechnicalClassWithFollows() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System.Threading;
                using System.Threading.Tasks;

                namespace Shop.Spending;

                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class CommitStep
                {
                    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 accepts a technical class with a follows attribute")]
    public Task AcceptsATechnicalClassWithFollowsAttribute() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Shop.Spending;

                internal sealed class FollowsAttribute : Attribute
                {
                    public FollowsAttribute(string id) { }
                }

                [Follows("decision/0002-store-expenses-as-documents")]
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(marks: "attribute"), Decision()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 accepts a technical class carrying only an implements mark")]
    public Task AcceptsATechnicalClassWithOnlyImplements() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System.Threading;
                using System.Threading.Tasks;

                namespace Shop.Spending;

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class SubmitExpenseValidator
                {
                    public Task ValidateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 accepts a technical class with both verbs")]
    public Task AcceptsATechnicalClassWithBothVerbs() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System.Threading;
                using System.Threading.Tasks;

                namespace Shop.Spending;

                /// <implements>aggregate/spending.expense</implements>
                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class ExpenseRepository
                {
                    public Task StoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """)],
            [ProjectFile(), Aggregate(), Decision()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 reports a handler carrying only a follows mark")]
    public Task ReportsAHandlerWithOnlyFollows() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class {|LS5001:SubmitExpenseCommandHandler|} : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 ignores a record, a migration, and a methodless class")]
    public Task IgnoresNonTechnicalShapes() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                public class Migration;

                public sealed record LoanSummary(string LoanId)
                {
                    public bool IsOpen() => true;
                }

                internal sealed class AddLoans : Migration
                {
                    public void Up()
                    {
                    }
                }

                internal sealed class LoanSettings
                {
                    public int CacheMinutes { get; set; }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5001 ignores an interface, a generated class, and a nested class")]
    public Task IgnoresInterfaceGeneratedAndNestedShapes() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Shop.Spending;

                public interface ILoanClock
                {
                    DateTimeOffset UtcNow();
                }

                internal sealed class LoanReports
                {
                    internal sealed class Builder
                    {
                        public string Build() => "";
                    }
                }
                """),
             ("LoanGeneratedClient.g.cs", """
                namespace Shop.Spending;

                internal sealed class LoanGeneratedClient
                {
                    public void Fetch()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");
}
