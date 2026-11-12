using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2019: a use case or reaction folder names no type of another such folder, except the message and result of a use case it runs.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-use-case-folders-isolated</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SliceIsolationAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2019);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the slice isolation analyzer rule with the compilation
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

        if (map.SliceOf(node.SyntaxTree.FilePath) is not string slice)
        {
            return;
        }

        if (!References.TryResolve(context, node, type => OtherSliceOf(type, slice, map, context.Compilation) is not null, out var name, out var type)
            || type is null
            || OtherSliceOf(type, slice, map, context.Compilation) is not string other)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2019, node.Identifier.GetLocation(), name, CodeMap.FolderName(other)));
    }

    /// <summary>Returns the other slice folder declaring the type, or null when it declares none.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="slice">The slice folder holding the file under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The other slice folder, or null when the type is declared in no other slice.</returns>
    private static string? OtherSliceOf(INamedTypeSymbol type, string slice, CodeMap map, Compilation compilation)
    {
        if (IsRunnableMessage(type) || CodeMap.SourceFileOf(type, compilation) is not string file)
        {
            return null;
        }

        if (map.SliceOf(file) is string other && !other.Equals(slice, StringComparison.Ordinal))
        {
            return other;
        }

        return null;
    }

    /// <summary>Returns true when the type is a use case message or result, or nested in one, which a slice that runs the use case may name.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the outermost type ends in Command, Query, CommandResult or QueryResult; otherwise false.</returns>
    private static bool IsRunnableMessage(INamedTypeSymbol type)
    {
        var outer = type;
        while (outer.ContainingType is not null)
        {
            outer = outer.ContainingType;
        }

        var name = outer.Name;
        return name.EndsWith("Command", StringComparison.Ordinal)
            || name.EndsWith("Query", StringComparison.Ordinal)
            || name.EndsWith("CommandResult", StringComparison.Ordinal)
            || name.EndsWith("QueryResult", StringComparison.Ordinal);
    }
}
