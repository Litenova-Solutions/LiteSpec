using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// Writes the runtime values the records state into the consumer's code: the mark attributes
/// for the attribute style, an event's Classification, a rule exception's Code, the assembly
/// registry, one constant per permission, and one entry per role. The project file and the
/// record pages arrive as additional files; with no project file, or settings that fail to
/// parse, the generator stays silent, and in an opted-out project (layer <c>None</c>) it writes
/// no source and reports no diagnostic. Every pipeline model holds strings and equatable arrays
/// only, so an edit to an unrelated file reruns nothing the edit cannot change.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</follows>
/// <follows>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</follows>
/// <follows>litespec/core/access#state-the-scope-of-every-permission</follows>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
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

        var declaredLayer = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => HostModel.DeclaredLayer(options));
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName ?? string.Empty);
        var xunit = context.CompilationProvider
            .Select(static (compilation, _) =>
                compilation.GetTypeByMetadataName(XunitTraitAttributeName) is not null);
        var host = declaredLayer.Combine(assemblyName).Combine(xunit)
            .Select(static (combined, _) => HostModel.Create(
                combined.Left.Left, combined.Left.Right, combined.Right));

        var input = records.Combine(marked.Collect()).Combine(host);
        context.RegisterSourceOutput(input, static (production, combined) => Emitter.Execute(
            production, combined.Left.Left, combined.Left.Right, combined.Right));
    }
}
