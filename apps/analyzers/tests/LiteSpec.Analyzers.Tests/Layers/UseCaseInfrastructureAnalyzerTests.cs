using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2025, layer direction inside a use case Infrastructure folder.</summary>
public sealed class UseCaseInfrastructureAnalyzerTests
{
    private const string Stubs = """
        namespace Claimwick.Export.Domain;

        public abstract class AggregateRoot<TId>;

        public readonly record struct WorkspaceId(System.Guid Value);
        """;

    private const string Root = """
        using Claimwick.Export.Domain;

        namespace Claimwick.Export.Domain.Workspaces;

        public sealed class Workspace : AggregateRoot<WorkspaceId>
        {
        }
        """;

    /// <summary>LS2025 reports an adapter naming a Domain type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 reports an adapter naming a Domain type")]
    public Task ReportsAnAdapterNamingADomainType() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Claimwick.Export/Domain/Workspaces/Workspace.cs", Root),
                ("Claimwick.Export/Infrastructure/WorkspaceArchive/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.Domain.Workspaces;

                    namespace Claimwick.Export.Infrastructure.WorkspaceArchive;

                    internal sealed class WorkspaceArchiveWriter
                    {
                        internal void Write({|LS2025:Workspace|} workspace)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 reports an adapter naming a Domain type no port signature names.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 reports an adapter naming a Domain type no port signature names")]
    public Task ReportsAnAdapterNamingADomainTypeNoPortSignatureNames() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Claimwick.Export/Domain/Workspaces/Workspace.cs", Root),
                ("Claimwick.Export/Ports/WorkspaceArchive/IWorkspaceArchive.cs", """
                    namespace Claimwick.Export.Ports.WorkspaceArchive;

                    internal interface IWorkspaceArchive
                    {
                        void Write(string path);
                    }
                    """),
                ("Claimwick.Export/Infrastructure/WorkspaceArchive/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.Domain.Workspaces;
                    using Claimwick.Export.Ports.WorkspaceArchive;

                    namespace Claimwick.Export.Infrastructure.WorkspaceArchive;

                    internal sealed class WorkspaceArchiveWriter : IWorkspaceArchive
                    {
                        public void Write(string path)
                        {
                        }

                        internal void Rebuild({|LS2025:Workspace|} workspace)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 reports an adapter naming a WebApi type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 reports an adapter naming a WebApi type")]
    public Task ReportsAnAdapterNamingAWebApiType() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Claimwick.Export/WebApi/Workspaces/ExportWorkspace/ExportWorkspaceEndpoint.cs", """
                    namespace Claimwick.Export.WebApi.Workspaces.ExportWorkspace;

                    internal sealed class ExportWorkspaceEndpoint
                    {
                    }
                    """),
                ("Claimwick.Export/Infrastructure/WorkspaceArchive/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.WebApi.Workspaces.ExportWorkspace;

                    namespace Claimwick.Export.Infrastructure.WorkspaceArchive;

                    internal sealed class WorkspaceArchiveWriter
                    {
                        internal void Describe({|LS2025:ExportWorkspaceEndpoint|} endpoint)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 accepts an adapter naming identifiers and shared values.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 accepts an adapter naming identifiers and shared values")]
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
                ("Claimwick.Export/Infrastructure/WorkspaceArchive/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.Domain;
                    using Claimwick.Shared;

                    namespace Claimwick.Export.Infrastructure.WorkspaceArchive;

                    internal sealed class WorkspaceArchiveWriter
                    {
                        internal void Write(WorkspaceId id, Money total)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 accepts an adapter naming its port.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 accepts an adapter naming its port")]
    public Task AcceptsAnAdapterNamingItsPort() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Ports/WorkspaceArchive/IWorkspaceArchive.cs", """
                    namespace Claimwick.Export.Ports.WorkspaceArchive;

                    internal interface IWorkspaceArchive
                    {
                    }
                    """),
                ("Claimwick.Export/Infrastructure/WorkspaceArchive/WorkspaceArchiveWriter.cs", """
                    using Claimwick.Export.Ports.WorkspaceArchive;

                    namespace Claimwick.Export.Infrastructure.WorkspaceArchive;

                    internal sealed class WorkspaceArchiveWriter : IWorkspaceArchive
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2025 accepts an adapter naming a Domain type its port returns.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2025 accepts an adapter naming a Domain type its port returns")]
    public Task AcceptsAnAdapterNamingADomainTypeItsPortReturns() =>
        Verify.AnalyzerAsync<UseCaseInfrastructureAnalyzer>(
            [
                ("Claimwick.Export/Domain/Stubs.cs", Stubs),
                ("Claimwick.Export/Domain/Workspaces/Workspace.cs", Root),
                ("Claimwick.Export/Ports/WorkspaceArchive/IWorkspaceArchive.cs", """
                    using Claimwick.Export.Domain;
                    using Claimwick.Export.Domain.Workspaces;

                    namespace Claimwick.Export.Ports.WorkspaceArchive;

                    internal interface IWorkspaceArchive
                    {
                        Workspace Open(WorkspaceId id);
                    }
                    """),
                ("Claimwick.Export/Infrastructure/WorkspaceArchive/WorkspaceArchiveReader.cs", """
                    using Claimwick.Export.Domain;
                    using Claimwick.Export.Domain.Workspaces;
                    using Claimwick.Export.Ports.WorkspaceArchive;

                    namespace Claimwick.Export.Infrastructure.WorkspaceArchive;

                    internal sealed class WorkspaceArchiveReader : IWorkspaceArchive
                    {
                        public Workspace Open(WorkspaceId id)
                        {
                            throw new System.NotImplementedException();
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
                ("Claimwick.Export/Domain/Workspaces/Workspace.cs", Root),
                ("Claimwick.Export/Infrastructure/Persistence/WorkspaceRepository.cs", """
                    using Claimwick.Export.Domain.Workspaces;

                    namespace Claimwick.Export.Infrastructure.Persistence;

                    internal sealed class WorkspaceRepository
                    {
                        internal void Store(Workspace workspace)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");
}
