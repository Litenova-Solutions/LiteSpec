using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1015, public mutation surface on non-root aggregate types.</summary>
public sealed class AggregateMutationAnalyzerTests
{
    private const string Stubs = """
        namespace Claimwick.Spending.Domain;

        public abstract class AggregateRoot<TId>;

        public abstract class Entity<TId>;

        public readonly record struct ExpenseId(System.Guid Value);

        public readonly record struct ExpenseLineId(System.Guid Value);
        """;

    /// <summary>LS1015 reports a public setter on a child entity.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 reports a public setter on a child entity")]
    public Task ReportsAPublicSetterOnAChildEntity() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLine.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseLine : Entity<ExpenseLineId>
                    {
                        public int Quantity { get; {|LS1015:set|}; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1015 reports a public constructor on a child entity.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 reports a public constructor on a child entity")]
    public Task ReportsAPublicConstructorOnAChildEntity() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLine.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseLine : Entity<ExpenseLineId>
                    {
                        public {|LS1015:ExpenseLine|}(ExpenseLineId id)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1015 reports a mutable public collection on a non-root type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 reports a mutable public collection on a non-root type")]
    public Task ReportsAMutablePublicCollection() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLine.cs", """
                    using System.Collections.Generic;
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseLine : Entity<ExpenseLineId>
                    {
                        public List<int> {|LS1015:Quantities|} { get; } = new List<int>();

                        public List<int> {|LS1015:Tags|} = new List<int>();
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1015 reports a public setter on a value object struct.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 reports a public setter on a value object struct")]
    public Task ReportsAPublicSetterOnAStruct() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/MoneyLine.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    public struct MoneyLine
                    {
                        public int Quantity { get; {|LS1015:set|}; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1015 accepts the aggregate root with public surface.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 accepts the aggregate root with public surface")]
    public Task AcceptsTheAggregateRoot() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    using System.Collections.Generic;
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public Expense(ExpenseId id)
                        {
                        }

                        public int Total { get; set; }

                        public List<int> Lines { get; } = new List<int>();
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1015 accepts records, init accessors, and internal members.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 accepts records, init accessors, and internal members")]
    public Task AcceptsRecordsInitAccessorsAndInternalMembers() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/ExpenseSnapshot.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed record ExpenseSnapshot(int Quantity)
                    {
                        public ExpenseSnapshot()
                            : this(0)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLine.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseLine : Entity<ExpenseLineId>
                    {
                        internal ExpenseLine(ExpenseLineId id)
                        {
                        }

                        public int Quantity { get; init; }

                        internal int Total { get; set; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1015 accepts a public constructor on the root's input type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 accepts a public constructor on the root's input type")]
    public Task AcceptsAPublicConstructorOnAnInputType() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public static Expense Submit(ExpenseId id, ExpenseLineInput input)
                        {
                            return new Expense(id);
                        }

                        private Expense(ExpenseId id)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLineInput.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseLineInput
                    {
                        public ExpenseLineInput(int quantity)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1015 stays silent outside an aggregate folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes</proves>
    [Fact(DisplayName = "LS1015 stays silent outside an aggregate folder")]
    public Task StaysSilentOutsideAnAggregateFolder() =>
        Verify.AnalyzerAsync<AggregateMutationAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/SharedCounter.cs", """
                    namespace Claimwick.Spending.Domain;

                    public sealed class SharedCounter
                    {
                        public SharedCounter()
                        {
                        }

                        public int Total { get; set; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
