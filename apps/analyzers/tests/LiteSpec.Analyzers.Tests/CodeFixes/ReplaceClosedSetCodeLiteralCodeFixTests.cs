using LiteSpec.Analyzers.CodeFixes;
using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.CodeFixes;

/// <summary>Tests the closed-set Code fix: the typed branch, the CodeValue comparison, and when the fix stays silent.</summary>
public sealed class ReplaceClosedSetCodeLiteralCodeFixTests
{
    /// <summary>The closed-set file the tests compare.</summary>
    private const string KindFile = "Claimwick.Spending/Domain/Expenses/ExpenseKind.cs";

    /// <summary>The closed set the tests compare, every case carrying its CodeValue constant.</summary>
    private const string KindSource = """
        namespace Claimwick.Spending.Domain.Expenses;

        public abstract record ExpenseKind
        {
            public abstract string Code { get; }
        }

        public sealed record StandardExpenseKind : ExpenseKind
        {
            public override string Code => "standard";

            public const string CodeValue = "standard";
        }

        public sealed record TravelExpenseKind : ExpenseKind
        {
            public override string Code => "travel";

            public const string CodeValue = "travel";
        }

        public sealed class Expense
        {
            public ExpenseKind Kind { get; }

            public Expense(ExpenseKind kind)
            {
                Kind = kind;
            }
        }
        """;

    /// <summary>The closed set the CodeValue test compares, the standard case declaring no constant yet.</summary>
    private const string KindSourceWithoutCodeValue = """
        namespace Claimwick.Spending.Domain.Expenses;

        public abstract record ExpenseKind
        {
            public abstract string Code { get; }
        }

        public sealed record StandardExpenseKind : ExpenseKind
        {
            public override string Code => "standard";
        }

        public sealed record TravelExpenseKind : ExpenseKind
        {
            public override string Code => "travel";

            public const string CodeValue = "travel";
        }

        public sealed class Expense
        {
            public ExpenseKind Kind { get; }

            public Expense(ExpenseKind kind)
            {
                Kind = kind;
            }
        }
        """;

    /// <summary>The closed set the CodeValue test leaves behind, Code reading the added constant.</summary>
    private const string KindSourceWithAddedCodeValue = """
        namespace Claimwick.Spending.Domain.Expenses;

        public abstract record ExpenseKind
        {
            public abstract string Code { get; }
        }

        public sealed record StandardExpenseKind : ExpenseKind
        {
            public override string Code => CodeValue;

            public const string CodeValue = "standard";
        }

        public sealed record TravelExpenseKind : ExpenseKind
        {
            public override string Code => "travel";

            public const string CodeValue = "travel";
        }

        public sealed class Expense
        {
            public ExpenseKind Kind { get; }

            public Expense(ExpenseKind kind)
            {
                Kind = kind;
            }
        }
        """;

    /// <summary>The closed set the ambiguity test compares, two cases returning the same literal.</summary>
    private const string KindSourceWithTwoStandards = """
        namespace Claimwick.Spending.Domain.Expenses;

        public abstract record ExpenseKind
        {
            public abstract string Code { get; }
        }

        public sealed record StandardExpenseKind : ExpenseKind
        {
            public override string Code => "standard";
        }

        public sealed record TravelExpenseKind : ExpenseKind
        {
            public override string Code => "standard";
        }

        public sealed class Expense
        {
            public ExpenseKind Kind { get; }

            public Expense(ExpenseKind kind)
            {
                Kind = kind;
            }
        }
        """;

    /// <summary>The handler file the tests fix.</summary>
    private const string HandlerFile = "Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs";

    /// <summary>The handler the branch tests fix.</summary>
    private const string HandlerSource = """
        using Claimwick.Spending.Domain.Expenses;

        namespace Claimwick.Spending.Application.SubmitExpense;

        internal sealed class SubmitExpenseCommandHandler
        {
            public bool Handle(Expense expense)
            {
                return expense.Kind.Code == "standard";
            }
        }
        """;

    /// <summary>The queryable handler file the tests fix.</summary>
    private const string QueryHandlerFile = "Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs";

    /// <summary>The queryable handler the CodeValue tests fix.</summary>
    private const string QueryHandlerSource = """
        using System.Linq;
        using Claimwick.Spending.Domain.Expenses;

        namespace Claimwick.Spending.Application.ListExpenses;

        internal sealed class ListExpensesQueryHandler
        {
            public IQueryable<Expense> Handle(IQueryable<Expense> query)
            {
                return query.Where(expense => expense.Kind.Code == "standard");
            }
        }
        """;

    /// <summary>The queryable handler reading the CodeValue constant.</summary>
    private const string QueryHandlerFixed = """
        using System.Linq;
        using Claimwick.Spending.Domain.Expenses;

        namespace Claimwick.Spending.Application.ListExpenses;

        internal sealed class ListExpensesQueryHandler
        {
            public IQueryable<Expense> Handle(IQueryable<Expense> query)
            {
                return query.Where(expense => expense.Kind.Code == StandardExpenseKind.CodeValue);
            }
        }
        """;

    /// <summary>The refusal file the silence test reads.</summary>
    private const string RefusalFile = "Claimwick.Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs";

