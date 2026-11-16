using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5003, the mark count.</summary>
public sealed class MarkCountTests
{
    /// <summary>LS5003 reports a handler carrying two marks.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports a handler carrying two marks")]
    public Task ReportsAHandlerCarryingTwoMarks() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>use-case/spending.submit-expense</implements>
                /// <implements>use-case/spending.approve-expense</implements>
                internal sealed class {|LS5003:SubmitExpenseCommandHandler|} : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase(), UseCase("use-case/spending.approve-expense", "Approve expense")],
            assemblyName: "Shop.Application");

    /// <summary>LS5003 reports one record named twice.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports one record named twice")]
    public Task ReportsOneRecordNamedTwice() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>use-case/spending.submit-expense</implements>
                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class {|LS5003:SubmitExpenseCommandHandler|} : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    /// <summary>LS5003 reports a root method carrying two use-case marks.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact(DisplayName = "LS5003 reports a root method carrying two use-case marks")]
    public Task ReportsARootMethodCarryingTwoMarks() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>
                {
                    protected void RaiseDomainEvent(IDomainEvent e) { }
                }

                public interface IDomainEvent;

                /// <implements>domain-event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent : IDomainEvent;

                /// <implements>aggregate/spending.expense</implements>
                public sealed class Expense : AggregateRoot<object>
                {
                    /// <implements>use-case/spending.submit-expense</implements>
                    /// <implements>use-case/spending.approve-expense</implements>
                    public void {|LS5003:Submit|}()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), UseCase("use-case/spending.approve-expense", "Approve expense"), DomainEvent()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5003 accepts a root method carrying one mark.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact(DisplayName = "LS5003 accepts a root method carrying one mark")]
    public Task AcceptsARootMethodCarryingOneMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>
                {
                    protected void RaiseDomainEvent(IDomainEvent e) { }
                }

                public interface IDomainEvent;

                /// <implements>domain-event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent : IDomainEvent;

                /// <implements>aggregate/spending.expense</implements>
                public sealed class Expense : AggregateRoot<object>
                {
                    /// <implements>use-case/spending.submit-expense</implements>
                    public void Submit()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), DomainEvent()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5003 reports one record named twice on a multi-mark kind.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports one record named twice on a multi-mark kind")]
    public Task ReportsOneRecordNamedTwiceOnAMultiMarkKind() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>
                {
                    protected void RaiseDomainEvent(IDomainEvent e) { }
                }

                public interface IDomainEvent;

                /// <implements>domain-event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent : IDomainEvent;

                /// <implements>aggregate/spending.expense</implements>
                public sealed class Expense : AggregateRoot<object>
                {
                    /// <implements>use-case/spending.submit-expense</implements>
                    /// <implements>use-case/spending.submit-expense</implements>
                    public void {|LS5003:Submit|}()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), DomainEvent()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5003 reports a technical class naming one decision twice.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5003 reports a technical class naming one decision twice")]
    public Task ReportsADuplicateFollows() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <follows>decision/0002-keep-expenses-as-documents</follows>
                /// <follows>decision/0002-keep-expenses-as-documents</follows>
                internal sealed class {|LS5003:CommitStep|}
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    /// <summary>LS5003 reports one proof named twice on a test.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports one proof named twice on a test")]
    public Task ReportsOneProofNamedTwiceOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>requirement/borrowing.borrow-latency</proves>
                    /// <proves>requirement/borrowing.borrow-latency</proves>
                    [Fact]
                    public void {|LS5003:BorrowingStaysFast|}()
                    {
                    }
                }
                """)],
            [ProjectFile(), Requirement()],
            assemblyName: "Shop.Domain.Tests");
}
