using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     The refusal-typed values one method body builds: each constructed exception, failure or
///     refusal type with its line. The refusal check reads this for every root method and helper
///     a handler calls, so a thrown code meets a result its use case declares.
/// </summary>
/// <follows>litespec/backend/application#return-only-declared-results-and-refusals</follows>
public static class ThrownRefusals
{
    /// <summary>One constructed refusal type: its simple name with its line.</summary>
    public sealed class ThrownType
    {
        /// <summary>Initializes a new instance of the <see cref="ThrownType" /> class.</summary>
        /// <param name="name">The simple type name.</param>
        /// <param name="line">The 1-based line of the construction.</param>
        public ThrownType(string name, int line)
        {
            Name = name;
            Line = line;
        }

        /// <summary>Gets the simple type name.</summary>
        public string Name { get; }

        /// <summary>Gets the 1-based line of the construction.</summary>
        public int Line { get; }
    }

    /// <summary>One refusal code a construction passes directly: the code with its line.</summary>
    public sealed class ThrownCode
    {
        /// <summary>Initializes a new instance of the <see cref="ThrownCode" /> class.</summary>
        /// <param name="code">The refusal code.</param>
        /// <param name="line">The 1-based line of the argument.</param>
        public ThrownCode(string code, int line)
        {
            Code = code;
            Line = line;
        }

        /// <summary>Gets the refusal code.</summary>
        public string Code { get; }

        /// <summary>Gets the 1-based line of the argument.</summary>
        public int Line { get; }
    }

    /// <summary>
    ///     Reads the refusal types the method body constructs, in line order: each object creation
    ///     whose simple type name ends with <c>Exception</c>, <c>Failure</c> or <c>Refusal</c>. With
    ///     a semantic model a target-typed creation reads its resolved type; without one it names
    ///     no type and reads nothing here.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <param name="tree">The parsed syntax tree, for line numbers.</param>
    /// <param name="semanticModel">The semantic model of the method, or null to read types as written.</param>
    /// <returns>The constructed refusal types, in line order, without duplicates.</returns>
    /// <exception cref="ArgumentNullException">The method or tree is null.</exception>
    public static IReadOnlyList<ThrownType> OfMethod(MethodDeclarationSyntax method, SyntaxTree tree, SemanticModel? semanticModel = null)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (tree is null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        var found = new List<ThrownType>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var creation in method.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (CreationName(creation, semanticModel) is not string name || !IsRefusalType(name))
            {
                continue;
            }

            if (seen.Add(name))
            {
                found.Add(new ThrownType(name, tree.GetLineSpan(creation.Span).StartLinePosition.Line + 1));
            }
        }

        found.Sort((left, right) => left.Line.CompareTo(right.Line));

