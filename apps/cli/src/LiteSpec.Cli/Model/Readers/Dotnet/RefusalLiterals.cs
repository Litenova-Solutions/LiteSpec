using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>The string literals of a file, and whether each one declares a refusal code.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class RefusalLiterals
{
    /// <summary>A kebab-case literal, the shape of a key a composed refusal code is built from.</summary>
    public static readonly System.Text.RegularExpressions.Regex KeyLiteralShape = new(
        "^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>The text of a string literal token, or null when the token holds none.</summary>
    /// <param name="token">The syntax token.</param>
    /// <returns>The text of the string literal token, or null when the token holds none.</returns>
    public static string? LiteralText(SyntaxToken token) =>
        token.Kind() switch
        {
            SyntaxKind.StringLiteralToken
                or SyntaxKind.Utf8StringLiteralToken
                or SyntaxKind.SingleLineRawStringLiteralToken
                or SyntaxKind.MultiLineRawStringLiteralToken
                when token.Value is string text => text,
            SyntaxKind.InterpolatedStringTextToken => token.ValueText,
            _ => null,
        };

    /// <summary>
    ///     Whether a literal sits where code declares a refusal code: inside a property, field or
    ///     constant named <c>Code</c>, as an argument of a <c>new ...Exception(...)</c>, or as a
    ///     value named <c>code</c> (a named argument, an anonymous object member or an assignment).
    /// </summary>
    /// <param name="token">The literal token.</param>
    /// <returns>True when the literal sits where code declares a refusal code.</returns>
    public static bool Declares(SyntaxToken token)
    {
        foreach (var node in token.Parent?.AncestorsAndSelf() ?? [])
        {
            switch (node)
            {
                case PropertyDeclarationSyntax property:
                    return property.Identifier.ValueText == "Code" && RefusalType(property);
                case FieldDeclarationSyntax field:
                    return field.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "Code") && RefusalType(field);
                case ArgumentSyntax { NameColon.Name.Identifier.ValueText: var name } when IsCodeName(name):
                    return true;
                case AnonymousObjectMemberDeclaratorSyntax { NameEquals.Name.Identifier.ValueText: var name }:
                    return IsCodeName(name);
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax } assignment:
                    return IsCodeName(LastName(assignment.Left));
                case BaseObjectCreationExpressionSyntax creation:
                    return creation is ObjectCreationExpressionSyntax { Type: var type }
                        && LastName(type).EndsWith("Exception", StringComparison.Ordinal);
                case StatementSyntax or MemberDeclarationSyntax:
                    return false;
            }
        }

        return false;

        // A Code member declares a refusal on a type named for one; on a closed set of
        // features or kinds the same member name holds a label instead.
        static bool RefusalType(SyntaxNode member) =>
            member.Parent is BaseTypeDeclarationSyntax type
            && (type.Identifier.ValueText.EndsWith("Exception", StringComparison.Ordinal)
                || type.Identifier.ValueText.EndsWith("Failure", StringComparison.Ordinal)
                || type.Identifier.ValueText.EndsWith("Refusal", StringComparison.Ordinal));

        static bool IsCodeName(string name) => string.Equals(name, "code", StringComparison.OrdinalIgnoreCase);

        static string LastName(SyntaxNode node) => node switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            ElementAccessExpressionSyntax element => element.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax key
                ? key.Token.ValueText
                : "",
            _ => "",
        };
    }

    /// <summary>The class enclosing a token, or null when no class encloses it.</summary>
    /// <param name="token">The syntax token.</param>
    /// <returns>The class enclosing the token, or null when no class encloses it.</returns>
    public static string? EnclosingClass(SyntaxToken token)
    {
        var parent = token.Parent;

        while (parent is not null && parent is not BaseTypeDeclarationSyntax)
        {
            parent = parent.Parent;
        }

        return (parent as BaseTypeDeclarationSyntax)?.Identifier.ValueText;
    }
}
