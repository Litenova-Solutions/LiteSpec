using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5014: a class, struct, record or interface tagged as a development shortcut is compiled into a
///     build that does not define DEBUG, or its tag states no reason. The tag is read from the
///     <c>///</c> comment text, so the rule works whether or not the project generates a documentation file.
/// </summary>
/// <follows>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DevelopmentShortcutAnalyzer : LayerAnalyzer
{
    /// <summary>The compilation symbol every Debug build defines.</summary>
    private const string DebugSymbol = "DEBUG";

    /// <summary>Matches one development-shortcut tag, reading its reason, or none for a self-closing tag.</summary>
    private static readonly Regex ShortcutTag = new(
        @"<development-shortcut\b[^>]*?(?:/>|>(?<reason>[\s\S]*?)</development-shortcut\s*>)",
        RegexOptions.Compiled);

    /// <summary>Matches the comment marker at the start of each documentation line.</summary>
    private static readonly Regex DocumentationMarker = new(
        @"^[ \t]*///",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Matches runs of whitespace inside a reason, so a reason wrapped over lines reads as one line.</summary>
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5014);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None;

    /// <summary>Registers the analysis actions for the development shortcut rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => Analyze(c, scope), SymbolKind.NamedType);
    }

    /// <summary>Reports a tagged type whose declarations compile without DEBUG, or whose tag states no reason.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(SymbolAnalysisContext context, FileScope scope)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.IsImplicitlyDeclared || type.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
        {
            return;
        }

        if (!scope.AppliesTo(type))
        {
            return;
        }

        var tagged = false;
        var withoutDebug = false;
        var withoutReason = false;

        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(context.CancellationToken) is not TypeDeclarationSyntax part)
            {
                continue;
            }

            foreach (var reason in ReasonsOf(part))
            {
                tagged = true;
                withoutDebug |= !DefinesDebug(part.SyntaxTree);
                withoutReason |= reason.Length == 0;
            }
        }

        if (!tagged)
        {
            return;
        }

        // One diagnostic per type: a shortcut that reaches a release build matters more than a missing reason.
        if (withoutDebug)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS5014, type.Locations[0], type.Name, Descriptors.DevelopmentShortcutWithoutDebug));
        }
        else if (withoutReason)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS5014, type.Locations[0], type.Name, Descriptors.DevelopmentShortcutWithoutReason));
        }
    }

    /// <summary>Reads the reason of each development-shortcut tag on one declaration, empty for a tag with none.</summary>
    /// <param name="part">One declaration of the type.</param>
    /// <returns>The reason of each tag, in source order.</returns>
    private static List<string> ReasonsOf(TypeDeclarationSyntax part)
    {
        var documentation = new StringBuilder();

        foreach (var trivia in part.GetLeadingTrivia())
        {
            if (IsDocumentation(trivia))
            {
                documentation.Append(trivia.ToFullString()).Append('\n');
            }
        }

        var text = DocumentationMarker.Replace(documentation.ToString(), string.Empty);
        var reasons = new List<string>();

        foreach (Match match in ShortcutTag.Matches(text))
        {
            var reason = match.Groups["reason"];
            reasons.Add(reason.Success ? Collapse(reason.Value) : string.Empty);
        }

        return reasons;
    }

    /// <summary>Returns true when the trivia is an XML documentation comment, parsed or as plain text.</summary>
    /// <param name="trivia">The trivia under inspection.</param>
    /// <returns>True when the trivia is a documentation comment; otherwise false.</returns>
    private static bool IsDocumentation(SyntaxTrivia trivia)
    {
        if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
        {
            return true;
        }

        // Without a documentation file the compiler parses "///" as an ordinary single-line comment.
        if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
        {
            return false;
        }

        var text = trivia.ToString();
        return text.StartsWith("///", StringComparison.Ordinal) && !text.StartsWith("////", StringComparison.Ordinal);
    }

    /// <summary>Returns the reason with its whitespace collapsed to single spaces and its ends trimmed.</summary>
    /// <param name="reason">The reason text as written.</param>
    /// <returns>The collapsed reason, empty when nothing but whitespace was written.</returns>
    private static string Collapse(string reason) => Whitespace.Replace(reason, " ").Trim();

    /// <summary>Returns true when the tree's parse options define DEBUG, which the compiler uses for the build's #if blocks.</summary>
    /// <param name="tree">The syntax tree of one declaration.</param>
    /// <returns>True when the tree defines DEBUG; otherwise false.</returns>
    private static bool DefinesDebug(SyntaxTree tree) =>
        tree.Options is CSharpParseOptions options
        && options.PreprocessorSymbolNames.Any(symbol => symbol == DebugSymbol);
}
