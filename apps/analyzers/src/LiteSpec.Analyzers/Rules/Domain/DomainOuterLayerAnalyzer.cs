using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1001: Domain references no outer-layer namespace, only the base class library and Domain types.</summary>
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
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the domain outer layer analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeName, SyntaxKind.IdentifierName, SyntaxKind.GenericName);
        context.RegisterSyntaxNodeAction(AnalyzeUsing, SyntaxKind.UsingDirective);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context)
    {
        var node = (SimpleNameSyntax)context.Node;
        // An implicit "var" names no type the author wrote; the expression it stands for is reported instead.
        if (node is IdentifierNameSyntax { IsVar: true } || IsCoveredByEnclosingNode(node, context))
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol;
        var (name, containing) = NameAndNamespace(symbol);
        if (name is null || !IsOuterLayerNamespace(containing, out var namespaceName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1001, node.Identifier.GetLocation(), name, namespaceName));
    }

    /// <summary>Analyzes the using.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeUsing(SyntaxNodeAnalysisContext context)
    {
        var directive = (UsingDirectiveSyntax)context.Node;
        if (directive.Name is null)
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

    /// <summary>Returns true when the node is covered by enclosing node; otherwise false.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <returns>True when the node is covered by enclosing node; otherwise false.</returns>
    private static bool IsCoveredByEnclosingNode(SimpleNameSyntax node, SyntaxNodeAnalysisContext context)
    {
        var child = (SyntaxNode)node;
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is UsingDirectiveSyntax || parent is DocumentationCommentTriviaSyntax)
            {
                return true;
            }

            if (parent is MemberAccessExpressionSyntax access
                && ReferenceEquals(access.Expression, child)
                && IsOuterLayerSymbol(context.SemanticModel.GetSymbolInfo(access.Name, context.CancellationToken).Symbol))
            {
                return true;
            }

            child = parent;
        }

        return false;
    }

    /// <summary>Returns true when the symbol is outer layer symbol; otherwise false.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>True when the symbol is outer layer symbol; otherwise false.</returns>
    private static bool IsOuterLayerSymbol(ISymbol? symbol)
    {
        var (_, containing) = NameAndNamespace(symbol);
        return IsOuterLayerNamespace(containing, out _);
    }

    /// <summary>Returns the simple name and containing namespace of the symbol.</summary>
    /// <param name="symbol">The symbol whose name and namespace the check reads, or null when it has none.</param>
    /// <returns>The name and namespace pair, with both parts null when the symbol is of no named kind.</returns>
    private static (string? Name, INamespaceSymbol? Namespace) NameAndNamespace(ISymbol? symbol)
    {
        switch (symbol)
        {
            case INamedTypeSymbol type:
                return (type.Name, type.ContainingNamespace);
            case IMethodSymbol { MethodKind: MethodKind.Constructor } constructor:
                return (constructor.ContainingType?.Name, constructor.ContainingType?.ContainingNamespace);
            case IMethodSymbol method:
                return (method.Name, method.ContainingType?.ContainingNamespace);
            case IPropertySymbol property:
                return (property.Name, property.ContainingType?.ContainingNamespace);
            case IFieldSymbol field:
                return (field.Name, field.ContainingType?.ContainingNamespace);
            case IEventSymbol @event:
                return (@event.Name, @event.ContainingType?.ContainingNamespace);
            default:
                return (null, null);
        }
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
