using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1014, construction of aggregate parts outside their Domain folder.</summary>
public sealed class AggregateAccessAnalyzerTests
{
    private const string Stubs = """
        namespace Claimwick.Spending.Domain;

        public abstract class AggregateRoot<TId>;

        public abstract class Entity<TId>;

        public interface IStronglyTypedId;

        public readonly record struct ExpenseId(System.Guid Value);

        public readonly record struct ExpenseLineId(System.Guid Value);
        """;

    private const string Root = """
        namespace Claimwick.Spending.Domain.Expenses;

        public sealed class Expense : AggregateRoot<ExpenseId>
        {
            public Expense(ExpenseId id)
            {
            }

            public static Expense Submit(ExpenseId id)
            {
                return new Expense(id);
            }
        }
        """;

    private const string RootWithInput = """
        namespace Claimwick.Spending.Domain.Expenses;

        public sealed class Expense : AggregateRoot<ExpenseId>
        {
            public Expense(ExpenseId id)
            {
            }

            public static Expense Submit(ExpenseId id, ExpenseLineInput input)
            {
                return new Expense(id);
            }
        }
        """;

    private const string Line = """
        namespace Claimwick.Spending.Domain.Expenses;

        public sealed class ExpenseLine : Entity<ExpenseLineId>
        {
            public ExpenseLine(ExpenseLineId id)
            {
            }
        }
        """;

    private const string Input = """
        namespace Claimwick.Spending.Domain.Expenses;

        public sealed record ExpenseLineInput(int Quantity);
        """;