        return found;
    }

    /// <summary>
    ///     Reads the refusal codes the method body passes directly when it constructs a refusal
    ///     type: each argument that is a string literal matching the refusal-code grammar in
    ///     full, or a member access or identifier that resolves through the constants map to
    ///     such a value. A bare identifier reads the constant of that name in the enclosing
    ///     type. With a semantic model an argument the map misses reads its constant value, and
    ///     a target-typed creation reads its resolved type; without one both read nothing here.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <param name="tree">The parsed syntax tree, for line numbers.</param>
    /// <param name="constants">The constant string value by declaring type and member.</param>
    /// <param name="semanticModel">The semantic model of the method, or null to read through the map only.</param>
    /// <returns>The directly passed refusal codes, in line order, without duplicates.</returns>
    /// <exception cref="ArgumentNullException">The method, tree or constants are null.</exception>
    public static IReadOnlyList<ThrownCode> CodesOf(
        MethodDeclarationSyntax method,
        SyntaxTree tree,
        IReadOnlyDictionary<string, string> constants,
        SemanticModel? semanticModel = null)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (tree is null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        if (constants is null)
        {
            throw new ArgumentNullException(nameof(constants));
        }

        var found = new List<ThrownCode>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var enclosing = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText;

        foreach (var creation in method.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (CreationName(creation, semanticModel) is not string name || !IsRefusalType(name))
            {
                continue;
            }

            if (creation.ArgumentList is null)
            {
                continue;
            }

            foreach (var argument in creation.ArgumentList.Arguments)
            {
                if (ResolveCode(argument.Expression, enclosing, constants, semanticModel) is { } code && seen.Add(code))
                {
                    found.Add(new ThrownCode(code, tree.GetLineSpan(argument.Span).StartLinePosition.Line + 1));
                }
            }
        }

        found.Sort((left, right) => left.Line.CompareTo(right.Line));

        return found;
    }

    /// <summary>The simple name of the constructed type: resolved through the model when one reads, else as written.</summary>
    /// <param name="creation">The object creation.</param>
    /// <param name="semanticModel">The semantic model of the method, or null to read the type as written.</param>
    /// <returns>The simple type name, or null when the creation names none.</returns>
    private static string? CreationName(BaseObjectCreationExpressionSyntax creation, SemanticModel? semanticModel)
    {
        var written = creation is ObjectCreationExpressionSyntax explicitCreation ? SimpleName(explicitCreation.Type) : null;

        if (semanticModel is not null && semanticModel.GetTypeInfo(creation).Type is INamedTypeSymbol resolved && resolved.Name.Length > 0)
        {
            return resolved.Name;
        }

        return written;
    }

    /// <summary>Resolves an argument to the refusal code it holds: a literal reads directly, a member access or identifier reads through the constants map, and anything the map misses reads its constant value with a semantic model.</summary>
    /// <param name="expression">The argument holding the code.</param>
    /// <param name="enclosing">The simple name of the enclosing type, for a bare identifier, or null outside any type.</param>
    /// <param name="constants">The constant string value by declaring type and member.</param>
    /// <param name="semanticModel">The semantic model of the method, or null to read through the map only.</param>
    /// <returns>The refusal code, or null when the argument holds none.</returns>
    private static string? ResolveCode(ExpressionSyntax expression, string? enclosing, IReadOnlyDictionary<string, string> constants, SemanticModel? semanticModel)
    {
        var current = expression;

        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        string? value = current switch
        {
            LiteralExpressionSyntax literal when literal.Token.Value is string text => text,
            MemberAccessExpressionSyntax access when MemberKey(access) is { } key
                && constants.TryGetValue(key, out var held) => held,
            IdentifierNameSyntax identifier when enclosing is not null
                && constants.TryGetValue(enclosing + "." + identifier.Identifier.ValueText, out var named) => named,
            _ => null,
        };

        if (value is null && semanticModel is not null)
        {
            var constant = semanticModel.GetConstantValue(current);

            if (constant.HasValue && constant.Value is string held)
            {
                value = held;
            }
        }

        if (value is null)
        {
            return null;
        }

        var match = Identifiers.RefusalCode().Match(value);

        return match.Success && match.Value == value ? value : null;
    }

    /// <summary>The map key a member access names: its last two identifiers as <c>Type.Member</c>.</summary>
    /// <param name="access">The member access expression.</param>
    /// <returns>The <c>Type.Member</c> key, or null when the access names none.</returns>
    private static string? MemberKey(MemberAccessExpressionSyntax access)
    {
        var names = new List<string> { access.Name.Identifier.ValueText };
        var current = access.Expression;

        while (current is MemberAccessExpressionSyntax nested)
        {
            names.Add(nested.Name.Identifier.ValueText);
            current = nested.Expression;
        }

        if (current is IdentifierNameSyntax root)
        {
            names.Add(root.Identifier.ValueText);
        }

        return names.Count >= 2 ? names[1] + "." + names[0] : null;
    }

    /// <summary>Whether the simple type name is a refusal type: it ends with Exception, Failure or Refusal.</summary>
    /// <param name="name">The simple type name.</param>
    /// <returns>True when the name ends with Exception, Failure or Refusal.</returns>
    private static bool IsRefusalType(string name) =>
        name.EndsWith("Exception", StringComparison.Ordinal)
        || name.EndsWith("Failure", StringComparison.Ordinal)
        || name.EndsWith("Refusal", StringComparison.Ordinal);

    /// <summary>Reads the simple name of a type as written.</summary>
    /// <param name="type">The type as written.</param>
    /// <returns>The simple name, or null when the type has none.</returns>
    private static string? SimpleName(TypeSyntax type)
    {
        switch (type)
        {
            case IdentifierNameSyntax plain:
                return plain.Identifier.ValueText;
            case GenericNameSyntax generic:
                return generic.Identifier.ValueText;
            case QualifiedNameSyntax qualified:
                return qualified.Right.Identifier.ValueText;
            case AliasQualifiedNameSyntax alias:
                return alias.Name.Identifier.ValueText;
            default:
                return null;
        }
    }
}
