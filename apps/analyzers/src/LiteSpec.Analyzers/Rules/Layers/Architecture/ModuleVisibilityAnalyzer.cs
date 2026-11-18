using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2028: in one project per module only the Contracts folder and the module entry type are public.</summary>
/// <follows>litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModuleVisibilityAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2028);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the module visibility analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        if (!map.IsModuleProject)
        {
            return;
        }

        var entry = EntryFileOf(context.Compilation.AssemblyName ?? string.Empty);
        context.RegisterSymbolAction(c => AnalyzeType(c, scope, entry), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="entry">The module entry file name, such as BooksModule.cs.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, FileScope scope, string entry)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.ContainingType is not null || !IsEffectivelyPublic(type))
        {
            return;
        }

        if (FirstSourceFile(type) is not string file)
        {
            return;
        }

        if (CodeMap.IsContracts(file) || FileNameOf(file).Equals(entry, StringComparison.Ordinal))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2028, type.Locations[0], type.Name, EntryTypeOf(entry)));
    }

    /// <summary>Returns the module entry file name of the assembly: its last segment with the Module suffix.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>The entry file name, such as BooksModule.cs.</returns>
    private static string EntryFileOf(string assemblyName)
    {
        var dot = assemblyName.LastIndexOf('.');
        var module = dot < 0 ? assemblyName : assemblyName.Substring(dot + 1);
        return module + "Module.cs";
    }

    /// <summary>Returns the module entry type name of the entry file.</summary>
    /// <param name="entry">The module entry file name, such as BooksModule.cs.</param>
    /// <returns>The entry type name, such as BooksModule.</returns>
    private static string EntryTypeOf(string entry) =>
        entry.EndsWith(".cs", StringComparison.Ordinal) ? entry.Substring(0, entry.Length - 3) : entry;

    /// <summary>Returns true when the type and every containing type are public; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is effectively public.</returns>
    private static bool IsEffectivelyPublic(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns the first source file declaring the type, or null for metadata.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The source file path, or null when the type comes from metadata.</returns>
    private static string? FirstSourceFile(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (!string.IsNullOrEmpty(reference.SyntaxTree.FilePath))
            {
                return reference.SyntaxTree.FilePath;
            }
        }

        return null;
    }

    /// <summary>Returns the file name the path ends with.</summary>
    /// <param name="filePath">The file path as the host reports it.</param>
    /// <returns>The file name.</returns>
    private static string FileNameOf(string filePath)
    {
        var slash = Math.Max(filePath.LastIndexOf('/'), filePath.LastIndexOf('\\'));
        return slash < 0 ? filePath : filePath.Substring(slash + 1);
    }
}
