using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests every traceability code fix: the before and after, and the Fix All policy.</summary>
public sealed class MarkCodeFixTests
{
    /// <summary>Convert mark turns a documentation tag into an attribute.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Convert mark turns a documentation tag into an attribute")]
    public Task ConvertMarkTurnsATagIntoAnAttribute() =>
        Verify.CodeFixAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
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

    /// <summary>Convert mark turns a follows tag into an attribute.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Convert mark turns a follows tag into an attribute")]
    public Task ConvertMarkTurnsAFollowsTagIntoAnAttribute() =>
        Verify.CodeFixAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Spending;

            /// <follows>decision/0002-keep-expenses-as-documents</follows>
            internal sealed class CommitStep
            {
                public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            namespace Shop.Spending;

            [LiteSpec.Follows("decision/0002-keep-expenses-as-documents")]
            internal sealed class CommitStep
            {
                public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """,
            "Convert mark to attribute",
            [ProjectFile(marks: "attribute"), Decision()],
            "Shop.Application");

    /// <summary>Convert mark turns a follows attribute into a documentation tag.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Convert mark turns a follows attribute into a documentation tag")]
    public Task ConvertMarkTurnsAFollowsAttributeIntoATag() =>
        Verify.CodeFixAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
            """
            using System;

            namespace Shop.Spending;

            internal sealed class FollowsAttribute : Attribute
            {
                public FollowsAttribute(string id) { }
            }

            [Follows("decision/0002-keep-expenses-as-documents")]
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

            /// <follows>decision/0002-keep-expenses-as-documents</follows>
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

    /// <summary>Convert mark turns an attribute into a documentation tag.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Convert mark turns an attribute into a documentation tag")]
    public Task ConvertMarkTurnsAnAttributeIntoATag() =>
        Verify.CodeFixAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
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

    /// <summary>Add partial declares the type partial.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    [Fact(DisplayName = "Add partial declares the type partial")]
    public Task AddPartialDeclaresTheTypePartial() =>
        Verify.CodeFixAsync<PartialTypeAnalyzer, AddPartialCodeFixProvider>(
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>domain-event/expense.submitted</implements>
            public sealed record ExpenseSubmittedEvent : IDomainEvent;
            """,
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>domain-event/expense.submitted</implements>
            public sealed partial record ExpenseSubmittedEvent : IDomainEvent;
            """,
            "Declare type partial",
            [ProjectFile(), DomainEvent()],
            "Shop.Domain");

    /// <summary>Delete value removes the drifting hand-written Classification.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Delete value removes the drifting hand-written Classification")]
    public Task DeleteValueRemovesTheDriftingClassification() =>
        Verify.CodeFixAsync<RuntimeValueAnalyzer, DeleteHandWrittenValueCodeFixProvider>(
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>domain-event/expense.submitted</implements>
            public sealed partial record ExpenseSubmittedEvent : IDomainEvent
            {
                public const string Classification = "domain-event/expense.placed";
            }
            """,
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>domain-event/expense.submitted</implements>
            public sealed partial record ExpenseSubmittedEvent : IDomainEvent
            {
            }
            """,
            "Delete hand-written 'Classification' (the record states 'domain-event/expense.submitted')",
            [ProjectFile(), DomainEvent()],
            "Shop.Domain");

    /// <summary>Delete value removes the hand-written value equal to the record.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Delete value removes the hand-written value equal to the record")]
    public Task DeleteValueRemovesTheValueEqualToTheRecord() =>
        Verify.CodeFixAsync<PartialTypeAnalyzer, DeleteHandWrittenValueCodeFixProvider>(
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>domain-event/expense.submitted</implements>
            public sealed record ExpenseSubmittedEvent : IDomainEvent
            {
                public const string Classification = "domain-event/expense.submitted";
            }
            """,
            """
            namespace Claimwick.Spending;

            public interface IDomainEvent;

            /// <implements>domain-event/expense.submitted</implements>
            public sealed record ExpenseSubmittedEvent : IDomainEvent
            {
            }
            """,
            "Delete hand-written 'Classification' equal to the record",
            [ProjectFile(), DomainEvent()],
            "Shop.Domain");

    /// <summary>Fix All converts every mark across the solution.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Fix All converts every mark across the solution")]
    public Task FixAllConvertsEveryMark() =>
        Verify.CodeFixAllAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
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

    /// <summary>Convert mark, add partial and delete value offer Fix All.</summary>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    /// <proves>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</proves>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Convert mark, add partial and delete value offer Fix All")]
    public void ConvertAddPartialAndDeleteOfferFixAll()
    {
        Assert.NotNull(new ConvertMarkStyleCodeFixProvider().GetFixAllProvider());
        Assert.NotNull(new AddPartialCodeFixProvider().GetFixAllProvider());
        Assert.NotNull(new DeleteHandWrittenValueCodeFixProvider().GetFixAllProvider());
    }

    /// <summary>Convert mark writes a qualified attribute that compiles with the generator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Convert mark writes a qualified attribute that compiles with the generator")]
    public Task ConvertMarkWritesQualifiedAttributeCompilingWithGenerator() =>
        Verify.CodeFixWithGeneratorAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
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

    /// <summary>Convert mark to attribute keeps the summary.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Convert mark to attribute keeps the summary")]
    public Task ConvertMarkToAttributeKeepsSummary() =>
        Verify.CodeFixAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
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

    /// <summary>Convert mark to tag keeps the summary.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</proves>
    [Fact(DisplayName = "Convert mark to tag keeps the summary")]
    public Task ConvertMarkToXmlKeepsSummary() =>
        Verify.CodeFixFilesAsync<MarkStyleAnalyzer, ConvertMarkStyleCodeFixProvider>(
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
