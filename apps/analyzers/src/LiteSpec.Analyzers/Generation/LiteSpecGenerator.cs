using LiteSpec.Analyzers.Generation.Sources;
using LiteSpec.Analyzers.LayerMap;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// Writes the runtime values the records state into the consumer's code: the mark attributes
/// for the attribute style, an event's Classification, a rule exception's Code, the assembly
/// registry, one constant per permission, and one entry per role. The project file and the
/// record files arrive as additional files; with no project file, or settings that fail to
/// parse, the generator stays silent, as it does in the Kernel, which generates
/// nothing. Without an Implementation claim it also writes no
/// source and reports no diagnostic, whatever the records state. Every pipeline model holds
/// strings and equatable arrays only, so an edit to an unrelated file reruns nothing the edit
/// cannot change.
/// </summary>
/// <follows>litespec/packs/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/packs/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</follows>
/// <follows>litespec/core/access#scope-every-role-to-its-one-reach</follows>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
/// <follows>litespec/core/scope#claim-the-record-layers-the-project-uses</follows>
[Generator]
public sealed class LiteSpecGenerator : IIncrementalGenerator
{
    /// <summary>The tracked step name of the record-reading stage.</summary>
    internal const string RecordsStepName = "LiteSpecRecords";

    /// <summary>The xUnit v3 trait attribute, detected by type name in the compilation.</summary>
    internal const string XunitTraitAttributeName = "Xunit.v3.ITraitAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var records = context.AdditionalTextsProvider
            .Collect()
            .Select(static (texts, cancellationToken) => RecordModel.Read(texts, cancellationToken))
            .WithTrackingName(RecordsStepName);

        var marked = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => MarkedType.IsCandidate(node),
                transform: static (syntax, _) => MarkedType.Read(syntax))
            .Where(static type => type is not null)
            .Select(static (type, _) => type!);

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName ?? string.Empty);
        var xunit = context.CompilationProvider
            .Select(static (compilation, _) =>
                compilation.GetTypeByMetadataName(XunitTraitAttributeName) is not null);
        var host = assemblyName.Combine(xunit)
            .Select(static (combined, _) => HostModel.Create(combined.Left, combined.Right));

        var claims = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => ClaimGate.Declared(options));

        var input = records.Combine(marked.Collect()).Combine(host).Combine(claims);
        context.RegisterSourceOutput(input, static (production, combined) =>
        {
            if (!ClaimGate.RunsTraceability(combined.Right))
            {
                return;
            }

            GeneratedSourceWriter.Execute(production, combined.Left.Left.Left, combined.Left.Left.Right, combined.Left.Right);
        });
    }
}
