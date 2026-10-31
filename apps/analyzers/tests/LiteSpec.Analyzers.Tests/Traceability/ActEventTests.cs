using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5007 and LS5008, the declared events of the marked act.</summary>
public sealed class ActEventTests
{
    /// <summary>LS5007 reports a marked method raising an undeclared event.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5007 reports a marked method raising an undeclared event")]
    public Task ReportsARaisedButUndeclaredEvent() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>
                {
                    protected void RaiseDomainEvent(IDomainEvent e) { }
                }

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent : IDomainEvent;

                /// <implements>event/expense.approved</implements>
                public sealed record ExpenseApprovedEvent : IDomainEvent;

                /// <implements>aggregate/spending.expense</implements>
                public sealed class Expense : AggregateRoot<object>
                {
                    /// <implements>use-case/spending.submit-expense</implements>
                    public void {|LS5007:Submit|}()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                        RaiseDomainEvent(new ExpenseApprovedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event(), Event("event/expense.approved", "Expense approved")],
            assemblyName: "Shop.Domain");

    /// <summary>LS5007 accepts a marked method raising only declared events.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5007 accepts a marked method raising only declared events")]
    public Task AcceptsAMethodRaisingOnlyDeclaredEvents() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
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

    /// <summary>LS5008 reports a declared event no marked method raises.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5008 reports a declared event no marked method raises")]
    public Task ReportsADeclaredButUnraisedEvent() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
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
                    public void {|LS5008:Submit|}()
                    {
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5008 reports once on the first marked method when none raises the event.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5008 reports once on the first marked method when none raises the event")]
    public Task ReportsOnceOnTheFirstMarkedMethod() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
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
                    public void {|LS5008:Submit|}()
                    {
                    }

                    /// <implements>use-case/spending.submit-expense</implements>
                    public void Resubmit()
                    {
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5008 accepts a declared event raised through a private helper.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5008 accepts a declared event raised through a private helper")]
    public Task AcceptsAnEventRaisedThroughAPrivateHelper() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
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
                    public void Submit() => ApplySubmission();

                    private void ApplySubmission()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5008 accepts a declared event another marked method raises.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5008 accepts a declared event another marked method raises")]
    public Task AcceptsAnEventAnotherMarkedMethodRaises() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
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

                    /// <implements>use-case/spending.submit-expense</implements>
                    public void Touch()
                    {
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");

    /// <summary>LS5008 reports the unraised event when one method serves two use cases.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5008 reports the unraised event when one method serves two use cases")]
    public Task ReportsTheUnraisedEventForOneOfTwoUseCases() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
            [("Source.cs", """
                namespace Claimwick.Spending;

                public abstract class AggregateRoot<TState>
                {
                    protected void RaiseDomainEvent(IDomainEvent e) { }
                }

                public interface IDomainEvent;

                /// <implements>event/expense.submitted</implements>
                public sealed record ExpenseSubmittedEvent : IDomainEvent;

                /// <implements>event/expense.approved</implements>
                public sealed record ExpenseApprovedEvent : IDomainEvent;

                /// <implements>aggregate/spending.expense</implements>
                public sealed class Expense : AggregateRoot<object>
                {
                    /// <implements>use-case/spending.submit-expense</implements>
                    /// <implements>use-case/spending.approve-expense</implements>
                    public void {|LS5008:Submit|}()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [
                ProjectFile(),
                Aggregate(),
                UseCase(raises: ["event/expense.submitted"]),
                UseCase("use-case/spending.approve-expense", "Approve expense", raises: ["event/expense.submitted", "event/expense.approved"]),
                Event(),
                Event("event/expense.approved", "Expense approved"),
            ],
            assemblyName: "Shop.Domain");

    /// <summary>LS5007 ignores a method with no use-case mark.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</proves>
    [Fact(DisplayName = "LS5007 ignores a method with no use-case mark")]
    public Task IgnoresAMethodWithNoUseCaseMark() =>
        Verify.AnalyzerWithRecordsAsync<ActEventAnalyzer>(
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
                    public void Submit()
                    {
                        RaiseDomainEvent(new ExpenseSubmittedEvent());
                    }
                }
                """)],
            [ProjectFile(), Aggregate(), UseCase(), Event()],
            assemblyName: "Shop.Domain");
}