    /// <summary>LS1014 reports a handler constructing an entity of an aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 reports a handler constructing an entity of an aggregate")]
    public Task ReportsAHandlerConstructingAnEntity() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLine.cs", Line),
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        internal void Handle(ExpenseLineId id)
                        {
                            var line = new {|LS1014:ExpenseLine|}(id);
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 accepts a handler constructing input records the root takes as a list, in a sub-namespace of the aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 accepts input records the root takes as a list")]
    public Task AcceptsInputRecordsTheRootTakesAsAList() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public Expense(ExpenseId id)
                        {
                        }

                        public static Expense Submit(ExpenseId id, System.Collections.Generic.IReadOnlyList<Inputs.ExpenseLineInput> lines)
                        {
                            return new Expense(id);
                        }
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/Inputs/ExpenseLineInput.cs", """
                    namespace Claimwick.Spending.Domain.Expenses.Inputs;

                    public sealed record ExpenseLineInput(int Quantity);
                    """),
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses.Inputs;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        internal ExpenseLineInput[] Handle()
                        {
                            return [new ExpenseLineInput(1)];
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 reports a handler constructing the aggregate root itself.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 reports a handler constructing the aggregate root itself")]
    public Task ReportsAHandlerConstructingTheRoot() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        internal void Handle(ExpenseId id)
                        {
                            var expense = new {|LS1014:Expense|}(id);
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 reports a handler constructing an event or an exception of an aggregate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 reports a handler constructing an event or an exception of an aggregate")]
    public Task ReportsAHandlerConstructingAnEventOrAnException() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expenses/Events/ExpenseClosedEvent.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseClosedEvent
                    {
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/Exceptions/ExpenseClosedException.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseClosedException : System.Exception
                    {
                    }
                    """),
                ("Claimwick.Spending/Application/Expenses/CloseExpense/CloseExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.Expenses.CloseExpense;

                    internal sealed class CloseExpenseCommandHandler
                    {
                        internal void Handle()
                        {
                            var fact = new {|LS1014:ExpenseClosedEvent|}();
                            throw new {|LS1014:ExpenseClosedException|}();
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 accepts the root constructing its parts inside its own folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 accepts the root constructing its parts inside its own folder")]
    public Task AcceptsConstructionInsideTheAggregateFolder() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public Expense(ExpenseId id)
                        {
                        }

                        public static Expense Submit(ExpenseId id, ExpenseLineId lineId)
                        {
                            var line = new ExpenseLine(lineId);
                            return new Expense(id);
                        }
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLine.cs", Line),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 accepts a handler constructing the root's input record.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 accepts a handler constructing the root's input record")]
    public Task AcceptsConstructionOfAnInputRecord() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", RootWithInput),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLineInput.cs", Input),
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        internal void Handle()
                        {
                            var input = new ExpenseLineInput(2);
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 accepts a handler constructing typed identifiers.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 accepts a handler constructing typed identifiers")]
    public Task AcceptsConstructionOfTypedIdentifiers() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expenses/CorrelationKey.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class CorrelationKey : IStronglyTypedId
                    {
                        public CorrelationKey(string value)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        internal void Handle()
                        {
                            var id = new ExpenseId(System.Guid.NewGuid());
                            var key = new CorrelationKey("submit");
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 reports construction from the one-modules-project deviation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 reports construction from the one-modules-project deviation")]
    public Task ReportsConstructionUnderOneModulesProject() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Modules/Spending/Contracts/Stubs.cs", Stubs),
                ("Claimwick.Modules/Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Modules/Spending/Domain/Expenses/ExpenseLine.cs", Line),
                ("Claimwick.Modules/Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        internal void Handle(ExpenseLineId id)
                        {
                            var line = new {|LS1014:ExpenseLine|}(id);
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS1014 stays silent in a test project.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 stays silent in a test project")]
    public Task StaysSilentInATestProject() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending.Tests/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending.Tests/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending.Tests/Domain/Expenses/ExpenseLine.cs", Line),
                ("Claimwick.Spending.Tests/ExpenseTests.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Tests;

                    internal sealed class ExpenseTests
                    {
                        internal void BuildsALine(ExpenseLineId id)
                        {
                            var line = new ExpenseLine(id);
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending.Tests");

    private const string Repository = """
        namespace Claimwick.Spending.Domain.Expenses;

        public sealed record ExpenseTotals(decimal Submitted, decimal Approved);

        public sealed record ExpenseYear(int Year);

        public interface IExpenseRepository
        {
            System.Threading.Tasks.Task<ExpenseTotals> TotalsAsync(ExpenseId id);

            System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ExpenseTotals>> AllTotalsAsync();

            System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ExpenseYear>> YearsAsync();
        }
        """;

    /// <summary>LS1014 accepts a repository building the query result its Domain interface returns.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 accepts a repository building the result its Domain interface returns")]
    public Task AcceptsARepositoryBuildingTheResultItsInterfaceReturns() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expenses/IExpenseRepository.cs", Repository),
                ("Claimwick.Spending/Infrastructure/Persistence/ExpenseRepository.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Infrastructure.Persistence;

                    internal sealed class ExpenseRepository : IExpenseRepository
                    {
                        public System.Threading.Tasks.Task<ExpenseTotals> TotalsAsync(ExpenseId id)
                        {
                            return System.Threading.Tasks.Task.FromResult(new ExpenseTotals(0m, 0m));
                        }

                        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ExpenseTotals>> AllTotalsAsync()
                        {
                            return System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IReadOnlyList<ExpenseTotals>>(new[] { new ExpenseTotals(1m, 1m) });
                        }

                        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ExpenseYear>> YearsAsync()
                        {
                            return System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IReadOnlyList<ExpenseYear>>(new[] { new ExpenseYear(2026) });
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 still reports a handler building the result a repository interface returns.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 reports a handler building the result a repository interface returns")]
    public Task ReportsAHandlerBuildingARepositoryResult() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expenses/IExpenseRepository.cs", Repository),
                ("Claimwick.Spending/Application/Expenses/ListExpenses/ListExpensesQueryHandler.cs", """
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Application.Expenses.ListExpenses;

                    internal sealed class ListExpensesQueryHandler
                    {
                        internal ExpenseTotals Handle()
                        {
                            return new {|LS1014:ExpenseTotals|}(0m, 0m);
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1014 accepts a repository building the result its interface returns inside a lambda.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 accepts a repository building its interface result inside a lambda")]
    public Task AcceptsARepositoryBuildingItsResultInsideALambda() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expenses/IExpenseRepository.cs", Repository),
                ("Claimwick.Spending/Infrastructure/Persistence/ExpenseRepository.cs", """
                    using System.Linq;
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expenses;

                    namespace Claimwick.Spending.Infrastructure.Persistence;

                    internal sealed class ExpenseRepository : IExpenseRepository
                    {
                        public System.Threading.Tasks.Task<ExpenseTotals> TotalsAsync(ExpenseId id)
                        {
                            var totals = new[] { 1m }.Select(amount => new ExpenseTotals(amount, amount)).First();
                            return System.Threading.Tasks.Task.FromResult(totals);
                        }

                        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ExpenseTotals>> AllTotalsAsync()
                        {
                            return System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IReadOnlyList<ExpenseTotals>>(new[] { new ExpenseTotals(1m, 1m) });
                        }

                        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ExpenseYear>> YearsAsync()
                        {
                            return System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IReadOnlyList<ExpenseYear>>(new[] { new ExpenseYear(2026) });
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
