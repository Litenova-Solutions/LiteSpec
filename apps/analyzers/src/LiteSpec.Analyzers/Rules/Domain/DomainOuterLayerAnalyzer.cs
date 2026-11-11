using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1001: Domain references no outer-layer namespace and no Application, Infrastructure or WebApi type.</summary>
/// <follows>litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainOuterLayerAnalyzer : LayerAnalyzer
{
    // The user-facing copy of this list lives in the Detection Limits section of docs/analyzers/README.md.
    /// <summary>The outer layer namespaces text held for this operation.</summary>
    private static readonly string[] OuterLayerNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Marten",
        "Npgsql",
        "Dapper",
        "Microsoft.Data",
        "System.Data",
        "Microsoft.AspNetCore",
        "System.Net.Http",
        "Microsoft.Extensions",
        "MediatR",
        "LiteBus",
        "Wolverine",
        "MassTransit",
        "Serilog",
        "NLog",
        "Newtonsoft.Json",
        "System.Text.Json",
        "System.Runtime.Serialization",
        "System.Xml.Serialization",
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1001);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the domain outer layer analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
        context.RegisterSyntaxNodeAction(c => AnalyzeUsing(c, scope), SyntaxKind.UsingDirective);
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

        if (!References.TryResolve(context, node, type => IsOuter(type, map, context.Compilation), out var name, out var type)
            || type is null)
        {
            return;
        }

        if (IsOuterLayerNamespace(type.ContainingNamespace, out var namespaceName))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1001, node.Identifier.GetLocation(), name, namespaceName));
            return;
        }

        var layer = map.ReferencedLayer(type, context.Compilation);
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1001, node.Identifier.GetLocation(), name, "the " + layer + " layer"));
    }

    /// <summary>Analyzes the using.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeUsing(SyntaxNodeAnalysisContext context, FileScope scope)
    {
        var directive = (UsingDirectiveSyntax)context.Node;
        if (directive.Name is null || !scope.AppliesTo(directive.SyntaxTree))
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(directive.Name, context.CancellationToken).Symbol;
        var containing = symbol switch
        {
            INamespaceSymbol namespaceSymbol => namespaceSymbol,
            INamedTypeSymbol type => type.ContainingNamespace,
            _ => null,
        };
        if (!IsOuterLayerNamespace(containing, out var namespaceName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1001, directive.Name.GetLocation(), directive.Name.ToString(), namespaceName));
    }

    /// <summary>Returns true when the type is an outer-layer concern for Domain; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the type is an outer-layer concern for Domain; otherwise false.</returns>
    // A value type in a module Contracts folder is a typed identifier or shared value the module
    // publishes, and an exception there is a rule refusal the module publishes; Domain may name both.
    // A port or an integration event there stays outer.
    private static bool IsOuter(INamedTypeSymbol type, CodeMap map, Compilation compilation) =>
        IsOuterLayerNamespace(type.ContainingNamespace, out _)
        || (map.ReferencedLayer(type, compilation) is ArchitectureLayer.Application or ArchitectureLayer.Infrastructure or ArchitectureLayer.WebApi
            && !((type.IsValueType || IsException(type)) && CodeMap.IsContracts(CodeMap.SourceFileOf(type, compilation))));

    /// <summary>Whether the type derives from <see cref="System.Exception" />.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when an exception type is among its base types; otherwise false.</returns>
    private static bool IsException(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Exception")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the containing is outer layer namespace; otherwise false.</summary>
    /// <param name="containing">The containing supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="namespaceName">The namespace name the check resolved.</param>
    /// <returns>True when the containing is outer layer namespace; otherwise false.</returns>
    private static bool IsOuterLayerNamespace(INamespaceSymbol? containing, out string namespaceName)
    {
        if (containing is null || containing.IsGlobalNamespace)
        {
            namespaceName = string.Empty;
            return false;
        }

        namespaceName = containing.ToDisplayString();
        foreach (var prefix in OuterLayerNamespaces)
        {
            if (namespaceName.StartsWith(prefix, StringComparison.Ordinal)
                && (namespaceName.Length == prefix.Length || namespaceName[prefix.Length] == '.'))
            {
                return true;
            }
        }

        return false;
    }
}
