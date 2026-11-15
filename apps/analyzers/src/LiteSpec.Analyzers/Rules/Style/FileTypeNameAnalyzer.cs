using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Style;

/// <summary>LS3003: a C# file holds one primary top-level type and takes its name.</summary>
/// <follows>litespec/backend/dotnet/naming#match-c-files-and-primary-types</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileTypeNameAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS3003);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>Registers the analysis actions for the file type name analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSyntaxTreeAction(tree => Analyze(tree, scope));
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(SyntaxTreeAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Tree))
        {
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(context.Tree.FilePath);
        if (fileName.Length == 0)
        {
            return;
        }

        var expected = fileName;
        var dot = fileName.IndexOf('.');
        if (dot >= 0)
        {
            expected = fileName.Substring(0, dot);
        }

        var found = new List<(string Name, int Arity, Location Location)>();
        var root = context.Tree.GetRoot(context.CancellationToken);
        foreach (var node in root.DescendantNodes())
        {
            if (IsFileScoped(node))
            {
                continue;
            }

            var identifier = default(SyntaxToken);
            var arity = 0;
            if (node is BaseTypeDeclarationSyntax typeDeclaration && IsTopLevel(node))
            {
                identifier = typeDeclaration.Identifier;
                arity = typeDeclaration is TypeDeclarationSyntax parameterized
                    ? Count(parameterized.TypeParameterList)
                    : 0;
            }
            else if (node is DelegateDeclarationSyntax delegateDeclaration && IsTopLevel(node))
            {
                identifier = delegateDeclaration.Identifier;
                arity = Count(delegateDeclaration.TypeParameterList);
            }
            else
            {
                continue;
            }

            if (!identifier.IsMissing && !AlreadySeen(found, identifier.ValueText, arity))
            {
                found.Add((identifier.ValueText, arity, identifier.GetLocation()));
            }
        }

        if (found.Count == 0)
        {
            return;
        }

        var display = Path.GetFileName(context.Tree.FilePath);
        if (found.Count == 1)
        {
            if (!found[0].Name.Equals(expected, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS3003, found[0].Location, found[0].Name, display));
            }

            return;
        }

        if (!found[0].Name.Equals(expected, StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS3003, found[0].Location, found[0].Name, display));
        }

        for (var i = 1; i < found.Count; i++)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS3003, found[i].Location, found[i].Name, display));
        }
    }

    /// <summary>Returns true when the node is top level; otherwise false.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <returns>True when the node is top level; otherwise false.</returns>
    private static bool IsTopLevel(SyntaxNode node) =>
        node.Parent is CompilationUnitSyntax || node.Parent is BaseNamespaceDeclarationSyntax;

    /// <summary>Returns true when the declaration is file-scoped and stays with its owner; otherwise false.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <returns>True when the node declares a file-scoped type or delegate; otherwise false.</returns>
    private static bool IsFileScoped(SyntaxNode node)
    {
        SyntaxTokenList modifiers;

        if (node is BaseTypeDeclarationSyntax type)
        {
            modifiers = type.Modifiers;
        }
        else if (node is DelegateDeclarationSyntax @delegate)
        {
            modifiers = @delegate.Modifiers;
        }
        else
        {
            return false;
        }

        foreach (var modifier in modifiers)
        {
            if (modifier.IsKind(SyntaxKind.FileKeyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Counts the int.</summary>
    /// <param name="parameters">The parameters supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>The int.</returns>
    private static int Count(TypeParameterListSyntax? parameters) =>
        parameters is null ? 0 : parameters.Parameters.Count;

    /// <summary>Returns true when the already-seen condition holds; otherwise false.</summary>
    /// <param name="found">The grants collected so far, by use-case identifier.</param>
    /// <param name="name">The name to convert or match.</param>
    /// <param name="arity">The generic arity of the type name.</param>
    /// <returns>True when the already-seen condition holds; otherwise false.</returns>
    private static bool AlreadySeen(List<(string Name, int Arity, Location Location)> found, string name, int arity)
    {
        foreach (var seen in found)
        {
            if (seen.Arity == arity && seen.Name == name)
            {
                return true;
            }
        }

        return false;
    }
}
