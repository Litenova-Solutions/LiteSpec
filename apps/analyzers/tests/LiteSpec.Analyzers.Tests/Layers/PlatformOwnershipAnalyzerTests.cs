using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2024, platform-owned capability code inside a module.</summary>
public sealed class PlatformOwnershipAnalyzerTests
{
    private const string Stubs = """
        namespace PlatformStubs;

        public sealed class StoreOptions;

        public abstract class DeclarationGuard<TMessage, TValue>;

        public abstract class JsonConverter;

        public interface IDocumentSession;
        """;

    /// <summary>LS2024 reports store setup inside a module.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions</proves>
    [Fact(DisplayName = "LS2024 reports store setup inside a module")]
    public Task ReportsStoreSetupInsideAModule() =>
        Verify.AnalyzerAsync<PlatformOwnershipAnalyzer>(
            [
                ("Claimwick.Spending/Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Infrastructure/Persistence/SpendingStoreSetup.cs", """
                    using PlatformStubs;

                    namespace Claimwick.Spending.Infrastructure.Persistence;

                    internal static class SpendingStoreSetup
                    {
                        internal static {|LS2024:StoreOptions|} Options()
                        {
                            return new {|LS2024:StoreOptions|}();
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2024 reports a guard inside a module.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions</proves>
    [Fact(DisplayName = "LS2024 reports a guard inside a module")]
    public Task ReportsAGuardInsideAModule() =>
        Verify.AnalyzerAsync<PlatformOwnershipAnalyzer>(
            [
                ("Claimwick.Spending/Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommand.cs", """
                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    public sealed class SubmitExpenseCommand;
                    """),
                ("Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseGuard.cs", """
                    using PlatformStubs;

                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    internal sealed class {|LS2024:SubmitExpenseGuard|} : DeclarationGuard<SubmitExpenseCommand, int>
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2024 reports a converter inside a module.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions</proves>
    [Fact(DisplayName = "LS2024 reports a converter inside a module")]
    public Task ReportsAConverterInsideAModule() =>
        Verify.AnalyzerAsync<PlatformOwnershipAnalyzer>(
            [
                ("Claimwick.Spending/Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/UseCases/SubmitExpense/WebApi/MoneyConverter.cs", """
                    using PlatformStubs;

                    namespace Claimwick.Spending.UseCases.SubmitExpense;

                    internal sealed class {|LS2024:MoneyConverter|} : JsonConverter
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2024 accepts store setup in the platform.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions</proves>
    [Fact(DisplayName = "LS2024 accepts store setup in the platform")]
    public Task AcceptsStoreSetupInThePlatform() =>
        Verify.AnalyzerAsync<PlatformOwnershipAnalyzer>(
            [
                ("Claimwick.Platform/Persistence/Stubs.cs", Stubs),
                ("Claimwick.Platform/Persistence/PlatformStoreSetup.cs", """
                    using PlatformStubs;

                    namespace Claimwick.Platform.Persistence;

                    internal static class PlatformStoreSetup
                    {
                        internal static StoreOptions Options()
                        {
                            return new StoreOptions();
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Platform");

    /// <summary>LS2024 accepts store use in a module repository.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions</proves>
    [Fact(DisplayName = "LS2024 accepts store use in a module repository")]
    public Task AcceptsStoreUseInAModuleRepository() =>
        Verify.AnalyzerAsync<PlatformOwnershipAnalyzer>(
            [
                ("Claimwick.Spending/Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Infrastructure/Persistence/ExpenseRepository.cs", """
                    using PlatformStubs;

                    namespace Claimwick.Spending.Infrastructure.Persistence;

                    internal sealed class ExpenseRepository
                    {
                        private readonly IDocumentSession _session;

                        public ExpenseRepository(IDocumentSession session)
                        {
                            _session = session;
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
