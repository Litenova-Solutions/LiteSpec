using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     The one meaning of partial every host shares: a type is partial when every
///     declaration carries the partial modifier. The analyzer and the generator call
///     this, so a type the analyzer accepts always takes a generated part.
/// </summary>
public static class PartialTypes
{
    /// <summary>Whether every declaration of the type carries the partial modifier.</summary>
    /// <param name="type">The named type symbol.</param>
    /// <returns>True when the type has a declaration and every one is partial.</returns>
    public static bool IsPartial(INamedTypeSymbol type)
    {
        var found = false;

        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not TypeDeclarationSyntax declared)
            {
                continue;
            }

            found = true;

            if (!declared.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return false;
            }
        }

        return found;
    }
}
