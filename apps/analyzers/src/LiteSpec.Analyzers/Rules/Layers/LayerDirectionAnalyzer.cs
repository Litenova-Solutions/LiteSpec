using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2018: an Application or WebApi file names no type outside its layer.</summary>
/// <follows>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LayerDirectionAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2018);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.WebApi;

    /// <summary>
    ///     Registers the analysis actions for the layer direction analyzer rule with the compilation
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
        if (!scope.AppliesTo(node.SyntaxTree))
        {
            return;
        }

        var source = scope.LayerOf(node.SyntaxTree);
        if (!References.TryResolve(context, node, type => ViolationOf(type, source, map, context.Compilation) is not null, out var name, out var type)
            || type is null
            || ViolationOf(type, source, map, context.Compilation) is not string reason)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2018, node.Identifier.GetLocation(), source, name, reason));
    }

    /// <summary>Returns why the layer may not name the type, or null when it may.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="source">The architecture layer the file belongs to.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The reason clause, or null when the layer may name the type.</returns>
    private static string? ViolationOf(INamedTypeSymbol type, ArchitectureLayer source, CodeMap map, Compilation compilation)
    {
        if (source == ArchitectureLayer.Application)
        {
            var layer = map.ReferencedLayer(type, compilation);
            if (layer == ArchitectureLayer.Infrastructure || layer == ArchitectureLayer.WebApi)
            {
                return "which lives in the " + layer + " layer";
            }

            if (type.Name.Equals("HttpContext", StringComparison.Ordinal))
            {
                return "which is an HttpContext";
            }

            if (IsAspNetCore(type.ContainingNamespace))
            {
                return "which is an ASP.NET Core type";
            }

            return null;
        }

        if (type.Name.EndsWith("Repository", StringComparison.Ordinal))
        {
            return "which is a repository";
        }

        var target = map.ReferencedLayer(type, compilation);
        if (target == ArchitectureLayer.Infrastructure)
        {
            return "which lives in the Infrastructure layer";
        }

        if (target == ArchitectureLayer.Domain && !IsIdentifier(type) && !IsSharedKernel(type, map, compilation))
        {
            return "which is a Domain type outside typed identifiers and shared-kernel values";
        }

        return null;
    }

    /// <summary>Returns true when the type is a typed identifier; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a typed identifier; otherwise false.</returns>
    private static bool IsIdentifier(INamedTypeSymbol type) =>
        type.Name.EndsWith("Id", StringComparison.Ordinal);

    /// <summary>Returns true when the type lives in the shared kernel; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the type lives in the shared kernel; otherwise false.</returns>
    private static bool IsSharedKernel(INamedTypeSymbol type, CodeMap map, Compilation compilation) =>
        CodeMap.SourceFileOf(type, compilation) is string file && map.ModuleOf(file) is null;

    /// <summary>Returns true when the namespace is an ASP.NET Core namespace; otherwise false.</summary>
    /// <param name="containing">The containing namespace, or null when the compilation does not provide it.</param>
    /// <returns>True when the namespace is an ASP.NET Core namespace; otherwise false.</returns>
    private static bool IsAspNetCore(INamespaceSymbol? containing)
    {
        if (containing is null || containing.IsGlobalNamespace)
        {
            return false;
        }

        const string prefix = "Microsoft.AspNetCore";
        var name = containing.ToDisplayString();
        return name.StartsWith(prefix, StringComparison.Ordinal)
            && (name.Length == prefix.Length || name[prefix.Length] == '.');
    }
}
