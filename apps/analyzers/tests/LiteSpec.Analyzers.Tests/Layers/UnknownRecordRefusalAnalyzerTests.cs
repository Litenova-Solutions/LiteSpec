using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2035, the unknown record refusal rule.</summary>
public sealed class UnknownRecordRefusalAnalyzerTests
{
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

            public sealed class ExpenseNotFoundException : Exception
            {
                public ExpenseNotFoundException(string message)
                    : base(message)
                {
                }
            }
        }
        """;

    /// <summary>The document session the test sources name, declared in one file of each project.</summary>
    private const string MartenStubs = """
        using System.Threading;
        using System.Threading.Tasks;

        namespace Marten
        {
            public interface IDocumentSession
            {
                Task<T?> LoadAsync<T>(object id, CancellationToken cancellationToken);
            }
        }
        """;

    /// <summary>LS2035 reports a coalesced throw after a repository find.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 reports a coalesced throw after a repository find")]
    public Task ReportsACoalescedThrowAfterAFind() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ReadExpense/ReadExpenseQueryHandler.cs", """
                    using System;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ReadExpense;

                    public interface IExpenseRepository
                    {
                        Task<Expense?> FindByIdAsync(ExpenseId id, CancellationToken cancellationToken);
                    }

                    internal sealed class ReadExpenseQueryHandler
                    {
                        private readonly IExpenseRepository _expenses;

                        public ReadExpenseQueryHandler(IExpenseRepository expenses)
                        {
                            _expenses = expenses;
                        }

                        public async Task<Expense> HandleAsync(ExpenseId id, CancellationToken cancellationToken)
                        {
                            var expense = await _expenses.FindByIdAsync(id, cancellationToken);
                            return expense ?? throw new {|LS2035:InvalidOperationException|}($"No expense with id '{id}'.");
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2035 reports an if-throw after a session load.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 reports an if-throw after a session load")]
    public Task ReportsAnIfThrowAfterALoad() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Domain/MartenStubs.cs", MartenStubs),
                ("Claimwick.Spending/Infrastructure/Persistence/Expenses/ExpenseRepository.cs", """
                    using System;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;
                    using Marten;

                    namespace Claimwick.Spending.Infrastructure.Persistence.Expenses;

                    internal sealed class ExpenseRepository
                    {
                        private readonly IDocumentSession _session;

                        public ExpenseRepository(IDocumentSession session)
                        {
                            _session = session;
                        }

                        public async Task<Expense> GetAsync(ExpenseId id, CancellationToken cancellationToken)
                        {
                            var expense = await _session.LoadAsync<Expense>(id, cancellationToken);
                            if (expense is null)
                            {
                                throw new {|LS2035:InvalidOperationException|}($"No expense with id '{id}'.");
                            }

                            return expense;
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2035 reports a bare exception thrown for an equality null check without braces.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 reports a bare exception thrown for an equality null check without braces")]
    public Task ReportsABareExceptionForAnEqualityCheck() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ReadExpense/ReadExpenseQueryHandler.cs", """
                    using System;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ReadExpense;

                    public interface IExpenseRepository
                    {
                        Task<Expense?> FindByIdAsync(ExpenseId id, CancellationToken cancellationToken);
                    }

                    internal sealed class ReadExpenseQueryHandler
                    {
                        private readonly IExpenseRepository _expenses;

                        public ReadExpenseQueryHandler(IExpenseRepository expenses)
                        {
                            _expenses = expenses;
                        }

                        public async Task<Expense> HandleAsync(ExpenseId id, CancellationToken cancellationToken)
                        {
                            var expense = await _expenses.FindByIdAsync(id, cancellationToken);
                            if (expense == null)
                                throw new {|LS2035:Exception|}($"No expense with id '{id}'.");
                            return expense;
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2035 reports a coalesced throw on a load call with no local.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 reports a coalesced throw on a load call with no local")]
    public Task ReportsACoalescedThrowOnADirectLoad() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ReadExpense/ReadExpenseQueryHandler.cs", """
                    using System;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ReadExpense;

                    public interface IExpenseRepository
                    {
                        Expense? FindById(ExpenseId id);
                    }

                    internal sealed class ReadExpenseQueryHandler
                    {
                        private readonly IExpenseRepository _expenses;

                        public ReadExpenseQueryHandler(IExpenseRepository expenses)
                        {
                            _expenses = expenses;
                        }

                        public Expense Handle(ExpenseId id)
                        {
                            return _expenses.FindById(id) ?? throw new {|LS2035:InvalidOperationException|}($"No expense with id '{id}'.");
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2035 accepts a coalesced throw of the not-found refusal.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 accepts a coalesced throw of the not-found refusal")]
    public Task AcceptsANotFoundRefusal() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ReadExpense/ReadExpenseQueryHandler.cs", """
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ReadExpense;

                    public interface IExpenseRepository
                    {
                        Task<Expense?> FindByIdAsync(ExpenseId id, CancellationToken cancellationToken);
                    }

                    internal sealed class ReadExpenseQueryHandler
                    {
                        private readonly IExpenseRepository _expenses;

                        public ReadExpenseQueryHandler(IExpenseRepository expenses)
                        {
                            _expenses = expenses;
                        }

                        public async Task<Expense> HandleAsync(ExpenseId id, CancellationToken cancellationToken)
                        {
                            var expense = await _expenses.FindByIdAsync(id, cancellationToken);
                            return expense ?? throw new ExpenseNotFoundException($"No expense with id '{id}'.");
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2035 stays quiet for a throw after an in-memory find.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 stays quiet for a throw after an in-memory find")]
    public Task StaysQuietForAnInMemoryFind() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ReadExpense/ReadExpenseQueryHandler.cs", """
                    using System;
                    using System.Collections.Generic;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ReadExpense;

                    internal sealed class ReadExpenseQueryHandler
                    {
                        public Expense Handle(List<Expense> expenses, ExpenseId id)
                        {
                            var expense = expenses.Find(candidate => candidate.Id.Equals(id));
                            if (expense is null)
                            {
                                throw new InvalidOperationException($"No expense with id '{id}'.");
                            }

                            return expense;
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2035 stays quiet for a null check after a required load.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 stays quiet for a null check after a required load")]
    public Task StaysQuietAfterARequiredLoad() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Application/ReadExpense/ReadExpenseQueryHandler.cs", """
                    using System;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.ReadExpense;

                    public interface IExpenseRepository
                    {
                        Task<Expense> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken);
                    }

                    internal sealed class ReadExpenseQueryHandler
                    {
                        private readonly IExpenseRepository _expenses;

                        public ReadExpenseQueryHandler(IExpenseRepository expenses)
                        {
                            _expenses = expenses;
                        }

                        public async Task<Expense> HandleAsync(ExpenseId id, CancellationToken cancellationToken)
                        {
                            var expense = await _expenses.GetByIdAsync(id, cancellationToken);
                            if (expense is null)
                            {
                                throw new InvalidOperationException($"No expense with id '{id}'.");
                            }

                            return expense;
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2035 stays quiet outside Application and Infrastructure.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</proves>
    [Fact(DisplayName = "LS2035 stays quiet outside Application and Infrastructure")]
    public Task StaysQuietOutsideItsLayers() =>
        Verify.AnalyzerAsync<UnknownRecordRefusalAnalyzer>(
            [
                ("Claimwick.Spending/Domain/DomainStubs.cs", DomainStubs),
                ("Claimwick.Spending/Domain/ReadExpenseQueryHandler.cs", """
                    using System;
                    using System.Threading;
                    using System.Threading.Tasks;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Domain;

                    public interface IExpenseRepository
                    {
                        Task<Expense?> FindByIdAsync(ExpenseId id, CancellationToken cancellationToken);
                    }

                    internal sealed class ReadExpenseQueryHandler
                    {
                        private readonly IExpenseRepository _expenses;

                        public ReadExpenseQueryHandler(IExpenseRepository expenses)
                        {
                            _expenses = expenses;
                        }

                        public async Task<Expense> HandleAsync(ExpenseId id, CancellationToken cancellationToken)
                        {
                            var expense = await _expenses.FindByIdAsync(id, cancellationToken);
                            return expense ?? throw new InvalidOperationException($"No expense with id '{id}'.");
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
