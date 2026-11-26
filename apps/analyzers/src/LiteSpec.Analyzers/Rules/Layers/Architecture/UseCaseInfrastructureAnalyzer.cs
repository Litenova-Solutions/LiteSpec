using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2025: an Infrastructure adapter names no Domain type outside identifiers, shared values and the types its ports name, and no WebApi type.</summary>
/// <follows>litespec/packs/dotnet/architecture#follow-layer-direction-in-every-file</follows>
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
        var ports = new ConcurrentDictionary<string, HashSet<INamedTypeSymbol>>(StringComparer.Ordinal);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map, ports), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="ports">The port signature types by adapter file, computed once per file.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope, CodeMap map, ConcurrentDictionary<string, HashSet<INamedTypeSymbol>> ports)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree) || !CodeMap.IsAdapter(node.SyntaxTree.FilePath))
        {
            return;
        }

        var allowed = ports.GetOrAdd(
            node.SyntaxTree.FilePath ?? string.Empty,
            _ => PortSignatureTypes(context.Compilation, node.SyntaxTree.FilePath));

        if (!References.TryResolve(context, node, type => ViolationOf(type, map, context.Compilation, allowed) is not null, out var name, out var type)
            || type is null
            || ViolationOf(type, map, context.Compilation, allowed) is not string reason)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2025, node.Identifier.GetLocation(), name, reason));
    }

    /// <summary>Returns why the adapter may not name the type, or null when it may.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="allowed">The port signature types the adapter may name, by original definition.</param>
    /// <returns>The reason clause, or null when the layer may name the type.</returns>
    private static string? ViolationOf(INamedTypeSymbol type, CodeMap map, Compilation compilation, HashSet<INamedTypeSymbol> allowed)
    {
        // The adapter implements its port, so a Ports type stays allowed beside Application and library types.
        var target = map.ReferencedLayer(type, compilation);
        if (target == ArchitectureLayer.WebApi)
        {
            return "which lives in the WebApi layer";
        }

        if (target == ArchitectureLayer.Domain
            && !IsIdentifier(type)
            && !IsSharedValue(type, map, compilation)
            && !allowed.Contains(type.OriginalDefinition))
        {
            return "which is a Domain type outside typed identifiers, shared-kernel values and the signatures of the ports the adapter implements";
        }

        return null;
    }

    /// <summary>Returns the types the adapter may name through its ports: every type the interfaces of the ports it implements name in their member signatures, and every type reachable from those through member signatures.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The named types by original definition.</returns>
    private static HashSet<INamedTypeSymbol> PortSignatureTypes(Compilation compilation, string? filePath)
    {
        var ports = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        CollectImplementedPorts(compilation, filePath, ports);
        CollectFolderPort(compilation, filePath, ports);

        return SignatureReach.Of(ports, compilation);
    }

    /// <summary>Collects the port interfaces the types declared in the adapter folder implement.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="ports">The port interfaces collected so far, by original definition.</param>
    private static void CollectImplementedPorts(Compilation compilation, string? filePath, HashSet<INamedTypeSymbol> ports)
    {
        var folder = FolderOf(filePath);
        if (folder is null)
        {
            return;
        }

        // Every source type of the compilation declared in the adapter folder, found through symbols: an analyzer may not ask for semantic models.
        foreach (var symbol in compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type))
        {
            if (symbol is not INamedTypeSymbol type || !DeclaredInFolder(type, folder))
            {
                continue;
            }

            foreach (var implemented in type.AllInterfaces)
            {
                if (IsPortInterface(implemented, compilation))
                {
                    ports.Add(implemented.OriginalDefinition);
                }
            }
        }
    }

    /// <summary>Returns true when a source location of the type sits directly in the folder; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="folder">The folder as <see cref="FolderOf"/> reports it.</param>
    /// <returns>True when the type is declared in the folder; otherwise false.</returns>
    private static bool DeclaredInFolder(INamedTypeSymbol type, string folder)
    {
        foreach (var location in type.Locations)
        {
            if (location.IsInSource && string.Equals(FolderOf(location.SourceTree?.FilePath), folder, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Collects the interface of the port folder the adapter folder names, so an adapter file that declares no implementing type keeps the same allowance.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="ports">The port interfaces collected so far, by original definition.</param>
    private static void CollectFolderPort(Compilation compilation, string? filePath, HashSet<INamedTypeSymbol> ports)
    {
        var folder = FolderOf(filePath);
        if (folder is null || folder.Length == 0)
        {
            return;
        }

        var slash = folder.LastIndexOf('/');
        var name = slash < 0 ? folder : folder.Substring(slash + 1);
        var marker = "/Ports/" + name + "/";

        // The port folder Ports/{Name}/ holds the interface I{Name}; find it by name, then confirm its folder.
        foreach (var symbol in compilation.GetSymbolsWithName("I" + name, SymbolFilter.Type))
        {
            if (symbol is not INamedTypeSymbol { TypeKind: TypeKind.Interface } face)
            {
                continue;
            }

            foreach (var location in face.Locations)
            {
                if (location.IsInSource
                    && (location.SourceTree?.FilePath ?? string.Empty).Replace('\\', '/').Contains(marker, StringComparison.Ordinal))
                {
                    ports.Add(face.OriginalDefinition);
                    break;
                }
            }
        }
    }

    /// <summary>Returns true when the interface is a module port: a type the compilation declares in a Ports folder; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the interface is declared in a Ports folder; otherwise false.</returns>
    private static bool IsPortInterface(INamedTypeSymbol type, Compilation compilation)
    {
        if (type.TypeKind != TypeKind.Interface)
        {
            return false;
        }

        if (CodeMap.SourceFileOf(type, compilation) is not string file)
        {
            return false;
        }

        // A module port lives in the module Ports folder and a cross-module port in Contracts Ports.
        foreach (var segment in file.Split('/', '\\'))
        {
            if (segment.Equals("Ports", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the folder holding the file, as a forward-slash directory path.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The folder, or null when the path is null or empty.</returns>
    private static string? FolderOf(string? filePath)
    {
        if (filePath is null || filePath.Length == 0)
        {
            return null;
        }

        var unified = filePath.Replace('\\', '/');
        var slash = unified.LastIndexOf('/');
        return slash < 0 ? string.Empty : unified.Substring(0, slash);
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
    /// <returns>True when the type lives in the Kernel.</returns>
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
            || assemblyName.Equals("Kernel", StringComparison.Ordinal)
            || assemblyName.EndsWith(".Kernel", StringComparison.Ordinal));
}
