using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5002, LS5003 and LS5004, the mark link, count and style.</summary>
public sealed class MarkLinkTests
{
    [Fact(DisplayName = "LS5002 reports a mark naming no record")]
    public Task ReportsAMarkNamingNoRecord() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>{|LS5002:use-case/spending.submit-expences|}</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5002 reports a mark naming a record of the wrong type")]
    public Task ReportsAMarkNamingTheWrongType() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>{|LS5002:rule/spending.receipt-required|}</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase(), Rule()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5002 reports a mistyped attribute mark")]
    public Task ReportsAMistypedAttributeMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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

                [Implements("{|LS5002:use-case/spending.submit-expences|}")]
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(marks: "attribute"), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5002 reports a mark carrying no record identifier")]
    public Task ReportsAMarkCarryingNoIdentifier() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// {|LS5002:<implements></implements>|}
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5002 accepts a mark naming the record the element takes")]
    public Task AcceptsAResolvingMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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

    [Fact(DisplayName = "LS5003 reports a handler carrying two marks")]
    public Task ReportsAHandlerCarryingTwoMarks() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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

    [Fact(DisplayName = "LS5003 reports one record named twice")]
    public Task ReportsOneRecordNamedTwice() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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

    [Fact(DisplayName = "LS5003 reports a root method carrying two use-case marks")]
    public Task ReportsARootMethodCarryingTwoMarks() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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
                    /// <implements>use-case/spending.submit-expense</implements>
                    /// <implements>use-case/spending.approve-expense</implements>
                    public void {|LS5003:Submit|}()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), UseCase("use-case/spending.approve-expense", "Approve expense"), Event()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5003 accepts a root method carrying one mark")]
    public Task AcceptsARootMethodCarryingOneMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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
                    /// <implements>use-case/spending.submit-expense</implements>
                    public void Submit()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5002 reports a mark on a reading method")]
    public Task ReportsAMarkOnAReadingMethod() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public sealed class ExpenseCalculator
                {
                    /// <implements>{|LS5002:use-case/spending.submit-expense|}</implements>
                    public int ApprovedCount() => 0;
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5002 accepts a reading method with no mark")]
    public Task AcceptsAReadingMethodWithNoMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public sealed class ExpenseCalculator
                {
                    public int ApprovedCount() => 0;
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5002 reports a mark on a private method")]
    public Task ReportsAMarkOnAPrivateMethod() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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
                    /// <implements>{|LS5002:use-case/spending.submit-expense|}</implements>
                    private void RecordSubmission()
                    {
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5002 accepts a private method with no mark")]
    public Task AcceptsAPrivateMethodWithNoMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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
                    private void RecordSubmission()
                    {
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5002 reports a proves mark on a method with no test attribute")]
    public Task ReportsAProvesMarkWithNoTestAttribute() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending.Tests;

                public sealed class ExpenseTests
                {
                    /// <proves>{|LS5002:scenario/submit-expense.submitting-with-a-receipt-succeeds|}</proves>
                    public void Submitting_with_a_receipt_succeeds()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS5003 reports one record named twice on a multi-mark kind")]
    public Task ReportsOneRecordNamedTwiceOnAMultiMarkKind() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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
                    /// <implements>use-case/spending.submit-expense</implements>
                    /// <implements>use-case/spending.submit-expense</implements>
                    public void {|LS5003:Submit|}()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS5002 reports a proves mark on a handler")]
    public Task ReportsAProvesMarkOnAHandler() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <proves>{|LS5002:use-case/spending.submit-expense|}</proves>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5002 reports an implements mark on a test")]
    public Task ReportsAnImplementsMarkOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <implements>{|LS5002:scenario/submit-expense.submitting-with-a-receipt-succeeds|}</implements>
                    [Fact]
                    public void Submitting_with_a_receipt_succeeds()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    [Fact(DisplayName = "LS5002 accepts a proves mark on a test")]
    public Task AcceptsAProvesMarkOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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

    [Fact(DisplayName = "LS5002 on a draft record reports a warning")]
    public Task DraftSubjectReportsWarning() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>{|LS5002:Warning:rule/spending.receipt-required|}</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase(), Rule(status: "draft")],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5004 reports an attribute mark when the project declares xml")]
    public Task ReportsAnAttributeMarkUnderXml() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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

                [Implements("{|LS5004:use-case/spending.submit-expense|}")]
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5004 reports a documentation tag when the project declares attribute")]
    public Task ReportsADocumentationTagUnderAttribute() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>{|LS5004:use-case/spending.submit-expense|}</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(marks: "attribute"), UseCase()],
            assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS5004 accepts a mark in the declared xml style")]
    public Task AcceptsAMarkInXmlStyle() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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

    [Fact(DisplayName = "LS5004 accepts a mark in the declared attribute style")]
    public Task AcceptsAMarkInAttributeStyle() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
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
}
