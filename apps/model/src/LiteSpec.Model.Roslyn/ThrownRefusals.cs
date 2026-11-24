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

    /// <summary>
    ///     Reads the refusal types the method body constructs, in line order: each object creation
    ///     whose simple type name ends with <c>Exception</c>, <c>Failure</c> or <c>Refusal</c>. A
    ///     target-typed creation names no type and reads nothing here.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <param name="tree">The parsed syntax tree, for line numbers.</param>
    /// <returns>The constructed refusal types, in line order, without duplicates.</returns>
    /// <exception cref="ArgumentNullException">The method or tree is null.</exception>
    public static IReadOnlyList<ThrownType> OfMethod(MethodDeclarationSyntax method, SyntaxTree tree)
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

        foreach (var creation in method.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (SimpleName(creation.Type) is not string name || !IsRefusalType(name))
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
