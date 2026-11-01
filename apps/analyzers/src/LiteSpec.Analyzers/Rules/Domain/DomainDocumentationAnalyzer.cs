using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1011: every public Domain type and member carries XML documentation.</summary>
/// <follows>litespec/backend/dotnet/domain#document-every-public-domain-contract</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainDocumentationAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1011);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the domain documentation analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSyntaxNodeAction(
            Analyze,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.DelegateDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.EventDeclaration,
            SyntaxKind.EventFieldDeclaration,
            SyntaxKind.FieldDeclaration,
            SyntaxKind.ConstructorDeclaration);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        switch (context.Node)
        {
            case TypeDeclarationSyntax type:
                CheckSymbol(type.Identifier, context.SemanticModel.GetDeclaredSymbol(type), type, context);
                break;
            case EnumDeclarationSyntax @enum:
                CheckSymbol(@enum.Identifier, context.SemanticModel.GetDeclaredSymbol(@enum), @enum, context);
                break;
            case DelegateDeclarationSyntax @delegate:
                CheckSymbol(@delegate.Identifier, context.SemanticModel.GetDeclaredSymbol(@delegate), @delegate, context);
                break;
            case MethodDeclarationSyntax method:
                CheckSymbol(method.Identifier, context.SemanticModel.GetDeclaredSymbol(method), method, context);
                break;
            case PropertyDeclarationSyntax property:
                CheckSymbol(property.Identifier, context.SemanticModel.GetDeclaredSymbol(property), property, context);
                break;
            case EventDeclarationSyntax @event:
                CheckSymbol(@event.Identifier, context.SemanticModel.GetDeclaredSymbol(@event), @event, context);
                break;
            case EventFieldDeclarationSyntax eventField:
                foreach (var variable in eventField.Declaration.Variables)
                {
                    CheckSymbol(variable.Identifier, context.SemanticModel.GetDeclaredSymbol(variable), eventField, context);
                }

                break;
            case FieldDeclarationSyntax field:
                foreach (var variable in field.Declaration.Variables)
                {
                    CheckSymbol(variable.Identifier, context.SemanticModel.GetDeclaredSymbol(variable), field, context);
                }

                break;
            case ConstructorDeclarationSyntax constructor:
                CheckSymbol(constructor.Identifier, context.SemanticModel.GetDeclaredSymbol(constructor), constructor, context);
                break;
        }
    }

    /// <summary>Checks the symbol.</summary>
    /// <param name="identifier">The identifier text to resolve.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="documentation">The documentation text under inspection.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void CheckSymbol(SyntaxToken identifier, ISymbol? symbol, SyntaxNode documentation, SyntaxNodeAnalysisContext context)
    {
        if (identifier.IsMissing || symbol is null || !RequiresDocumentation(symbol))
        {
            return;
        }

        if (!HasDocumentation(documentation, identifier))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1011, identifier.GetLocation(), identifier.ValueText));
        }
    }

    /// <summary>Returns true when the requires-documentation condition holds; otherwise false.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>True when the requires-documentation condition holds; otherwise false.</returns>
    private static bool RequiresDocumentation(ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol type)
        {
            return IsPubliclyVisible(type);
        }

        if (symbol.ContainingType is null || !IsPubliclyVisible(symbol.ContainingType))
        {
            return false;
        }

        if (symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
        {
            return false;
        }

        switch (symbol)
        {
            case IMethodSymbol method:
                return !method.IsOverride
                    && method.ExplicitInterfaceImplementations.Length == 0
                    && method.MethodKind is MethodKind.Ordinary or MethodKind.Constructor;
            case IPropertySymbol property:
                return !property.IsOverride && property.ExplicitInterfaceImplementations.Length == 0;
            case IEventSymbol @event:
                return !@event.IsOverride && @event.ExplicitInterfaceImplementations.Length == 0;
            case IFieldSymbol:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Returns true when the type is publicly visible; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is publicly visible; otherwise false.</returns>
    private static bool IsPubliclyVisible(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns true when the node has documentation; otherwise false.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <param name="identifier">The identifier text to resolve.</param>
    /// <returns>True when the node has documentation; otherwise false.</returns>
    private static bool HasDocumentation(SyntaxNode node, SyntaxToken identifier)
    {
        // Without a documentation file the compiler parses "///" as ordinary comments, so those
        // lines are gathered and read as text; with one, the structured comment is read.
        StringBuilder? plain = null;
        foreach (var trivia in node.DescendantTrivia(descendIntoTrivia: false))
        {
            if (trivia.Span.Start >= identifier.Span.Start)
            {
                break;
            }

            if ((trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                && Documents(trivia))
            {
                return true;
            }

            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                var text = trivia.ToString();
                if (text.StartsWith("///", StringComparison.Ordinal))
                {
                    (plain ??= new StringBuilder()).Append(text, 3, text.Length - 3).Append('\n');
                }
            }
        }

        return plain is not null && DocumentsPlain(plain.ToString());
    }

    /// <summary>Stores the filled summary used by the containing code.</summary>
    private static readonly Regex FilledSummary = new(@"<summary>\s*\S[\s\S]*?</summary>", RegexOptions.Compiled);

    /// <summary>Returns true when the documents-plain condition holds; otherwise false.</summary>
    /// <param name="text">The source text the edit applies to.</param>
    /// <returns>True when the documents-plain condition holds; otherwise false.</returns>
    private static bool DocumentsPlain(string text) =>
        text.IndexOf("<inheritdoc", StringComparison.Ordinal) >= 0 || FilledSummary.IsMatch(text);

    /// <summary>Returns true when the documents condition holds; otherwise false.</summary>
    /// <param name="trivia">The trivia under inspection.</param>
    /// <returns>True when the documents condition holds; otherwise false.</returns>
    private static bool Documents(SyntaxTrivia trivia)
    {
        if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax comment)
        {
            return false;
        }

        foreach (var content in comment.Content)
        {
            switch (content)
            {
                case XmlElementSyntax element when IsName(element.StartTag.Name, "summary"):
                    if (!string.IsNullOrWhiteSpace(element.Content.ToFullString()))
                    {
                        return true;
                    }

                    break;
                case XmlElementSyntax element when IsName(element.StartTag.Name, "inheritdoc"):
                    return true;
                case XmlEmptyElementSyntax empty when IsName(empty.Name, "inheritdoc"):
                    return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the name is name; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <param name="expected">The value the record states.</param>
    /// <returns>True when the name is name; otherwise false.</returns>
    private static bool IsName(XmlNameSyntax name, string expected) =>
        name.LocalName.ValueText.Equals(expected, StringComparison.Ordinal);
}
