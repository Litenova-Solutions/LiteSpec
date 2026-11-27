using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2041, a closed-set Code compared or carried as a string literal.</summary>
public sealed class ClosedSetCodeLiteralAnalyzerTests
{
    /// <summary>The closed set the tests compare.</summary>
    private const string KindFile = "Claimwick.Spending/Domain/Expenses/ExpenseKind.cs";

    /// <summary>The closed set source the tests compare.</summary>
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

    /// <summary>The refusal file the trigger tests use.</summary>
    private const string RefusalFile = "Claimwick.Spending/Domain/Expenses/Exceptions/ExpenseReceiptMissingException.cs";

    /// <summary>The refusal base the trigger tests declare.</summary>
    private const string RefusalBase = """
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
        """;

    /// <summary>LS2041 reports a Code comparison to a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 reports a Code comparison to a string literal")]
    public Task ReportsAComparisonToALiteral() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind.Code == {|LS2041:"standard"|};
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 reports a Code inequality against a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 reports a Code inequality against a string literal")]
    public Task ReportsAnInequalityAgainstALiteral() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind.Code != {|LS2041:"travel"|};
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 reports a literal on the left of a Code comparison.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 reports a literal on the left of a Code comparison")]
    public Task ReportsALiteralOnTheLeftOfAComparison() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return {|LS2041:"standard"|} == expense.Kind.Code;
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 reports an instance Equals call with a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 reports an instance Equals call with a string literal")]
    public Task ReportsAnInstanceEqualsCallWithALiteral() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind.Code.Equals({|LS2041:"standard"|});
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 reports a static Equals call with a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 reports a static Equals call with a string literal")]
    public Task ReportsAStaticEqualsCallWithALiteral() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return string.Equals(expense.Kind.Code, {|LS2041:"standard"|});
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 reports a refusal Code written as a string literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 reports a refusal Code written as a string literal")]
    public Task ReportsARefusalCodeWrittenAsALiteral() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                (RefusalFile, RefusalBase + """

                    internal sealed class ExpenseReceiptMissingException : DomainException
                    {
                        public ExpenseReceiptMissingException()
                            : base("An expense above the receipt threshold carries no receipt.")
                        {
                        }

                        public override string Code => {|LS2041:"spending.receipt-missing"|};
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 reports a refusal Code returned from a getter.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 reports a refusal Code returned from a getter")]
    public Task ReportsARefusalCodeReturnedFromAGetter() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                (RefusalFile, RefusalBase + """

                    internal sealed class ExpenseReceiptMissingException : DomainException
                    {
                        public ExpenseReceiptMissingException()
                            : base("An expense above the receipt threshold carries no receipt.")
                        {
                        }

                        public override string Code
                        {
                            get
                            {
                                return {|LS2041:"spending.receipt-missing"|};
                            }
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 accepts a comparison against a CodeValue constant.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 accepts a comparison against a CodeValue constant")]
    public Task AcceptsAComparisonAgainstACodeValueConstant() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(Expense expense)
                        {
                            return expense.Kind.Code == StandardExpenseKind.CodeValue;
                        }
                    }
                    """),
                (KindFile, KindSource),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 accepts a typed case check with no literal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 accepts a typed case check with no literal")]
    public Task AcceptsATypedCaseCheckWithNoLiteral() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
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
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 accepts a refusal Code read from a synced constant.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 accepts a refusal Code read from a synced constant")]
    public Task AcceptsARefusalCodeReadFromASyncedConstant() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                (RefusalFile, RefusalBase + """

                    internal static class SpendingRefusalCodes
                    {
                        public const string ReceiptMissing = "spending.receipt-missing";
                    }

                    internal sealed class ExpenseReceiptMissingException : DomainException
                    {
                        public ExpenseReceiptMissingException()
                            : base("An expense above the receipt threshold carries no receipt.")
                        {
                        }

                        public override string Code => SpendingRefusalCodes.ReceiptMissing;
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 stays quiet for a Code on a concrete type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 stays quiet for a Code on a concrete type")]
    public Task StaysQuietForACodeOnAConcreteType() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal readonly record struct CountryKey(string Code);

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        public bool Handle(CountryKey key)
                        {
                            return key.Code == "FR";
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2041 stays quiet in a test project.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</proves>
    [Fact(DisplayName = "LS2041 stays quiet in a test project")]
    public Task StaysQuietInATestProject() =>
        Verify.AnalyzerAsync<ClosedSetCodeLiteralAnalyzer>(
            [
                ("Source.cs", """
                    namespace Claimwick.Spending.Tests;

                    public abstract record ExpenseKind
                    {
                        public abstract string Code { get; }
                    }

                    public sealed record StandardExpenseKind : ExpenseKind
                    {
                        public override string Code => "standard";
                    }

                    public sealed class ExpenseKindTests
                    {
                        public bool Matches(ExpenseKind kind)
                        {
                            return kind.Code == "standard";
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending.Tests");
}
