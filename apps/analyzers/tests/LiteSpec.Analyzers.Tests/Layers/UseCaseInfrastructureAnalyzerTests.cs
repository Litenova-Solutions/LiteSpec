using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2025, layer direction inside a use case Infrastructure folder.</summary>
public sealed class UseCaseInfrastructureAnalyzerTests
{
    private const string Stubs = """
        namespace Claimwick.Export.Domain;

        public abstract class AggregateRoot<TId>;

        public readonly record struct ExpenseId(System.Guid Value);
        """;

    private const string Root = """
        using Claimwick.Export.Domain;

        namespace Claimwick.Export.Domain.Expense;

        public sealed class Expense : AggregateRoot<ExpenseId>
        {
        }
        """;

    /// <summary>LS2025 reports a use case adapter naming a Domain type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 reports a use case adapter naming a Domain type")]
    public Task ReportsAUseCaseAdapterNamingADomainType() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Claimwick.Export/Domain/Expense/Expense.cs", Root),
                ("Claimwick.Export/Infrastructure/ExportWorkspace/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.Domain.Expense;

                    namespace Claimwick.Export.Infrastructure.ExportWorkspace;

                    internal sealed class WorkspaceArchiveWriter
                    {
                        internal void Write({|LS2025:Expense|} expense)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 reports a use case adapter naming a WebApi type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 reports a use case adapter naming a WebApi type")]
    public Task ReportsAUseCaseAdapterNamingAWebApiType() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Claimwick.Export/WebApi/ExportWorkspace/ExportWorkspaceEndpoint.cs", """
                    namespace Claimwick.Export.WebApi.ExportWorkspace;

                    internal sealed class ExportWorkspaceEndpoint
                    {
                    }
                    """),
                ("Claimwick.Export/Infrastructure/ExportWorkspace/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.WebApi.ExportWorkspace;

                    namespace Claimwick.Export.Infrastructure.ExportWorkspace;

                    internal sealed class WorkspaceArchiveWriter
                    {
                        internal void Describe({|LS2025:ExportWorkspaceEndpoint|} endpoint)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 accepts a use case adapter naming identifiers and shared values.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 accepts a use case adapter naming identifiers and shared values")]
    public Task AcceptsIdentifiersAndSharedValues() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Shared/Domain/Money.cs", """
                    namespace Claimwick.Shared;

                    public sealed class Money
                    {
                    }
                    """),
                ("Claimwick.Export/Infrastructure/ExportWorkspace/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.Domain;
                    using Claimwick.Shared;

                    namespace Claimwick.Export.Infrastructure.ExportWorkspace;

                    internal sealed class WorkspaceArchiveWriter
                    {
                        internal void Write(ExpenseId id, Money total)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 stays silent in a module Infrastructure folder.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 stays silent in a module Infrastructure folder")]
    public Task StaysSilentInAModuleInfrastructureFolder() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Claimwick.Export/Domain/Expense/Expense.cs", Root),
                ("Claimwick.Export/Infrastructure/Persistence/ExpenseRepository.cs", """
                    using Claimwick.Export.Domain.Expense;

                    namespace Claimwick.Export.Infrastructure.Persistence;

                    internal sealed class ExpenseRepository
                    {
                        internal void Store(Expense expense)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");
}
