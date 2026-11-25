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
            [("Shop.Spending/Application/Source.cs", """
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
            [ProjectFile(), UseCase(), UseCase("use-case/spending.approve-expense", "Approve expense")]);

    /// <summary>LS5003 reports one record named twice.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports one record named twice")]
    public Task ReportsOneRecordNamedTwice() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Shop.Spending/Application/Source.cs", """
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
            [ProjectFile(), UseCase()]);

    /// <summary>LS5003 reports a root method carrying two use-case marks.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact(DisplayName = "LS5003 reports a root method carrying two use-case marks")]
    public Task ReportsARootMethodCarryingTwoMarks() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Shop.Spending/Domain/Source.cs", """
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
            [ProjectFile(), Aggregate(), UseCase(), UseCase("use-case/spending.approve-expense", "Approve expense"), DomainEvent()]);

    /// <summary>LS5003 accepts a root method carrying one mark.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
    [Fact(DisplayName = "LS5003 accepts a root method carrying one mark")]
    public Task AcceptsARootMethodCarryingOneMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Shop.Spending/Domain/Source.cs", """
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
            [ProjectFile(), Aggregate(), UseCase(), DomainEvent()]);

    /// <summary>LS5003 reports one record named twice on a multi-mark kind.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports one record named twice on a multi-mark kind")]
    public Task ReportsOneRecordNamedTwiceOnAMultiMarkKind() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Shop.Spending/Domain/Source.cs", """
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
            [ProjectFile(), Aggregate(), UseCase(), DomainEvent()]);

    /// <summary>LS5003 reports a technical class naming one decision twice.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5003 reports a technical class naming one decision twice")]
    public Task ReportsADuplicateFollows() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Shop.Spending/Application/Source.cs", """
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
            [ProjectFile(), Decision()]);

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

    /// <summary>LS5003 accepts a partial exception with its mark on one half.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 accepts a partial exception with its mark on one half")]
    public Task AcceptsAPartialExceptionWithItsMarkOnOneHalf() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Shop.Spending/Domain/ExpenseReceiptMissingException.cs", """
                namespace Claimwick.Spending;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                public sealed partial class ExpenseReceiptMissingException : DomainException
                {
                    public ExpenseReceiptMissingException()
                        : base("The expense is above the receipt threshold and carries no receipt.")
                    {
                    }
                }
                """),
            ("Shop.Spending/Domain/ExpenseReceiptMissingException.generated.cs", """
                // This file is generated by litespec generate from the records. Do not edit it by hand. LiteSpec 7.0.0.
                // <auto-generated/>

                namespace Claimwick.Spending;

                /// <implements>rule/spending.receipt-required</implements>
                public sealed partial class ExpenseReceiptMissingException : DomainException
                {
                    public override string Code => "spending.receipt-missing";
                }
                """)],
            [ProjectFile(), Rule()]);

    /// <summary>LS5003 reports a partial exception naming one rule on both halves.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports a partial exception naming one rule on both halves")]
    public Task ReportsAPartialExceptionNamingOneRuleOnBothHalves() =>
        Verify.AnalyzerWithRecordsAsync<MarkCountAnalyzer>(
            [("Shop.Spending/Domain/ExpenseReceiptMissingException.cs", """
                namespace Claimwick.Spending;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                /// <implements>rule/spending.receipt-required</implements>
                public sealed partial class {|LS5003:ExpenseReceiptMissingException|} : DomainException
                {
                    public ExpenseReceiptMissingException()
                        : base("The expense is above the receipt threshold and carries no receipt.")
                    {
                    }
                }
                """),
            ("Shop.Spending/Domain/ExpenseReceiptMissingException.generated.cs", """
                // This file is generated by litespec generate from the records. Do not edit it by hand. LiteSpec 7.0.0.
                // <auto-generated/>

                namespace Claimwick.Spending;

                /// <implements>rule/spending.receipt-required</implements>
                public sealed partial class ExpenseReceiptMissingException : DomainException
                {
                    public override string Code => "spending.receipt-missing";
                }
                """)],
            [ProjectFile(), Rule()]);
}
