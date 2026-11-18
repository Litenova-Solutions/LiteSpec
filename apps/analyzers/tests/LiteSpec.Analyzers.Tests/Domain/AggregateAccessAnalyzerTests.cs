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
        namespace Claimwick.Spending.Domain.Expense;

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
        namespace Claimwick.Spending.Domain.Expense;

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
        namespace Claimwick.Spending.Domain.Expense;

        public sealed class ExpenseLine : Entity<ExpenseLineId>
        {
            public ExpenseLine(ExpenseLineId id)
            {
            }
        }
        """;

    private const string Input = """
        namespace Claimwick.Spending.Domain.Expense;

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
                ("Claimwick.Spending/Domain/Expense/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expense/ExpenseLine.cs", Line),
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expense;

                    namespace Claimwick.Spending.UseCases.SubmitExpense;

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

    /// <summary>LS1014 reports a handler constructing the aggregate root itself.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 reports a handler constructing the aggregate root itself")]
    public Task ReportsAHandlerConstructingTheRoot() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expense/Expense.cs", Root),
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expense;

                    namespace Claimwick.Spending.UseCases.SubmitExpense;

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
                ("Claimwick.Spending/Domain/Expense/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expense/Events/ExpenseClosedEvent.cs", """
                    namespace Claimwick.Spending.Domain.Expense;

                    public sealed class ExpenseClosedEvent
                    {
                    }
                    """),
                ("Claimwick.Spending/Domain/Expense/Exceptions/ExpenseClosedException.cs", """
                    namespace Claimwick.Spending.Domain.Expense;

                    public sealed class ExpenseClosedException : System.Exception
                    {
                    }
                    """),
                ("Claimwick.Spending/UseCases/CloseExpense/Application/CloseExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expense;

                    namespace Claimwick.Spending.UseCases.CloseExpense;

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
                ("Claimwick.Spending/Domain/Expense/Expense.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expense;

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
                ("Claimwick.Spending/Domain/Expense/ExpenseLine.cs", Line),
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
                ("Claimwick.Spending/Domain/Expense/Expense.cs", RootWithInput),
                ("Claimwick.Spending/Domain/Expense/ExpenseLineInput.cs", Input),
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain.Expense;

                    namespace Claimwick.Spending.UseCases.SubmitExpense;

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
                ("Claimwick.Spending/Domain/Expense/Expense.cs", Root),
                ("Claimwick.Spending/Domain/Expense/CorrelationKey.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expense;

                    public sealed class CorrelationKey : IStronglyTypedId
                    {
                        public CorrelationKey(string value)
                        {
                        }
                    }
                    """),
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expense;

                    namespace Claimwick.Spending.UseCases.SubmitExpense;

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

    /// <summary>LS1014 reports construction from the old modules project layout.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</proves>
    [Fact(DisplayName = "LS1014 reports construction from the old modules project layout")]
    public Task ReportsConstructionUnderOneModulesProject() =>
        Verify.AnalyzerAsync<AggregateAccessAnalyzer>(
            [
                ("Claimwick.Modules/Spending/Stubs.cs", Stubs),
                ("Claimwick.Modules/Spending/Expense/Domain/Expense.cs", Root),
                ("Claimwick.Modules/Spending/Expense/Domain/ExpenseLine.cs", Line),
                ("Claimwick.Modules/Spending/Expense/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expense;

                    namespace Claimwick.Spending.UseCases.SubmitExpense;

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
                ("Claimwick.Spending.Tests/Domain/Expense/Expense.cs", Root),
                ("Claimwick.Spending.Tests/Domain/Expense/ExpenseLine.cs", Line),
                ("Claimwick.Spending.Tests/ExpenseTests.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Expense;

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
}
