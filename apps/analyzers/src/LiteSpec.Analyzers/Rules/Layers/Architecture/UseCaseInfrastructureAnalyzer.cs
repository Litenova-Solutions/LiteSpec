using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2025: an Infrastructure adapter names no Domain type outside identifiers and shared values, and no WebApi type.</summary>
/// <follows>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseCaseInfrastructureAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2025);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Infrastructure;

    /// <summary>
    ///     Registers the analysis actions for the use case infrastructure analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope, CodeMap map)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree) || !CodeMap.IsAdapter(node.SyntaxTree.FilePath))
        {
            return;
        }

        if (!References.TryResolve(context, node, type => ViolationOf(type, map, context.Compilation) is not null, out var name, out var type)
            || type is null
            || ViolationOf(type, map, context.Compilation) is not string reason)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2025, node.Identifier.GetLocation(), name, reason));
    }

    /// <summary>Returns why the adapter may not name the type, or null when it may.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The reason clause, or null when the layer may name the type.</returns>
    private static string? ViolationOf(INamedTypeSymbol type, CodeMap map, Compilation compilation)
    {
        // The adapter implements its port, so a Ports type stays allowed beside Application and library types.
        var target = map.ReferencedLayer(type, compilation);
        if (target == ArchitectureLayer.WebApi)
        {
            return "which lives in the WebApi layer";
        }

        if (target == ArchitectureLayer.Domain && !IsIdentifier(type) && !IsSharedValue(type, map, compilation))
        {
            return "which is a Domain type outside typed identifiers and shared-kernel values";
        }

        return null;
    }

    /// <summary>Returns true when the type is a typed identifier; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a typed identifier.</returns>
    private static bool IsIdentifier(INamedTypeSymbol type) =>
        type.Name.EndsWith("Id", StringComparison.Ordinal);

    /// <summary>Returns true when the type is a shared-kernel value; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the type lives in the shared kernel.</returns>
    private static bool IsSharedValue(INamedTypeSymbol type, CodeMap map, Compilation compilation) =>
        (CodeMap.SourceFileOf(type, compilation) is string file && map.ModuleOf(file) is null)
        || IsKernelAssembly(type.ContainingAssembly?.Name);

    /// <summary>Returns true when the assembly is the business kernel; otherwise false.</summary>
    /// <param name="assemblyName">The assembly name, or null when the compilation does not provide it.</param>
    /// <returns>True when the assembly is the kernel.</returns>
    private static bool IsKernelAssembly(string? assemblyName) =>
        !string.IsNullOrEmpty(assemblyName)
        && (assemblyName!.Equals("Kernel", StringComparison.Ordinal)
            || assemblyName.EndsWith(".Kernel", StringComparison.Ordinal)
            || assemblyName.Equals("SharedKernel", StringComparison.Ordinal)
            || assemblyName.EndsWith(".SharedKernel", StringComparison.Ordinal));
}
