using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2026, models duplicated across use case folders.</summary>
public sealed class DuplicateModelAnalyzerTests
{
    /// <summary>LS2026 reports identical records in two use case folders.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2026 reports identical records in two use case folders")]
    public Task ReportsIdenticalRecordsInTwoUseCaseFolders() =>
        Verify.AnalyzerAsync<DuplicateModelAnalyzer>(
            [
                ("Claimwick.Spending/WebApi/Expenses/ApproveExpense/MoneyModel.cs", """
                    namespace Claimwick.Spending.WebApi.Expenses.ApproveExpense;

                    internal sealed record MoneyModel(decimal Amount, string Currency);
                    """),
                ("Claimwick.Spending/WebApi/Expenses/SubmitExpense/MoneyModel.cs", """
                    namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

                    internal sealed record {|LS2026:MoneyModel|}(decimal Amount, string Currency);
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2026 accepts one record with no duplicate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2026 accepts one record with no duplicate")]
    public Task AcceptsOneRecordWithNoDuplicate() =>
        Verify.AnalyzerAsync<DuplicateModelAnalyzer>(
            [
                ("Claimwick.Spending/WebApi/Expenses/SubmitExpense/MoneyModel.cs", """
                    namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

                    internal sealed record MoneyModel(decimal Amount, string Currency);
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2026 accepts the same name with different members.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2026 accepts the same name with different members")]
    public Task AcceptsTheSameNameWithDifferentMembers() =>
        Verify.AnalyzerAsync<DuplicateModelAnalyzer>(
            [
                ("Claimwick.Spending/WebApi/Expenses/ApproveExpense/MoneyModel.cs", """
                    namespace Claimwick.Spending.WebApi.Expenses.ApproveExpense;

                    internal sealed record MoneyModel(decimal Amount, string Currency);
                    """),
                ("Claimwick.Spending/WebApi/Expenses/SubmitExpense/MoneyModel.cs", """
                    namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

                    internal sealed record MoneyModel(decimal Amount);
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2026 accepts identical records in one use case folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</proves>
    [Fact(DisplayName = "LS2026 accepts identical records in one use case folder")]
    public Task AcceptsIdenticalRecordsInOneUseCaseFolder() =>
        Verify.AnalyzerAsync<DuplicateModelAnalyzer>(
            [
                ("Claimwick.Spending/Application/Expenses/SubmitExpense/MoneyInput.cs", """
                    namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

                    internal sealed record MoneyInput(decimal Amount, string Currency);
                    """),
                ("Claimwick.Spending/WebApi/Expenses/SubmitExpense/MoneyInput.cs", """
                    namespace Claimwick.Spending.WebApi.Expenses.SubmitExpense;

                    internal sealed record MoneyInput(decimal Amount, string Currency);
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
