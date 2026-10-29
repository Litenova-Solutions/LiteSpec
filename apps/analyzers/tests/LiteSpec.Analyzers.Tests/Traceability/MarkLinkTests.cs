using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5002, LS5003 and LS5004, the mark link, count and style.</summary>
public sealed class MarkLinkTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
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

    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
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

    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
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

    /// <proves>litespec/backend/domain#give-each-use-case-its-own-act</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
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

    /// <proves>litespec/core/model#follow-effective-status-in-code-findings</proves>
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

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
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

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
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

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
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

    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
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

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 reports a follows naming an unknown decision")]
    public Task ReportsAFollowsNamingAnUnknownDecision() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <follows>{|LS5002:decision/0099-no-such-decision|}</follows>
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 reports a follows naming a retired decision")]
    public Task ReportsAFollowsNamingARetiredDecision() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <follows>{|LS5002:decision/0002-store-expenses-as-documents|}</follows>
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision(status: "retired")],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 reports a follows naming an unknown provision")]
    public Task ReportsAFollowsNamingAnUnknownProvision() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <follows>{|LS5002:litespec/backend/no-such-page#no-such-provision|}</follows>
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 accepts a follows naming a provision of the carried release")]
    public Task AcceptsAFollowsNamingAProvision() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 accepts a follows naming a decision or deviation")]
    public Task AcceptsAFollowsNamingADecisionOrDeviation() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <follows>decision/0002-store-expenses-as-documents</follows>
                /// <follows>deviation/marks-deferred</follows>
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision(), Deviation(["mark-missing"])],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 accepts a follows on a handler beside its implements")]
    public Task AcceptsAFollowsOnAHandler() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <implements>use-case/spending.submit-expense</implements>
                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase(), Decision()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 reports an implements mark naming a non-Behavior record on a technical class")]
    public Task ReportsAnImplementsNamingNonBehaviorOnATechnicalClass() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <implements>{|LS5002:decision/0002-store-expenses-as-documents|}</implements>
                internal sealed class CommitStep
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 accepts an implements mark naming a use case on a technical class")]
    public Task AcceptsAnImplementsNamingAUseCaseOnATechnicalClass() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class SubmitExpenseValidator
                {
                    public void Validate()
                    {
                    }
                }
                """)],
            [ProjectFile(), UseCase()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5002 accepts a technical class with both verbs")]
    public Task AcceptsATechnicalClassWithBothVerbs() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <implements>aggregate/spending.expense</implements>
                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class ExpenseRepository
                {
                    public void Store()
                    {
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), Decision()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "LS5003 reports a technical class naming one decision twice")]
    public Task ReportsADuplicateFollows() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Spending;

                /// <follows>decision/0002-store-expenses-as-documents</follows>
                /// <follows>decision/0002-store-expenses-as-documents</follows>
                internal sealed class {|LS5003:CommitStep|}
                {
                    public void Commit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Application");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts a proves mark naming a rule on a test")]
    public Task AcceptsAProvesMarkNamingARuleOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>rule/spending.receipt-required</proves>
                    [Fact]
                    public void Receipt_is_required()
                    {
                    }
                }
                """)],
            [ProjectFile(), Rule()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts a proves mark naming a quality requirement on a test")]
    public Task AcceptsAProvesMarkNamingAQualityRequirementOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>requirement/borrowing.borrow-latency</proves>
                    [Fact]
                    public void Borrowing_stays_fast()
                    {
                    }
                }
                """)],
            [ProjectFile(), Requirement()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts a proves mark naming a technical decision on a test")]
    public Task AcceptsAProvesMarkNamingATechnicalDecisionOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>decision/0002-store-expenses-as-documents</proves>
                    [Fact]
                    public void Expenses_round_trip_as_documents()
                    {
                    }
                }
                """)],
            [ProjectFile(), TechnicalDecision()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts a proves mark naming an external system on a test")]
    public Task AcceptsAProvesMarkNamingAnExternalSystemOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>external-system/payments</proves>
                    [Fact]
                    public void Payments_settle()
                    {
                    }
                }
                """)],
            [ProjectFile(), ExternalSystem()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts a proves mark naming an integration event on a test")]
    public Task AcceptsAProvesMarkNamingAnIntegrationEventOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>integration-event/expense-submitted</proves>
                    [Fact]
                    public void Expense_submitted_publishes()
                    {
                    }
                }
                """)],
            [ProjectFile(), IntegrationEvent()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts a proves mark naming a provision on a test")]
    public Task AcceptsAProvesMarkNamingAProvisionOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>litespec/backend/persistence#commit-each-command-pipeline-once</proves>
                    [Fact]
                    public void Commits_once()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts a test proving two kinds")]
    public Task AcceptsATestProvingTwoKinds() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>scenario/submit-expense.submitting-with-a-receipt-succeeds</proves>
                    /// <proves>requirement/borrowing.borrow-latency</proves>
                    [Fact]
                    public void Submitting_with_a_receipt_succeeds()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario(), Requirement()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 reports a proves mark naming a functional requirement")]
    public Task ReportsAProvesMarkNamingAFunctionalRequirement() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>{|LS5002:requirement/borrowing.borrow-latency|}</proves>
                    [Fact]
                    public void Borrowing_stays_fast()
                    {
                    }
                }
                """)],
            [ProjectFile(), Requirement(kind: "functional")],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 reports a proves mark naming a product decision")]
    public Task ReportsAProvesMarkNamingAProductDecision() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>{|LS5002:decision/0002-store-expenses-as-documents|}</proves>
                    [Fact]
                    public void Expenses_round_trip_as_documents()
                    {
                    }
                }
                """)],
            [ProjectFile(), Decision()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 reports a proves mark naming an aggregate")]
    public Task ReportsAProvesMarkNamingAnAggregate() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>{|LS5002:aggregate/spending.expense|}</proves>
                    [Fact]
                    public void Expense_holds()
                    {
                    }
                }
                """)],
            [ProjectFile(), Aggregate()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 reports a proves mark naming an unknown provision")]
    public Task ReportsAProvesMarkNamingAnUnknownProvision() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>{|LS5002:litespec/backend/no-such-page#no-such-provision|}</proves>
                    [Fact]
                    public void Keeps_structure()
                    {
                    }
                }
                """)],
            [ProjectFile(), Scenario()],
            assemblyName: "Shop.Domain.Tests");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports one proof named twice on a test")]
    public Task ReportsOneProofNamedTwiceOnATest() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    /// <proves>requirement/borrowing.borrow-latency</proves>
                    /// <proves>requirement/borrowing.borrow-latency</proves>
                    [Fact]
                    public void {|LS5003:Borrowing_stays_fast|}()
                    {
                    }
                }
                """)],
            [ProjectFile(), Requirement()],
            assemblyName: "Shop.Domain.Tests");
}
