using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Style;

/// <summary>Tests for LS3005, primary constructors on classes and structs.</summary>
public sealed class PrimaryConstructorAnalyzerTests
{
    /// <summary>LS3005 reports a class primary constructor at info severity.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs</proves>
    [Fact(DisplayName = "LS3005 reports a class primary constructor at info severity")]
    public Task ReportsAClassPrimaryConstructor() =>
        Verify.AnalyzerAsync<PrimaryConstructorAnalyzer>(
            [
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs", """
                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    public interface IExpenseRepository;

                    internal sealed class SubmitExpenseCommandHandler{|LS3005:Info:(IExpenseRepository expenses)|}
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS3005 reports a struct primary constructor at info severity.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs</proves>
    [Fact(DisplayName = "LS3005 reports a struct primary constructor at info severity")]
    public Task ReportsAStructPrimaryConstructor() =>
        Verify.AnalyzerAsync<PrimaryConstructorAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Money.cs", """
                    namespace Claimwick.Spending.Domain;

                    internal struct Money{|LS3005:Info:(decimal Amount)|}
                    {
                        public decimal Value => Amount;
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS3005 accepts a class with an explicit constructor.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs</proves>
    [Fact(DisplayName = "LS3005 accepts a class with an explicit constructor")]
    public Task AcceptsAClassWithAnExplicitConstructor() =>
        Verify.AnalyzerAsync<PrimaryConstructorAnalyzer>(
            [
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs", """
                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    public interface IExpenseRepository;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private readonly IExpenseRepository _expenses;

                        public SubmitExpenseCommandHandler(IExpenseRepository expenses)
                        {
                            _expenses = expenses;
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS3005 accepts positional records and record structs.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs</proves>
    [Fact(DisplayName = "LS3005 accepts positional records and record structs")]
    public Task AcceptsPositionalRecords() =>
        Verify.AnalyzerAsync<PrimaryConstructorAnalyzer>(
            [
                ("Claimwick.Spending/Domain/Money.cs", """
                    namespace Claimwick.Spending.Domain;

                    internal sealed record Money(decimal Amount, string Currency);

                    internal readonly record struct ExpenseId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
