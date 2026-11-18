using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2028, public types outside a module Contracts project.</summary>
public sealed class ModuleVisibilityAnalyzerTests
{
    /// <summary>LS2028 reports a public class under a use case folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 reports a public class under a use case folder")]
    public Task ReportsAPublicClassUnderAUseCaseFolder() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

                    public sealed class {|LS2028:SubmitExpenseCommand|};
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2028 stays quiet in a sibling Contracts project compilation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 stays quiet in a sibling Contracts project compilation")]
    public Task StaysQuietInASiblingContractsProjectCompilation() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Identifiers/ExpenseId.cs", """
                    namespace Claimwick.Spending.Contracts.Identifiers;

                    public readonly record struct ExpenseId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Spending.Contracts");

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
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommand;
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2028 accepts an internal type outside Contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</proves>
    [Fact(DisplayName = "LS2028 accepts an internal type outside Contracts")]
    public Task AcceptsAnInternalTypeOutsideContracts() =>
        Verify.AnalyzerAsync<ModuleVisibilityAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

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
                ("Claimwick.Modules/Spending/Application/SubmitExpense/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.Application.SubmitExpense;

                    public sealed class SubmitExpenseCommand;
                    """),
            ],
            assemblyName: "Claimwick.Modules");
}
