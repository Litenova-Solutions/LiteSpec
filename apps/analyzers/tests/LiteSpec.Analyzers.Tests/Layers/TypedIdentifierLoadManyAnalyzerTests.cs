using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2033, the typed identifier multi-load rule.</summary>
public sealed class TypedIdentifierLoadManyAnalyzerTests
{
    /// <summary>The Marten sessions the test sources name, declared in one file of each project.</summary>
    private const string MartenStubs = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Marten
        {
            public interface IDocumentSession
            {
                Task<IReadOnlyList<T>> LoadManyAsync<T>(Guid[] ids, CancellationToken cancellationToken);
            }

            public interface IQuerySession
            {
                Task<IReadOnlyList<T>> LoadManyAsync<T>(Guid[] ids, CancellationToken cancellationToken);
            }
        }
        """;

    /// <summary>The expense identity and aggregate the test sources name, declared in one file of each project.</summary>
    private const string DomainStubs = """
        using System;

        namespace Claimwick.Spending.Domain.Expenses
        {
            public readonly record struct ExpenseId(Guid Value);

            public sealed class Expense
            {
                public ExpenseId Id { get; set; }
            }

            public sealed class ExpenseArchive
            {
                public Guid Id { get; set; }
            }
        }
        """;

    /// <summary>LS2033 reports a LoadManyAsync on a document whose id member is a typed identifier.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time</proves>
    [Fact(DisplayName = "LS2033 reports a LoadManyAsync on a document whose id member is a typed identifier")]
    public Task ReportsALoadManyOnATypedIdentifierDocument() =>
        Verify.AnalyzerAsync<TypedIdentifierLoadManyAnalyzer>(
            [
                ("Claimwick.Spending/Domain/MartenStubs.cs", MartenStubs),
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Collections.Generic;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;
                    using Marten;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        private readonly IDocumentSession _session;

                        public ListExpensesQueryHandler(IDocumentSession session)
                        {
                            _session = session;
                        }

                        public Task<IReadOnlyList<Expense>> HandleAsync(Guid[] ids, CancellationToken cancellationToken) =>
                            _session.{|LS2033:LoadManyAsync|}<Expense>(ids, cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2033 reports a LoadManyAsync on the query session for a typed identifier document.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time</proves>
    [Fact(DisplayName = "LS2033 reports a LoadManyAsync on the query session for a typed identifier document")]
    public Task ReportsALoadManyOnTheQuerySession() =>
        Verify.AnalyzerAsync<TypedIdentifierLoadManyAnalyzer>(
            [
                ("Claimwick.Spending/Domain/MartenStubs.cs", MartenStubs),
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Collections.Generic;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;
                    using Marten;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        private readonly IQuerySession _session;

                        public ListExpensesQueryHandler(IQuerySession session)
                        {
                            _session = session;
                        }

                        public Task<IReadOnlyList<Expense>> HandleAsync(Guid[] ids, CancellationToken cancellationToken) =>
                            _session.{|LS2033:LoadManyAsync|}<Expense>(ids, cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2033 accepts a LoadManyAsync on a document whose id member is a plain value.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time</proves>
    [Fact(DisplayName = "LS2033 accepts a LoadManyAsync on a document whose id member is a plain value")]
    public Task AcceptsALoadManyOnAPlainIdentifierDocument() =>
        Verify.AnalyzerAsync<TypedIdentifierLoadManyAnalyzer>(
            [
                ("Claimwick.Spending/Domain/MartenStubs.cs", MartenStubs),
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Collections.Generic;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;
                    using Marten;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        private readonly IDocumentSession _session;

                        public ListExpensesQueryHandler(IDocumentSession session)
                        {
                            _session = session;
                        }

                        public Task<IReadOnlyList<ExpenseArchive>> HandleAsync(Guid[] ids, CancellationToken cancellationToken) =>
                            _session.LoadManyAsync<ExpenseArchive>(ids, cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2033 stays quiet for a LoadManyAsync on a type that is no session.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time</proves>
    [Fact(DisplayName = "LS2033 stays quiet for a LoadManyAsync on a type that is no session")]
    public Task StaysQuietForALoadManyOutsideASession() =>
        Verify.AnalyzerAsync<TypedIdentifierLoadManyAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ListExpenses/ListExpensesQueryHandler.cs", """
                    using System;
                    using System.Collections.Generic;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ListExpenses;

                    internal sealed class BulkLoader
                    {
                        public Task<IReadOnlyList<T>> LoadManyAsync<T>(Guid[] ids, CancellationToken cancellationToken) => null!;
                    }

                    internal sealed class ListExpensesQueryHandler
                    {
                        private readonly BulkLoader _loader;

                        public ListExpensesQueryHandler(BulkLoader loader)
                        {
                            _loader = loader;
                        }

                        public Task<IReadOnlyList<Expense>> HandleAsync(Guid[] ids, CancellationToken cancellationToken) =>
                            _loader.LoadManyAsync<Expense>(ids, cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2033 stays quiet outside Application, Infrastructure and Worker.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time</proves>
    [Fact(DisplayName = "LS2033 stays quiet outside Application, Infrastructure and Worker")]
    public Task StaysQuietOutsideItsLayers() =>
        Verify.AnalyzerAsync<TypedIdentifierLoadManyAnalyzer>(
            [
                ("Claimwick.Spending/Domain/MartenStubs.cs", MartenStubs),
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Domain/Expenses.cs", """
                    using System;
                    using System.Collections.Generic;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;
                    using Marten;

                    namespace Claimwick.Spending.Domain;

                    internal sealed class ExpenseQueries
                    {
                        private readonly IDocumentSession _session;

                        public ExpenseQueries(IDocumentSession session)
                        {
                            _session = session;
                        }

                        public Task<IReadOnlyList<Expense>> HandleAsync(Guid[] ids, CancellationToken cancellationToken) =>
                            _session.LoadManyAsync<Expense>(ids, cancellationToken);
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
