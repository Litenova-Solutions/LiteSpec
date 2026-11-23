using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2032, the typed identifier Contains rule.</summary>
public sealed class TypedIdentifierContainsAnalyzerTests
{
    /// <summary>The expense identity and aggregate the test sources name, declared in one file of each project.</summary>
    private const string Stubs = """
        using System;

        namespace Claimwick.Spending.Domain.Expenses
        {
            public readonly record struct ExpenseId(Guid Value);

            public sealed class Expense
            {
                public ExpenseId Id { get; set; }
            }
        }
        """;

    /// <summary>LS2032 reports an Enumerable Contains over typed identifiers in a queryable expression.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2032 reports an Enumerable Contains over typed identifiers in a queryable expression")]
    public Task ReportsAnEnumerableContainsOverTypedIdentifiers() =>
        Verify.AnalyzerAsync<TypedIdentifierContainsAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System.Collections.Generic;
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        public IQueryable<Expense> Handle(IQueryable<Expense> query, IEnumerable<ExpenseId> ids)
                        {
                            return query.Where(expense => ids.{|LS2032:Contains|}(expense.Id));
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2032 reports an instance Contains over typed identifiers in a queryable expression.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2032 reports an instance Contains over typed identifiers in a queryable expression")]
    public Task ReportsAnInstanceContainsOverTypedIdentifiers() =>
        Verify.AnalyzerAsync<TypedIdentifierContainsAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System.Collections.Generic;
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        public IQueryable<Expense> Handle(IQueryable<Expense> query, List<ExpenseId> ids)
                        {
                            return query.Where(expense => ids.{|LS2032:Contains|}(expense.Id));
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2032 accepts a Contains that compares the underlying value in a queryable expression.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2032 accepts a Contains that compares the underlying value in a queryable expression")]
    public Task AcceptsAContainsOverTheUnderlyingValue() =>
        Verify.AnalyzerAsync<TypedIdentifierContainsAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        public IQueryable<Expense> Handle(IQueryable<Expense> query, IEnumerable<Guid> values)
                        {
                            return query.Where(expense => values.Contains(expense.Id.Value));
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2032 stays quiet for a Contains over typed identifiers in memory.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2032 stays quiet for a Contains over typed identifiers in memory")]
    public Task StaysQuietForAnInMemoryContains() =>
        Verify.AnalyzerAsync<TypedIdentifierContainsAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System.Collections.Generic;
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        public IEnumerable<Expense> Handle(IEnumerable<Expense> expenses, IEnumerable<ExpenseId> ids)
                        {
                            return expenses.Where(expense => ids.Contains(expense.Id));
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2032 stays quiet for an expression tree passed to a method that is no queryable operator.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2032 stays quiet for an expression tree passed to a method that is no queryable operator")]
    public Task StaysQuietForANonQueryableOperator() =>
        Verify.AnalyzerAsync<TypedIdentifierContainsAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Collections.Generic;
                    using System.Linq;
                    using System.Linq.Expressions;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ExpenseFinder
                    {
                        public Expense? Find(Expression<Func<Expense, bool>> predicate) => null;
                    }

                    internal sealed class ListExpensesQueryHandler
                    {
                        public Expense? Handle(ExpenseFinder finder, IEnumerable<ExpenseId> ids)
                        {
                            return finder.Find(expense => ids.Contains(expense.Id));
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2032 stays quiet outside Application, Infrastructure and Worker.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</proves>
    [Fact(DisplayName = "LS2032 stays quiet outside Application, Infrastructure and Worker")]
    public Task StaysQuietOutsideItsLayers() =>
        Verify.AnalyzerAsync<TypedIdentifierContainsAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses.cs", """
                    using System.Collections.Generic;
                    using System.Linq;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Domain;

                    internal sealed class ExpenseQueries
                    {
                        public IQueryable<Expense> Handle(IQueryable<Expense> query, IEnumerable<ExpenseId> ids)
                        {
                            return query.Where(expense => ids.Contains(expense.Id));
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
