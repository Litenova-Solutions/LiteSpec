using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2029: a handler, reaction or job names another module's public query or query result, only a reaction or a job names its public command or command result, and a Platform file names no module identifier. LS2030: a Contracts project declares no interface.</summary>
/// <follows>litespec/packs/dotnet/architecture#follow-layer-direction-in-every-file</follows>
/// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModuleContractsAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2029, Descriptors.LS2030);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the module contracts analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var map = CodeMap.For(context.Compilation, context.Options);
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var assemblyName = context.Compilation.AssemblyName ?? string.Empty;
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map, assemblyName), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
        context.RegisterSymbolAction(c => AnalyzeType(c, scope, assemblyName), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope, CodeMap map, string assemblyName)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree))
        {
            return;
        }

        if (CodeMap.IsPlatformFile(node.SyntaxTree.FilePath, assemblyName))
        {
            AnalyzePlatformName(context, node, map);
            return;
        }

        if (map.ModuleOf(node.SyntaxTree.FilePath) is not string module)
        {
            return;
        }

        if (!References.TryResolve(context, node, type => IsPublicQueryOrCommandOf(type, context.Compilation), out var name, out var type)
            || type is null
            || CodeMap.SourceFileOf(type, context.Compilation) is not string file
            || map.ModuleOf(file) is not string other
            || other.Equals(module, StringComparison.Ordinal)
            || MayName(type, context))
        {
            return;
        }

        var provider = CodeMap.FolderName(other);
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2029,
            node.Identifier.GetLocation(),
            Where(scope, node, module),
            name,
            ReasonFor(type, provider)));
    }

    /// <summary>Returns true when the type containing the name may name the referenced contract type: a handler, reaction or job names a public query or query result, and only a reaction or a job names a public command or command result; otherwise false.</summary>
    /// <param name="type">The referenced contract type.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <returns>True when the holder's role may name the referenced type; otherwise false.</returns>
    private static bool MayName(INamedTypeSymbol type, SyntaxNodeAnalysisContext context)
    {
        if (ContainingType(context) is not INamedTypeSymbol holder)
        {
            return false;
        }

        if (IsCommandSide(type.Name))
        {
            return IsReactionOrJob(holder);
        }

        return IsHandler(holder) || IsReactionOrJob(holder);
    }

    /// <summary>Returns why the holder may not name the referenced contract type.</summary>
    /// <param name="type">The referenced contract type.</param>
    /// <param name="provider">The display name of the provider module.</param>
    /// <returns>The reason clause naming the provider module and the roles that may name the type.</returns>
    private static string ReasonFor(INamedTypeSymbol type, string provider)
    {
        if (IsCommandSide(type.Name))
        {
            return "which is a public command or command result of module '" + provider + "'; only a reaction or a job may name it";
        }

        return "which is a public query or query result of module '" + provider + "'; only a handler, a reaction, or a job may name it";
    }

    /// <summary>Returns true when the contract type sits on the command side: a public command, a command result, or a command result part; otherwise false.</summary>
    /// <param name="typeName">The simple name of the referenced contract type.</param>
    /// <returns>True when the name carries the command role; otherwise false.</returns>
    private static bool IsCommandSide(string typeName) =>
        typeName.Contains("Command", StringComparison.Ordinal);

    /// <summary>Returns the type containing the name, or null when no type contains it.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <returns>The containing type, or null.</returns>
    private static INamedTypeSymbol? ContainingType(SyntaxNodeAnalysisContext context)
    {
        for (var symbol = context.ContainingSymbol; symbol is not null; symbol = symbol.ContainingSymbol)
        {
            if (symbol is INamedTypeSymbol type)
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>Returns true when the type is a command or query handler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a command or query handler; otherwise false.</returns>
    private static bool IsHandler(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal)
            || type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name is "ICommandHandler" or "IQueryHandler")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is a reaction or a job: a type named *Job or *Worker, a type deriving from BackgroundService, or a type implementing IEventHandler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is a reaction or a job; otherwise false.</returns>
    private static bool IsReactionOrJob(INamedTypeSymbol type)
    {
        if (type.Name.EndsWith("Job", StringComparison.Ordinal)
            || type.Name.EndsWith("Worker", StringComparison.Ordinal))
        {
            return true;
        }

        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "BackgroundService")
            {
                return true;
            }
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IEventHandler")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Analyzes the name in a Platform file.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="node">The name syntax under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzePlatformName(SyntaxNodeAnalysisContext context, SimpleNameSyntax node, CodeMap map)
    {
        if (!References.TryResolve(context, node, type => IsModuleIdentifier(type, map, context.Compilation), out var name, out _))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2029,
            node.Identifier.GetLocation(),
            "Platform",
            name,
            "which is a module identifier; the Platform names no business identifier"));
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, FileScope scope, string assemblyName)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Interface)
        {
            return;
        }

        if (CodeMap.SourceFileOf(type, context.Compilation) is not string file)
        {
            return;
        }

        if (!CodeMap.IsContracts(file) && !CodeMap.IsContractsAssembly(assemblyName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2030, type.Locations[0], type.Name));
    }

    /// <summary>Returns where the diagnostic sits: the file's layer, or its module when no layer folder holds the file.</summary>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="node">The name syntax under inspection.</param>
    /// <param name="module">The module folder holding the file under inspection.</param>
    /// <returns>The layer name, or the module name when the file takes no layer.</returns>
    private static string Where(FileScope scope, SimpleNameSyntax node, string module)
    {
        var layer = scope.LayerOf(node.SyntaxTree);
        return layer == ArchitectureLayer.None ? "Module '" + CodeMap.FolderName(module) + "'" : layer.ToString();
    }

    /// <summary>Returns true when the type is a public query, public command or result declared in a Contracts project; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when a Contracts use case folder declares the type.</returns>
    private static bool IsPublicQueryOrCommandOf(INamedTypeSymbol type, Compilation compilation) =>
        CodeMap.SourceFileOf(type, compilation) is string file && Contracts.IsPublicQueryOrCommand(file, type.Name);

    /// <summary>Returns true when the type is a module identifier: a type ending in Id declared in a module; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when a module file declares the identifier.</returns>
    private static bool IsModuleIdentifier(INamedTypeSymbol type, CodeMap map, Compilation compilation) =>
        type.Name.EndsWith("Id", StringComparison.Ordinal)
        && CodeMap.SourceFileOf(type, compilation) is string file
        && map.ModuleOf(file) is not null;
}
