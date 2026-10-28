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
}
