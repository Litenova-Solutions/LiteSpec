using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>The base types a declaration names, and whether one of them, or an interface a symbol implements, has a given simple name.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class BaseTypeNames
{
    /// <summary>Whether a base of the type is one of the known names.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="known">The known base names.</param>
    /// <returns>True when a base carries a known name.</returns>
    internal static bool BaseCarries(TypeDeclarationSyntax type, IReadOnlyCollection<string> known)
    {
        if (type.BaseList is null || known.Count == 0)
        {
            return false;
        }

        foreach (var baseType in type.BaseList.Types)
        {
            if (known.Contains(SyntaxNames.SimpleName(baseType.Type)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the declaration is a class or a class record.</summary>
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the declaration is class-like.</returns>
    internal static bool IsClassLike(TypeDeclarationSyntax type)
    {
        switch (type)
        {
            case ClassDeclarationSyntax:
                return true;
            case RecordDeclarationSyntax record:
                return !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword);
            default:
                return false;
        }
    }

    /// <summary>Lists the base types a declaration names.</summary>
    /// <param name="type">The type declaration.</param>
    /// <returns>The base types in declaration order.</returns>
    internal static IEnumerable<TypeSyntax> BaseTypes(TypeDeclarationSyntax type)
    {
        if (type.BaseList is null)
        {
            yield break;
        }

        foreach (var item in type.BaseList.Types)
        {
            yield return item.Type;
        }
    }

    /// <summary>Whether a base of the type has the simple name.</summary>
    /// <param name="type">The type declaration.</param>
    /// <param name="name">The simple base name.</param>
    /// <returns>True when a base carries the name.</returns>
    internal static bool HasBaseName(TypeDeclarationSyntax type, string name)
    {
        foreach (var baseType in BaseTypes(type))
        {
            if (SyntaxNames.SimpleName(baseType) == name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the type implements an interface with the name.</summary>
    /// <param name="type">The type symbol.</param>
    /// <param name="name">The interface name.</param>
    /// <returns>True when the type implements the interface.</returns>
    internal static bool Implements(INamedTypeSymbol type, string name)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == name)
            {
                return true;
            }
        }

        return false;
    }
}
