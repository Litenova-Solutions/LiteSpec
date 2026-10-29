using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests every traceability code fix: the before and after, and the Fix All policy.</summary>
public sealed class MarkCodeFixTests
{
    [Fact(DisplayName = "Add mark offers one action per candidate record titled with the record title")]
    public Task AddMarkOffersOneActionPerCandidate() =>
        Verify.CodeFixFilesAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            [("Spending/SubmitExpenseCommandHandler.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [("Spending/SubmitExpenseCommandHandler.cs", """
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
            "Submit expense",
            [ProjectFile(), UseCase(), UseCase("use-case/shopping.submit-expense", "Submit shopping expense")],
            "Shop.Application",
            null,
            true,
            2);

    [Fact(DisplayName = "Add mark writes an attribute when the project declares attribute")]
    public Task AddMarkWritesAnAttribute() =>
        Verify.CodeFixFilesAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            [("Spending/SubmitExpenseCommandHandler.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [("Spending/SubmitExpenseCommandHandler.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                [LiteSpec.Implements("use-case/spending.submit-expense")]
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            "Submit expense",
            [ProjectFile(marks: "attribute"), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Add mark writes a proves tag on a test")]
    public Task AddMarkWritesAProvesTagOnATest() =>
        Verify.CodeFixFilesAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            [("SubmitExpense/ExpenseTests.cs", """
                using System;

                namespace Claimwick.Spending.Tests;

                public sealed class FactAttribute : Attribute;

                public sealed class ExpenseTests
                {
                    [Fact]
                    public void Submitting_with_a_receipt_succeeds()
                    {
                    }
                }
                """)],
            [("SubmitExpense/ExpenseTests.cs", """
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
            "Submitting with a receipt succeeds",
            [ProjectFile(), Scenario()],
            "Shop.Domain.Tests");

    [Fact(DisplayName = "Correct mark offers the three nearest records of the expected type")]
    public Task CorrectMarkOffersTheThreeNearestRecords() =>
        Verify.CodeFixAsync<MarkLinkAnalyzer, CorrectMarkCodeFixProvider>(
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <implements>use-case/spending.submit-expence</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "use-case/spending.submit-expense",
            [
                ProjectFile(),
                UseCase(),
                UseCase("use-case/spending.approve-expense", "Approve expense"),
                UseCase("use-case/spending.reimburse-expense", "Reimburse expense"),
            ],
            "Shop.Application",
            expectedActionCount: 3);

    [Fact(DisplayName = "Convert mark turns a documentation tag into an attribute")]
    public Task ConvertMarkTurnsATagIntoAnAttribute() =>
        Verify.CodeFixAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            [LiteSpec.Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "Convert mark to attribute",
            [ProjectFile(marks: "attribute"), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Convert mark turns a follows tag into an attribute")]
    public Task ConvertMarkTurnsAFollowsTagIntoAnAttribute() =>
        Verify.CodeFixAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Spending;

            /// <follows>decision/0002-store-expenses-as-documents</follows>
            internal sealed class CommitStep
            {
                public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Spending;

            [LiteSpec.Follows("decision/0002-store-expenses-as-documents")]
            internal sealed class CommitStep
            {
                public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """,
            "Convert mark to attribute",
            [ProjectFile(marks: "attribute"), Decision()],
            "Shop.Application");

    [Fact(DisplayName = "Convert mark turns a follows attribute into a documentation tag")]
    public Task ConvertMarkTurnsAFollowsAttributeIntoATag() =>
        Verify.CodeFixAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
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
            """,
            """
            using System;

            namespace Shop.Spending;

            internal sealed class FollowsAttribute : Attribute
            {
                public FollowsAttribute(string id) { }
            }

            /// <follows>decision/0002-store-expenses-as-documents</follows>
            internal sealed class CommitStep
            {
                public void Commit()
                {
                }
            }
            """,
            "Convert mark to documentation tag",
            [ProjectFile(), Decision()],
            "Shop.Application");

    [Fact(DisplayName = "Add mark offers no action for a technical class")]
    public async Task AddMarkOffersNoActionForATechnicalClass()
    {
        var titles = await Verify.CodeFixActionTitlesAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            [("Source.cs", """
                using System.Threading;
                using System.Threading.Tasks;

                namespace Shop.Spending;

                internal sealed class CommitStep
                {
                    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """)],
            [ProjectFile(), Decision()],
            "Shop.Application");

        Assert.Empty(titles);
    }

    [Fact(DisplayName = "Convert mark turns an attribute into a documentation tag")]
    public Task ConvertMarkTurnsAnAttributeIntoATag() =>
        Verify.CodeFixAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
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
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            internal sealed class ImplementsAttribute : Attribute
            {
                public ImplementsAttribute(string id) { }
            }

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "Convert mark to documentation tag",
            [ProjectFile(), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Add partial declares the type partial")]
    public Task AddPartialDeclaresTheTypePartial() =>
        Verify.CodeFixAsync<PartialTypeAnalyzer, AddPartialCodeFixProvider>(
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>event/expense.submitted</implements>
            public sealed record ExpenseSubmittedEvent : IDomainEvent;
            """,
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>event/expense.submitted</implements>
            public sealed partial record ExpenseSubmittedEvent : IDomainEvent;
            """,
            "Declare type partial",
            [ProjectFile(), Event()],
            "Shop.Domain");

    [Fact(DisplayName = "Delete value removes the drifting hand-written Classification")]
    public Task DeleteValueRemovesTheDriftingClassification() =>
        Verify.CodeFixAsync<RuntimeValueAnalyzer, DeleteHandWrittenValueCodeFixProvider>(
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>event/expense.submitted</implements>
            public sealed partial record ExpenseSubmittedEvent : IDomainEvent
            {
                public const string Classification = "event/expense.placed";
            }
            """,
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>event/expense.submitted</implements>
            public sealed partial record ExpenseSubmittedEvent : IDomainEvent
            {
            }
            """,
            "Delete hand-written 'Classification' (the record states 'event/expense.submitted')",
            [ProjectFile(), Event()],
            "Shop.Domain");

    [Fact(DisplayName = "Delete value removes the hand-written value equal to the record")]
    public Task DeleteValueRemovesTheValueEqualToTheRecord() =>
        Verify.CodeFixAsync<PartialTypeAnalyzer, DeleteHandWrittenValueCodeFixProvider>(
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>event/expense.submitted</implements>
            public sealed record ExpenseSubmittedEvent : IDomainEvent
            {
                public const string Classification = "event/expense.submitted";
            }
            """,
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>event/expense.submitted</implements>
            public sealed record ExpenseSubmittedEvent : IDomainEvent
            {
            }
            """,
            "Delete hand-written 'Classification' equal to the record",
            [ProjectFile(), Event()],
            "Shop.Domain");

    [Fact(DisplayName = "Fix All converts every mark across the solution")]
    public Task FixAllConvertsEveryMark() =>
        Verify.CodeFixAllAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            [
                ("Submit.cs", """
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
                    """),
                ("Approve.cs", """
                    namespace Claimwick.Spending;

                    public sealed class ApproveExpenseCommand;

                    public sealed class ApproveExpenseCommandResult;

                    [Implements("use-case/spending.approve-expense")]
                    internal sealed class ApproveExpenseCommandHandler : ICommandHandler<ApproveExpenseCommand, ApproveExpenseCommandResult>
                    {
                    }
                    """),
            ],
            [
                ("Submit.cs", """
                    using System;

                    namespace Claimwick.Spending;

                    public interface ICommandHandler<TCommand, TResult>;

                    public sealed class SubmitExpenseCommand;

                    public sealed class SubmitExpenseCommandResult;

                    internal sealed class ImplementsAttribute : Attribute
                    {
                        public ImplementsAttribute(string id) { }
                    }

                    /// <implements>use-case/spending.submit-expense</implements>
                    internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                    {
                    }
                    """),
                ("Approve.cs", """
                    namespace Claimwick.Spending;

                    public sealed class ApproveExpenseCommand;

                    public sealed class ApproveExpenseCommandResult;

                    /// <implements>use-case/spending.approve-expense</implements>
                    internal sealed class ApproveExpenseCommandHandler : ICommandHandler<ApproveExpenseCommand, ApproveExpenseCommandResult>
                    {
                    }
                    """),
            ],
            [ProjectFile(), UseCase(), UseCase("use-case/spending.approve-expense", "Approve expense")],
            "ConvertMarkStyle",
            "Shop.Application");

    [Fact(DisplayName = "Add mark and correct mark offer no Fix All")]
    public void AddMarkAndCorrectMarkOfferNoFixAll()
    {
        Assert.Null(new AddMarkCodeFixProvider().GetFixAllProvider());
        Assert.Null(new CorrectMarkCodeFixProvider().GetFixAllProvider());
    }

    [Fact(DisplayName = "Convert mark, add partial and delete value offer Fix All")]
    public void ConvertAddPartialAndDeleteOfferFixAll()
    {
        Assert.NotNull(new ConvertMarkStyleCodeFixProvider().GetFixAllProvider());
        Assert.NotNull(new AddPartialCodeFixProvider().GetFixAllProvider());
        Assert.NotNull(new DeleteHandWrittenValueCodeFixProvider().GetFixAllProvider());
    }

    [Fact(DisplayName = "Add mark writes a qualified attribute that compiles with the generator")]
    public Task AddMarkWritesQualifiedAttributeCompilingWithGenerator() =>
        Verify.CodeFixWithGeneratorAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            [LiteSpec.Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "Submit expense",
            [ProjectFile(marks: "attribute"), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Convert mark writes a qualified attribute that compiles with the generator")]
    public Task ConvertMarkWritesQualifiedAttributeCompilingWithGenerator() =>
        Verify.CodeFixWithGeneratorAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            [LiteSpec.Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "Convert mark to attribute",
            [ProjectFile(marks: "attribute"), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Add mark ranks the module-folder exact match first in the real layout")]
    public async Task AddMarkRanksModuleFolderExactFirst()
    {
        var titles = await Verify.CodeFixActionTitlesAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            [("Spending/Expense/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase("use-case/billing.submit-expense", "Submit billing expense"), UseCase()],
            "Shop.Application");

        Assert.Equal(["Submit expense", "Submit billing expense"], titles);
    }

    [Fact(DisplayName = "Add mark falls back to identifier order with no exact home")]
    public async Task AddMarkFallsBackToIdentifierOrder()
    {
        var titles = await Verify.CodeFixActionTitlesAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            [("Other/Thing/SubmitExpenseCommandHandler.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
                {
                }
                """)],
            [ProjectFile(), UseCase("use-case/billing.submit-expense", "Submit billing expense"), UseCase()],
            "Shop.Application");

        Assert.Equal(["Submit billing expense", "Submit expense"], titles);
    }

    [Fact(DisplayName = "Add mark keeps the summary and adds the tag after it")]
    public Task AddMarkKeepsSummary() =>
        Verify.CodeFixAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <summary>Submits an expense.</summary>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <summary>Submits an expense.</summary>
            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "Submit expense",
            [ProjectFile(), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Add mark keeps the summary and adds the qualified attribute after it")]
    public Task AddMarkAttributeKeepsSummary() =>
        Verify.CodeFixAsync<MarkPresenceAnalyzer, AddMarkCodeFixProvider>(
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <summary>Submits an expense.</summary>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <summary>Submits an expense.</summary>
            [LiteSpec.Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "Submit expense",
            [ProjectFile(marks: "attribute"), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Convert mark to attribute keeps the summary")]
    public Task ConvertMarkToAttributeKeepsSummary() =>
        Verify.CodeFixAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <summary>Submits an expense.</summary>
            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            """
            using System;

            namespace Claimwick.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed class SubmitExpenseCommand;

            public sealed class SubmitExpenseCommandResult;

            /// <summary>Submits an expense.</summary>
            [LiteSpec.Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
            {
            }
            """,
            "Convert mark to attribute",
            [ProjectFile(marks: "attribute"), UseCase()],
            "Shop.Application");

    [Fact(DisplayName = "Convert mark to tag keeps the summary")]
    public Task ConvertMarkToXmlKeepsSummary() =>
        Verify.CodeFixFilesAsync<MarkLinkAnalyzer, ConvertMarkStyleCodeFixProvider>(
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <summary>Submits an expense.</summary>
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
            [("Source.cs", """
                using System;

                namespace Claimwick.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed class SubmitExpenseCommand;

                public sealed class SubmitExpenseCommandResult;

                /// <summary>Submits an expense.</summary>
                /// <implements>use-case/spending.submit-expense</implements>
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
            "Convert mark to documentation tag",
            [ProjectFile(), UseCase()],
            "Shop.Application");
}
