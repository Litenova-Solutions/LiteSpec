using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2028, public types outside a module Contracts folder.</summary>
public sealed class ModuleVisibilityAnalyzerTests
{
    /// <summary>LS2028 reports a public class under a use case folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 reports a public class under a use case folder")]
    public Task ReportsAPublicClassUnderAUseCaseFolder() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    public sealed class {|LS2028:SubmitExpenseCommand|};
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2028 accepts a public type in Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 accepts a public type in Contracts")]
    public Task AcceptsAPublicTypeInContracts() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Spending/Contracts/ExpenseId.cs", """
                    namespace Claimwick.Spending.Contracts;

                    public readonly record struct ExpenseId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2028 accepts the public module entry type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 accepts the public module entry type")]
    public Task AcceptsThePublicModuleEntryType() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Spending/SpendingModule.cs", """
                    namespace Claimwick.Spending;

                    public sealed class SpendingModule
                    {
                    }
                    """),
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    internal sealed class SubmitExpenseCommand;
                    """),
            ],
            assemblyName: "Claimwick.Spending",
            layer: "Application");

    /// <summary>LS2028 accepts an internal type outside Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 accepts an internal type outside Contracts")]
    public Task AcceptsAnInternalTypeOutsideContracts() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    internal sealed class SubmitExpenseCommand;
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2028 stays silent in a one-modules-project compilation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 stays silent in a one-modules-project compilation")]
    public Task StaysSilentInAOneModulesProjectCompilation() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Modules/Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    public sealed class SubmitExpenseCommand;
                    """),
            ],
            assemblyName: "Claimwick.Modules");
}
