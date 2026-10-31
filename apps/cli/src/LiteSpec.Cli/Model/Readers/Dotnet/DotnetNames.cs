using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>The names of .NET declarations, as the backend reader writes them.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class DotnetNames
{
    /// <summary>The namespace-qualified name of a type, including each containing type.</summary>
    /// <param name="declared">The type declaration.</param>
    /// <returns>The namespace-qualified name of the type.</returns>
    public static string QualifiedNameOf(TypeDeclarationSyntax declared)
    {
        var parts = new List<string>();

        for (var node = declared.Parent; node is not null; node = node.Parent)
        {
            if (node is TypeDeclarationSyntax containing)
            {
                parts.Add(containing.Identifier.ValueText);
            }
            else if (node is BaseNamespaceDeclarationSyntax space)
            {
                parts.Add(space.Name.ToString());
            }
        }

        parts.Reverse();
        parts.Add(declared.Identifier.ValueText);

        return string.Join(".", parts);
    }

    /// <summary>The namespace holding the declaration, or empty in the global namespace.</summary>
    /// <param name="declared">The type declaration.</param>
    /// <returns>The namespace holding the declaration, or empty in the global namespace.</returns>
    public static string NamespaceOf(TypeDeclarationSyntax declared)
    {
        for (var node = declared.Parent; node is not null; node = node.Parent)
        {
            if (node is BaseNamespaceDeclarationSyntax space)
            {
                return space.Name.ToString();
            }
        }

        return "";
    }

    /// <summary>The simple type name without generics or qualification.</summary>
    /// <param name="type">The type name as written.</param>
    /// <returns>The simple type name without generics or qualification.</returns>
    public static string SimpleName(string type)
    {
        var kind = KindOf(type);
        var dot = kind.LastIndexOf('.');

        return dot >= 0 ? kind[(dot + 1)..] : kind;
    }

    /// <summary>The receiver kind: the simple type name without its generic arguments.</summary>
    /// <param name="type">The type name as written.</param>
    /// <returns>The simple type name without its generic arguments.</returns>
    public static string KindOf(string type) =>
        type.Contains('<') ? type[..type.IndexOf('<')] : type;

    /// <summary>The base name of a type syntax: its simple name with any generic arguments.</summary>
    /// <param name="type">The type syntax.</param>
    /// <returns>The base name of the type syntax.</returns>
    public static string BaseName(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => $"{generic.Identifier.ValueText}<{string.Join(",", generic.TypeArgumentList.Arguments)}>",
        IdentifierNameSyntax plain => plain.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.ToString(),
        _ => type.ToString(),
    };

    /// <summary>The simple type name: unqualified, without nullability, keeping generic arguments.</summary>
    /// <param name="type">The type syntax.</param>
    /// <returns>The simple type name.</returns>
    public static string SimpleTypeName(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => $"{generic.Identifier.ValueText}<{string.Join(",", generic.TypeArgumentList.Arguments.Select(argument => argument.ToString()))}>",
        IdentifierNameSyntax plain => plain.Identifier.ValueText,
        QualifiedNameSyntax qualified => SimpleTypeName(qualified.Right),
        NullableTypeSyntax nullable => SimpleTypeName(nullable.ElementType),
        ArrayTypeSyntax array => SimpleTypeName(array.ElementType),
        _ => type.ToString(),
    };
}
