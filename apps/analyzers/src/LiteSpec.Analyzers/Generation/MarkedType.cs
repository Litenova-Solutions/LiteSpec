using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// One marked type in the compilation, projected to strings: its namespace, nesting and name for
/// the generated part, its kind and marks for the values it takes, and whether it can take a
/// generated part at all. Kinds come from the Roslyn kind reader and marks from the Roslyn mark
/// reader; no symbol or syntax node crosses the pipeline.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class MarkedType : IEquatable<MarkedType>
{
    /// <summary>Initializes a new instance of the <see cref="MarkedType"/> class.Creates a marked type.</summary>
    /// <param name="fullName">The dotted name with generic arity, the registry key.</param>
    /// <param name="ns">The escaped dotted namespace, or empty in the global namespace.</param>
    /// <param name="nesting">Each enclosing type outside in, as an escaped partial declaration.</param>
    /// <param name="name">The escaped simple name.</param>
    /// <param name="typeParameters">The escaped type parameter list, or empty.</param>
    /// <param name="keyword">The declaration keyword: class, record, record struct, struct or interface.</param>
    /// <param name="kind">The code kind, or null when the type is of no known kind.</param>
    /// <param name="markIds">The record identifiers in source order, without duplicates.</param>
    /// <param name="useCaseMarkCount">The use-case marks in source order, counting duplicates.</param>
    /// <param name="messageFullName">The fully qualified first type argument of the handler interface, or empty.</param>
    /// <param name="isPartial">Whether the type and every enclosing type carry partial in every declaration.</param>
    /// <param name="hasClassification">Whether a Classification member is declared in source.</param>
    /// <param name="hasCode">Whether a Code member is declared in source.</param>
    /// <param name="baseHasVirtualCode">Whether a base type declares a virtual string Code.</param>
    public MarkedType(
        string fullName,
        string ns,
        ImmutableArray<string> nesting,
        string name,
        string typeParameters,
        string keyword,
        CodeKind? kind,
        ImmutableArray<string> markIds,
        int useCaseMarkCount,
        string messageFullName,
        bool isPartial,
        bool hasClassification,
        bool hasCode,
        bool baseHasVirtualCode)
    {
        FullName = fullName;
        Namespace = ns;
        Nesting = nesting;
        Name = name;
        TypeParameters = typeParameters;
        Keyword = keyword;
        Kind = kind;
        MarkIds = markIds;
        UseCaseMarkCount = useCaseMarkCount;
        MessageFullName = messageFullName;
        IsPartial = isPartial;
        HasClassification = hasClassification;
        HasCode = hasCode;
        BaseHasVirtualCode = baseHasVirtualCode;
    }

    /// <summary>Gets the dotted name with generic arity, the registry key.</summary>
    public string FullName { get; }

    /// <summary>Gets the escaped dotted namespace, or empty in the global namespace.</summary>
    public string Namespace { get; }

    /// <summary>Gets each enclosing type outside in, as an escaped partial declaration.</summary>
    public ImmutableArray<string> Nesting { get; }

    /// <summary>Gets the escaped simple name.</summary>
    public string Name { get; }

    /// <summary>Gets the escaped type parameter list, or empty.</summary>
    public string TypeParameters { get; }

    /// <summary>Gets the declaration keyword: class, record, record struct, struct or interface.</summary>
    public string Keyword { get; }

    /// <summary>Gets the code kind, or null when the type is of no known kind.</summary>
    public CodeKind? Kind { get; }

    /// <summary>Gets the record identifiers in source order, without duplicates.</summary>
    public ImmutableArray<string> MarkIds { get; }

    /// <summary>Gets the use-case marks in source order, counting duplicates.</summary>
    public int UseCaseMarkCount { get; }

    /// <summary>Gets the fully qualified first type argument of the handler interface, or empty.</summary>
    public string MessageFullName { get; }

    /// <summary>Gets a value indicating whether whether the type and every enclosing type carry partial in every declaration.</summary>
    public bool IsPartial { get; }

    /// <summary>Gets a value indicating whether whether a Classification member is declared in source.</summary>
    public bool HasClassification { get; }

    /// <summary>Gets a value indicating whether whether a Code member is declared in source.</summary>
    public bool HasCode { get; }

    /// <summary>Gets a value indicating whether whether a base type declares a virtual string Code.</summary>
    public bool BaseHasVirtualCode { get; }

    /// <summary>
    /// Whether a syntax node may carry a mark: a type declaration with an attribute list or a
    /// mark tag in its leading trivia. The check stays textual, with no symbol lookup.
    /// </summary>
    /// <param name="node">The syntax node.</param>
    /// <returns>Whether the node may carry a mark.</returns>
    public static bool IsCandidate(SyntaxNode node) =>
        node is TypeDeclarationSyntax candidate
        && (candidate.AttributeLists.Count > 0 || HasMarkComment(candidate));

    /// <summary>
    /// Reads one candidate declaration into its marked type, or null when the type carries no
    /// mark. Marks and partial modifiers merge across every declaration of the type in file and
    /// position order, so the first mark in source order wins wherever one is read.
    /// </summary>
    /// <param name="context">The syntax context.</param>
    /// <returns>The marked type, or null when it carries no mark.</returns>
    public static MarkedType? Read(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var parts = new List<TypeDeclarationSyntax>();

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is TypeDeclarationSyntax part)
            {
                parts.Add(part);
            }
        }

        if (parts.Count == 0)
        {
            return null;
        }

        parts.Sort(static (left, right) =>
        {
            var files = string.Compare(
                left.SyntaxTree.FilePath, right.SyntaxTree.FilePath, StringComparison.Ordinal);

            return files != 0 ? files : left.SpanStart.CompareTo(right.SpanStart);
        });

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ids = new List<string>();
        var useCaseMarks = 0;

        foreach (var part in parts)
        {
            foreach (var mark in MarkReader.Read(part))
            {
                if (mark.Verb == MarkVerb.Follows)
                {
                    continue;
                }

                if (mark.RecordId.StartsWith("use-case/", StringComparison.Ordinal))
                {
                    useCaseMarks++;
                }

                if (mark.RecordId.Length > 0 && seen.Add(mark.RecordId))
                {
                    ids.Add(mark.RecordId);
                }
            }
        }

        if (ids.Count == 0)
        {
            return null;
        }

        var isPartial = PartialTypes.IsPartial(symbol);

        for (var outer = symbol.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            if (!PartialTypes.IsPartial(outer))
            {
                isPartial = false;
                break;
            }
        }

        return new MarkedType(
            FullNameOf(symbol),
            NamespaceOf(symbol),
            NestingOf(symbol),
            Escape(symbol.Name),
            TypeParametersOf(symbol),
            KeywordOf(symbol),
            KindReader.KindOf(symbol, KindContext.Empty, symbol.ContainingAssembly.Name),
            ImmutableArray.CreateRange(ids),
            useCaseMarks,
            MessageFullNameOf(symbol),
            isPartial,
            symbol.GetMembers("Classification").Length > 0,
            symbol.GetMembers("Code").Length > 0,
            BaseDeclaresVirtualCode(symbol));
    }

    /// <inheritdoc />
    public bool Equals(MarkedType? other) =>
        other is not null
        && string.Equals(FullName, other.FullName, StringComparison.Ordinal)
        && string.Equals(Namespace, other.Namespace, StringComparison.Ordinal)
        && Nesting.SequenceEqual(other.Nesting)
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(TypeParameters, other.TypeParameters, StringComparison.Ordinal)
        && string.Equals(Keyword, other.Keyword, StringComparison.Ordinal)
        && Kind == other.Kind
        && MarkIds.SequenceEqual(other.MarkIds)
        && UseCaseMarkCount == other.UseCaseMarkCount
        && string.Equals(MessageFullName, other.MessageFullName, StringComparison.Ordinal)
        && IsPartial == other.IsPartial
        && HasClassification == other.HasClassification
        && HasCode == other.HasCode
        && BaseHasVirtualCode == other.BaseHasVirtualCode;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MarkedType other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(FullName);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Namespace);

            foreach (var enclosing in Nesting)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(enclosing);
            }

            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(TypeParameters);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Keyword);
            hash = hash * 31 + (Kind?.GetHashCode() ?? 0);

            foreach (var id in MarkIds)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(id);
            }

            hash = hash * 31 + UseCaseMarkCount.GetHashCode();
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(MessageFullName);
            hash = hash * 31 + IsPartial.GetHashCode();
            hash = hash * 31 + HasClassification.GetHashCode();
            hash = hash * 31 + HasCode.GetHashCode();
            hash = hash * 31 + BaseHasVirtualCode.GetHashCode();

            return hash;
        }
    }

    /// <summary>Returns true when the candidate has mark comment; otherwise false.</summary>
    /// <param name="candidate">The type declaration that may carry the mark.</param>
    /// <returns>True when the candidate has mark comment; otherwise false.</returns>
    private static bool HasMarkComment(TypeDeclarationSyntax candidate)
    {
        foreach (var trivia in candidate.GetLeadingTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                && !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                && !trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                continue;
            }

            var text = trivia.ToFullString();

            if (text.Contains("<implements") || text.Contains("<proves"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the name of.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The name of as text.</returns>
    private static string FullNameOf(INamedTypeSymbol symbol)
    {
        var elements = new List<string>();

        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            elements.Add(current.MetadataName);
        }

        elements.Reverse();

        var namespaces = NamespaceParts(symbol);
        var dotted = string.Join(".", elements);

        return namespaces.Count == 0 ? dotted : string.Join(".", namespaces) + "." + dotted;
    }

    /// <summary>Returns the namespace of the symbol.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The namespace of the symbol.</returns>
    private static string NamespaceOf(INamedTypeSymbol symbol)
    {
        var parts = NamespaceParts(symbol);
        var escaped = new List<string>();

        foreach (var part in parts)
        {
            escaped.Add(Escape(part));
        }

        return string.Join(".", escaped);
    }

    /// <summary>Returns the parts.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The parts, in the order this method builds them, or an empty collection when none apply.</returns>
    private static List<string> NamespaceParts(INamedTypeSymbol symbol)
    {
        var parts = new List<string>();

        for (var current = symbol.ContainingNamespace;
            current is not null && !current.IsGlobalNamespace;
            current = current.ContainingNamespace)
        {
            parts.Add(current.Name);
        }

        parts.Reverse();

        return parts;
    }

    /// <summary>Returns the nesting of the symbol.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The nesting of the symbol.</returns>
    private static ImmutableArray<string> NestingOf(INamedTypeSymbol symbol)
    {
        var chain = new List<INamedTypeSymbol>();

        for (var current = symbol.ContainingType; current is not null; current = current.ContainingType)
        {
            chain.Add(current);
        }

        if (chain.Count == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        chain.Reverse();

        var declarations = new List<string>();

        foreach (var outer in chain)
        {
            declarations.Add("partial " + KeywordOf(outer) + " " + Escape(outer.Name) + TypeParametersOf(outer));
        }

        return ImmutableArray.CreateRange(declarations);
    }

    /// <summary>Returns the parameters of.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The parameters of as text.</returns>
    private static string TypeParametersOf(INamedTypeSymbol symbol)
    {
        if (symbol.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        var names = new List<string>();

        foreach (var parameter in symbol.TypeParameters)
        {
            names.Add(Escape(parameter.Name));
        }

        return "<" + string.Join(", ", names) + ">";
    }

    /// <summary>Returns the keyword of the symbol.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The keyword of the symbol.</returns>
    private static string KeywordOf(INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord)
        {
            return symbol.TypeKind == TypeKind.Struct ? "record struct" : "record";
        }

        if (symbol.TypeKind == TypeKind.Struct)
        {
            return "struct";
        }

        if (symbol.TypeKind == TypeKind.Interface)
        {
            return "interface";
        }

        return "class";
    }

    /// <summary>
    /// The message a handler serves: the first type argument of its command or query handler
    /// interface, read by interface name and arity in any namespace. A class implementing several
    /// handler interfaces takes the first message in ordinal order, so the choice is stable.
    /// Empty when the type is no class, implements no handler interface, or the message names a
    /// type parameter or an unresolvable type, which generated code cannot name.
    /// </summary>
    /// <param name="symbol">The type symbol.</param>
    /// <returns>The fully qualified message name, or empty.</returns>
    private static string MessageFullNameOf(INamedTypeSymbol symbol)
    {
        if (symbol.TypeKind != TypeKind.Class)
        {
            return string.Empty;
        }

        string? found = null;

        foreach (var contract in symbol.AllInterfaces)
        {
            if ((contract.Name != "ICommandHandler" && contract.Name != "IQueryHandler")
                || contract.TypeArguments.Length == 0)
            {
                continue;
            }

            var message = contract.TypeArguments[0];

            if (message.TypeKind == TypeKind.Error || ContainsTypeParameter(message))
            {
                continue;
            }

            var display = message.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (found is null || string.Compare(display, found, StringComparison.Ordinal) < 0)
            {
                found = display;
            }
        }

        return found ?? string.Empty;
    }

    /// <summary>Returns true when the contains-type-parameter condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the contains-type-parameter condition holds; otherwise false.</returns>
    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;
            case IArrayTypeSymbol array:
                return ContainsTypeParameter(array.ElementType);
            case IPointerTypeSymbol pointer:
                return ContainsTypeParameter(pointer.PointedAtType);
            case INamedTypeSymbol named:
                foreach (var argument in named.TypeArguments)
                {
                    if (ContainsTypeParameter(argument))
                    {
                        return true;
                    }
                }

                return named.ContainingType is not null && ContainsTypeParameter(named.ContainingType);
            default:
                return false;
        }
    }

    /// <summary>Returns true when the base-declares-virtual-code condition holds; otherwise false.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>True when the base-declares-virtual-code condition holds; otherwise false.</returns>
    private static bool BaseDeclaresVirtualCode(INamedTypeSymbol symbol)
    {
        for (var current = symbol.BaseType; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers("Code"))
            {
                if (member is IPropertySymbol property
                    && (property.IsVirtual || property.IsAbstract || property.IsOverride)
                    && property.Type.SpecialType == SpecialType.System_String)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Escapes the identifier with a verbatim prefix when it is a keyword.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <returns>The string as text.</returns>
    private static string Escape(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
        || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
            ? "@" + name
            : name;
}
