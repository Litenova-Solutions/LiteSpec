using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>Reads simple type and attribute names from syntax, without qualification.</summary>
internal static class SyntaxNames
{
    /// <summary>
    ///     The simple name of an attribute, without its namespace qualifier, or null
    ///     when the name is generic: a mark or test attribute is never generic.
    /// </summary>
    internal static string? SimpleName(NameSyntax name)
    {
        switch (name)
        {
            case IdentifierNameSyntax plain:
                return plain.Identifier.ValueText;
            case QualifiedNameSyntax qualified:
                return qualified.Right.Identifier.ValueText;
            case AliasQualifiedNameSyntax alias:
                return alias.Name.Identifier.ValueText;
            default:
                return null;
        }
    }

    /// <summary>The simple name of a base type, without qualification or type arguments.</summary>
    internal static string SimpleName(TypeSyntax type)
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
                return type.ToString();
        }
    }

    /// <summary>Strips one trailing <c>Attribute</c> suffix from a simple name.</summary>
    internal static string StripAttributeSuffix(string name)
    {
        const string suffix = "Attribute";
        return name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal)
            ? name.Substring(0, name.Length - suffix.Length)
            : name;
    }
}
