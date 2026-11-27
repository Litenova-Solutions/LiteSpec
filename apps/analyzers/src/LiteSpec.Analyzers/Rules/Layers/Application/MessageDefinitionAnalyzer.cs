using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2023: a class implements exactly one LiteBus message definition contract, never two concerns.</summary>
/// <follows>litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessageDefinitionAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2023);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the message definition analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => AnalyzeType(c, scope), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        var definitions = new List<INamedTypeSymbol>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.OriginalDefinition.Name == "IMessageDefinition"
                && contract.OriginalDefinition.Arity == 2
                && seen.Add(contract))
            {
                definitions.Add(contract);
            }
        }

        if (definitions.Count < 2)
        {
            return;
        }

        definitions.Sort(static (left, right) => string.Compare(
            left.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            right.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            StringComparison.Ordinal));
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2023, type.Locations[0], type.Name, definitions.Count, SplitOf(definitions)));
    }

    /// <summary>Returns the split the message names: the two per-concern class names, or the generic fix.</summary>
    /// <param name="definitions">The distinct message definition contracts, in ordinal order.</param>
    /// <returns>The fix clause of the diagnostic message.</returns>
    private static string SplitOf(List<INamedTypeSymbol> definitions)
    {
        var first = ConcernOf(definitions[0]);
        var second = ConcernOf(definitions[1]);

        if (first is null || second is null)
        {
            return "give each pipeline concern its own class";
        }

        return "split it into '" + first + "' and '" + second + "'";
    }

    /// <summary>Returns the per-concern class name of one contract, or null when it cannot be read.</summary>
    /// <param name="contract">The constructed message definition contract.</param>
    /// <returns>The class name, or null when the contract names no message and declaration.</returns>
    private static string? ConcernOf(INamedTypeSymbol contract)
    {
        if (contract.TypeArguments.Length != 2
            || contract.TypeArguments[0] is not INamedTypeSymbol message
            || contract.TypeArguments[1] is not INamedTypeSymbol declaration)
        {
            return null;
        }

        var concern = declaration.Name;
        const string suffix = "Declaration";
        if (concern.EndsWith(suffix, StringComparison.Ordinal) && concern.Length > suffix.Length)
        {
            concern = concern.Substring(0, concern.Length - suffix.Length);
        }

        return message.Name + concern + "Definition";
    }
}
