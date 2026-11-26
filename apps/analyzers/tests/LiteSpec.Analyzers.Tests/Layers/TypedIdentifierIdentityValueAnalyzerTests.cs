using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2037, the identity value read rule.</summary>
public sealed class TypedIdentifierIdentityValueAnalyzerTests
{
    /// <summary>The expense identity and aggregate the test sources name, declared in one file of each project.</summary>
    private const string Stubs = """
        using System;

        namespace Claimwick.Spending.Domain.Expenses
        {
            public readonly record struct ExpenseId(Guid Value);

            public readonly record struct OwnerId(Guid Value);

            public sealed class Expense
            {
                public ExpenseId Id { get; set; }

                public OwnerId Owner { get; set; }
            }
        }
        """;

    /// <summary>LS2037 reports an identity value comparison in a queryable expression.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2037 reports an identity value comparison in a queryable expression")]
    public Task ReportsAnIdentityValueComparison() =>
        Verify.AnalyzerAsync<TypedIdentifierIdentityValueAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses
                    {
                        internal sealed class ListExpensesQueryHandler
                        {
                            public IQueryable<Expense> Handle(IQueryable<Expense> query, ExpenseId id)
                            {
                                return query.Where(expense => expense.Id.{|LS2037:Value|} == id.Value);
                            }
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2037 reports an identity value CompareTo in a queryable expression.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2037 reports an identity value CompareTo in a queryable expression")]
    public Task ReportsAnIdentityValueCompareTo() =>
        Verify.AnalyzerAsync<TypedIdentifierIdentityValueAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses
                    {
                        internal sealed class ListExpensesQueryHandler
                        {
                            public IQueryable<Expense> Handle(IQueryable<Expense> query, Guid cursor)
                            {
                                return query.Where(expense => expense.Id.{|LS2037:Value|}.CompareTo(cursor) < 0);
                            }
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2037 accepts a value read on a member that is no identity in a queryable expression.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2037 accepts a value read on a member that is no identity in a queryable expression")]
    public Task AcceptsAValueReadOnANonIdentityMember() =>
        Verify.AnalyzerAsync<TypedIdentifierIdentityValueAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses
                    {
                        internal sealed class ListExpensesQueryHandler
                        {
                            public IQueryable<Expense> Handle(IQueryable<Expense> query, OwnerId owner)
                            {
                                return query.Where(expense => expense.Owner.Value == owner.Value);
                            }
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2037 stays quiet for an identity value read in memory.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2037 stays quiet for an identity value read in memory")]
    public Task StaysQuietForAnInMemoryValueRead() =>
        Verify.AnalyzerAsync<TypedIdentifierIdentityValueAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System.Collections.Generic;
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses
                    {
                        internal sealed class ListExpensesQueryHandler
                        {
                            public IEnumerable<Expense> Handle(IEnumerable<Expense> expenses, ExpenseId id)
                            {
                                return expenses.Where(expense => expense.Id.Value == id.Value);
                            }
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2037 stays quiet for an expression tree passed to a method that is no queryable operator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2037 stays quiet for an expression tree passed to a method that is no queryable operator")]
    public Task StaysQuietForANonQueryableOperator() =>
        Verify.AnalyzerAsync<TypedIdentifierIdentityValueAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Linq.Expressions;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses
                    {
                        internal sealed class ExpenseFinder
                        {
                            public Expense? Find(Expression<Func<Expense, bool>> predicate) => null;
                        }

                        internal sealed class ListExpensesQueryHandler
                        {
                            public Expense? Handle(ExpenseFinder finder, ExpenseId id)
                            {
                                return finder.Find(expense => expense.Id.Value == id.Value);
                            }
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2037 stays quiet outside Application, Infrastructure and Worker.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2037 stays quiet outside Application, Infrastructure and Worker")]
    public Task StaysQuietOutsideItsLayers() =>
        Verify.AnalyzerAsync<TypedIdentifierIdentityValueAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses.cs", """
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Domain;

                    internal sealed class ExpenseQueries
                    {
                        public IQueryable<Expense> Handle(IQueryable<Expense> query, ExpenseId id)
                        {
                            return query.Where(expense => expense.Id.Value == id.Value);
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