    /// <summary>The refusal carrying its code as a literal, which the fix leaves alone.</summary>
    private const string RefusalSource = """
        using System;

        namespace Claimwick.Spending.Domain.Expenses.Exceptions;

        public abstract class DomainException : Exception
        {
            protected DomainException(string message)
                : base(message)
            {
            }

            public abstract string Code { get; }
        }

        internal sealed class ExpenseReceiptMissingException : DomainException
        {
            public ExpenseReceiptMissingException()
                : base("An expense above the receipt threshold carries no receipt.")
            {
            }

            public override string Code => "spending.receipt-missing";
        }
        """;

    /// <summary>Branch fix turns a Code equality into a typed case check.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Branch fix turns a Code equality into a typed case check")]
    public Task TurnsACodeEqualityIntoATypedCaseCheck() =>
        Verify.CodeFixFilesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (HandlerFile, HandlerSource),
                (KindFile, KindSource),
            ],
            [
                (HandlerFile, """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind is StandardExpenseKind;
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            "Branch on 'StandardExpenseKind'",
            [],
            "Claimwick.Spending");

    /// <summary>Branch fix turns a Code inequality into a negated typed case check.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Branch fix turns a Code inequality into a negated typed case check")]
    public Task TurnsACodeInequalityIntoANegatedTypedCaseCheck() =>
        Verify.CodeFixFilesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (HandlerFile, """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind.Code != "travel";
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            [
                (HandlerFile, """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind is not TravelExpenseKind;
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            "Branch on 'TravelExpenseKind'",
            [],
            "Claimwick.Spending");

    /// <summary>Branch fix turns a literal on the left into a typed case check.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Branch fix turns a literal on the left into a typed case check")]
    public Task TurnsALiteralOnTheLeftIntoATypedCaseCheck() =>
        Verify.CodeFixFilesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (HandlerFile, """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return "standard" == expense.Kind.Code;
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            [
                (HandlerFile, """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind is StandardExpenseKind;
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            "Branch on 'StandardExpenseKind'",
            [],
            "Claimwick.Spending");

    /// <summary>CodeValue fix reads the constant in a queryable expression.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "CodeValue fix reads the constant in a queryable expression")]
    public Task ReadsTheConstantInAQueryableExpression() =>
        Verify.CodeFixFilesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (QueryHandlerFile, QueryHandlerSource),
                (KindFile, KindSource),
            ],
            [
                (QueryHandlerFile, QueryHandlerFixed),
                (KindFile, KindSource),
            ],
            "Compare 'StandardExpenseKind.CodeValue'",
            [],
            "Claimwick.Spending");

    /// <summary>CodeValue fix adds the constant and points Code at it.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "CodeValue fix adds the constant and points Code at it")]
    public Task AddsTheConstantAndPointsCodeAtIt() =>
        Verify.CodeFixFilesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (QueryHandlerFile, QueryHandlerSource),
                (KindFile, KindSourceWithoutCodeValue),
            ],
            [
                (QueryHandlerFile, QueryHandlerFixed),
                (KindFile, KindSourceWithAddedCodeValue),
            ],
            "Compare 'StandardExpenseKind.CodeValue'",
            [],
            "Claimwick.Spending");

    /// <summary>Fix All branches two comparisons on their typed cases.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Fix All branches two comparisons on their typed cases")]
    public Task FixAllBranchesTwoComparisonsOnTheirTypedCases() =>
        Verify.CodeFixAllAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (HandlerFile, HandlerSource),
                ("Claimwick.Spending/Application/ApproveExpense/ApproveExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ApproveExpense;

                    internal sealed class ApproveExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind.Code == "standard";
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            [
                (HandlerFile, """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind is StandardExpenseKind;
                        }
                    }
                    """),
                ("Claimwick.Spending/Application/ApproveExpense/ApproveExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ApproveExpense;

                    internal sealed class ApproveExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind is StandardExpenseKind;
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            [],
            "BranchOnClosedSetCase",
            "Claimwick.Spending");

    /// <summary>Branch fix stays silent when no case returns the literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Branch fix stays silent when no case returns the literal")]
    public async Task StaysSilentWhenNoCaseReturnsTheLiteral()
    {
        var titles = await Verify.CodeFixActionTitlesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (HandlerFile, """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind.Code == "express";
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            [],
            "Claimwick.Spending");

        Assert.Empty(titles);
    }

    /// <summary>Branch fix stays silent when two cases return the literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Branch fix stays silent when two cases return the literal")]
    public async Task StaysSilentWhenTwoCasesReturnTheLiteral()
    {
        var titles = await Verify.CodeFixActionTitlesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [
                (HandlerFile, HandlerSource),
                (KindFile, KindSourceWithTwoStandards),
            ],
            [],
            "Claimwick.Spending");

        Assert.Empty(titles);
    }

    /// <summary>Branch fix stays silent for a refusal Code written as a literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Branch fix stays silent for a refusal Code written as a literal")]
    public async Task StaysSilentForARefusalCodeWrittenAsALiteral()
    {
        var titles = await Verify.CodeFixActionTitlesAsync<ClosedSetCodeLiteralAnalyzer, ReplaceClosedSetCodeLiteralCodeFixProvider>(
            [(RefusalFile, RefusalSource)],
            [],
            "Claimwick.Spending");

        Assert.Empty(titles);
    }

    /// <summary>Branch fix offers Fix All.</summary>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "Branch fix offers Fix All")]
    public void OffersFixAll()
    {
        Assert.NotNull(new ReplaceClosedSetCodeLiteralCodeFixProvider().GetFixAllProvider());
    }
}
