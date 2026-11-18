using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2027, infrastructure work inside Application.</summary>
public sealed class InfrastructureWorkAnalyzerTests
{
    /// <summary>LS2027 reports an archive type in an Application file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/architecture#produce-file-formats-in-infrastructure</proves>
    [Fact(DisplayName = "LS2027 reports an archive type in an Application file")]
    public Task ReportsAnArchiveTypeInAnApplicationFile() =>
        Verify.AnalyzerAsync<InfrastructureWorkAnalyzer>(
            [
                ("Claimwick.Export/UseCases/ExportWorkspace/Application/ExportWorkspaceQueryHandler.cs", """
                    using System.IO.Compression;

                    namespace Claimwick.Export.UseCases.ExportWorkspace;

                    internal sealed class ExportWorkspaceQueryHandler
                    {
                        internal void Handle({|LS2027:ZipArchive|} archive)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2027 reports a JSON writer in an Application file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/architecture#produce-file-formats-in-infrastructure</proves>
    [Fact(DisplayName = "LS2027 reports a JSON writer in an Application file")]
    public Task ReportsAJsonWriterInAnApplicationFile() =>
        Verify.AnalyzerAsync<InfrastructureWorkAnalyzer>(
            [
                ("Claimwick.Export/UseCases/ExportWorkspace/Application/ExportWorkspaceQueryHandler.cs", """
                    using System.Text.Json;

                    namespace Claimwick.Export.UseCases.ExportWorkspace;

                    internal sealed class ExportWorkspaceQueryHandler
                    {
                        internal void Handle({|LS2027:Utf8JsonWriter|} writer)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2027 stays silent for the same use in an Infrastructure file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/architecture#produce-file-formats-in-infrastructure</proves>
    [Fact(DisplayName = "LS2027 stays silent for the same use in an Infrastructure file")]
    public Task StaysSilentInAnInfrastructureFile() =>
        Verify.AnalyzerAsync<InfrastructureWorkAnalyzer>(
            [
                ("Claimwick.Export/UseCases/ExportWorkspace/Infrastructure/WorkspaceArchiveWriter.cs", """
                    using System.IO.Compression;

                    namespace Claimwick.Export.UseCases.ExportWorkspace;

                    internal sealed class WorkspaceArchiveWriter
                    {
                        internal void Write(ZipArchive archive)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");

    /// <summary>LS2027 stays silent when a namespace only shares a prefix.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/architecture#produce-file-formats-in-infrastructure</proves>
    [Fact(DisplayName = "LS2027 stays silent when a namespace only shares a prefix")]
    public Task StaysSilentWhenANamespaceOnlySharesAPrefix() =>
        Verify.AnalyzerAsync<InfrastructureWorkAnalyzer>(
            [
                ("Claimwick.Export/UseCases/ExportWorkspace/Application/TableReader.cs", """
                    namespace CsvReader.Utils;

                    public sealed class TableReader
                    {
                    }
                    """),
                ("Claimwick.Export/UseCases/ExportWorkspace/Application/ExportWorkspaceQueryHandler.cs", """
                    using CsvReader.Utils;

                    namespace Claimwick.Export.UseCases.ExportWorkspace;

                    internal sealed class ExportWorkspaceQueryHandler
                    {
                        internal void Handle(TableReader reader)
                        {
                        }
                    }
                    """),
            ],
            assemblyName: "Claimwick.Export");
}
