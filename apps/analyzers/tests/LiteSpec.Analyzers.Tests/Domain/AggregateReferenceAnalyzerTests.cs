using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1016, object references between aggregates.</summary>
public sealed class AggregateReferenceAnalyzerTests
{
    private const string Stubs = """
        namespace Claimwick.Spending.Domain;

        public abstract class AggregateRoot<TId>;

        public abstract class Entity<TId>;

        public readonly record struct ExpenseId(System.Guid Value);

        public readonly record struct CounterpartyId(System.Guid Value);
        """;

    private const string CounterpartyRoot = """
        using Claimwick.Spending.Domain;

        namespace Claimwick.Spending.Domain.Counterparties;

        public sealed class Counterparty : AggregateRoot<CounterpartyId>
        {
        }
        """;

    /// <summary>LS1016 reports a property typed as another aggregate's root.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier</proves>
    [Fact(DisplayName = "LS1016 reports a property typed as another aggregate's root")]
    public Task ReportsAPropertyTypedAsAnotherAggregatesRoot() =>
        Verify.AnalyzerAsync<AggregateReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Counterparties/Counterparty.cs", CounterpartyRoot),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    namespace Claimwick.Spending.Domain.Expenses;

                    using Claimwick.Spending.Domain.Counterparties;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public Counterparty {|LS1016:Counterparty|} { get; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1016 reports a property typed as another aggregate's entity.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier</proves>
    [Fact(DisplayName = "LS1016 reports a property typed as another aggregate's entity")]
    public Task ReportsAPropertyTypedAsAnotherAggregatesEntity() =>
        Verify.AnalyzerAsync<AggregateReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Counterparties/Counterparty.cs", CounterpartyRoot),
                ("Claimwick.Spending/Domain/Counterparties/CounterpartyContact.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Counterparties;

                    public sealed class CounterpartyContact : Entity<CounterpartyId>
                    {
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Counterparties;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public CounterpartyContact {|LS1016:Contact|} { get; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1016 accepts a property typed as the other aggregate's identifier.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier</proves>
    [Fact(DisplayName = "LS1016 accepts a property typed as the other aggregate's identifier")]
    public Task AcceptsAPropertyTypedAsATypedIdentifier() =>
        Verify.AnalyzerAsync<AggregateReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Counterparties/Counterparty.cs", CounterpartyRoot),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    using Claimwick.Spending.Domain;
                    using Claimwick.Spending.Domain.Counterparties;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public CounterpartyId CounterpartyId { get; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS1016 accepts a property typed as the same aggregate's own entity.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier</proves>
    [Fact(DisplayName = "LS1016 accepts a property typed as the same aggregate's own entity")]
    public Task AcceptsAPropertyTypedAsTheSameAggregate() =>
        Verify.AnalyzerAsync<AggregateReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Stubs.cs", Stubs),
                ("Claimwick.Spending/Domain/Expenses/ExpenseLine.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class ExpenseLine : Entity<ExpenseId>
                    {
                    }
                    """),
                ("Claimwick.Spending/Domain/Expenses/Expense.cs", """
                    using Claimwick.Spending.Domain;

                    namespace Claimwick.Spending.Domain.Expenses;

                    public sealed class Expense : AggregateRoot<ExpenseId>
                    {
                        public ExpenseLine FirstLine { get; }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
