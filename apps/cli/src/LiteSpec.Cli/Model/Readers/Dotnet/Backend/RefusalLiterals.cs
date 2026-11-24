using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

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
    ///     constant named <c>Code</c> or <c>RefusalCode</c> on an exception type, as the first
    ///     argument of a base call passing a code and a message, as an argument of a
    ///     <c>new ...Exception(...)</c>, or as a value named <c>code</c> (a named argument,
    ///     an anonymous object member or an assignment).
    /// </summary>
    /// <param name="token">The literal token.</param>
    /// <returns>True when the literal sits where code declares a refusal code.</returns>
    public static bool Declares(SyntaxToken token) =>
        DeclaresAt(token.Parent?.AncestorsAndSelf() ?? []);

    /// <summary>
    ///     Whether an expression sits where code declares a refusal code: the same positions
    ///     a literal declares from, so a generated constant reads where a literal would.
    /// </summary>
    /// <param name="expression">The expression holding the code.</param>
    /// <returns>True when the expression sits where code declares a refusal code.</returns>
    public static bool Declares(ExpressionSyntax expression) =>
        DeclaresAt(expression.AncestorsAndSelf());

    /// <summary>Whether any node on the walk from a literal or expression declares a refusal code.</summary>
    /// <param name="nodes">The nodes from the literal or expression up to its members.</param>
    /// <returns>True when the walk reaches a declaring position before any statement or member.</returns>
    static bool DeclaresAt(IEnumerable<SyntaxNode> nodes)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case PropertyDeclarationSyntax property:
                    return IsCodeMember(property.Identifier.ValueText) && RefusalType(property);
                case FieldDeclarationSyntax field:
                    return field.Declaration.Variables.Any(variable => IsCodeMember(variable.Identifier.ValueText)) && RefusalType(field);
                case ArgumentSyntax { NameColon.Name.Identifier.ValueText: var name } when IsCodeName(name):
                    return true;
                case ArgumentSyntax argument when IsBaseCodeArgument(argument):
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

        static bool IsCodeMember(string name) =>
            name is "Code" or "RefusalCode";

        // The first argument of a base call passing at least a code and a message,
        // inside an exception type: a single base argument carries the message alone.
        static bool IsBaseCodeArgument(ArgumentSyntax argument) =>
            argument.NameColon is null
            && argument.Parent is ArgumentListSyntax list
            && list.Parent is ConstructorInitializerSyntax initializer
            && initializer.ArgumentList.Arguments.Count >= 2
            && ReferenceEquals(initializer.ArgumentList.Arguments[0], argument)
            && argument.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault() is { } type
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

    /// <summary>Every constant candidate under a node: each outermost member access and each identifier outside one, so a generated constant reads where it sits.</summary>
    /// <param name="root">The node whose constants are wanted.</param>
    /// <returns>The member access and identifier expressions under the node.</returns>
    public static IEnumerable<ExpressionSyntax> ConstantCandidates(SyntaxNode root)
    {
        foreach (var node in root.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax access && access.Parent is not MemberAccessExpressionSyntax)
            {
                yield return access;
            }
            else if (node is IdentifierNameSyntax identifier && identifier.Parent is not MemberAccessExpressionSyntax)
            {
                yield return identifier;
            }
        }
    }

    /// <summary>The class enclosing an expression, or null when no class encloses it.</summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The class enclosing the expression, or null when no class encloses it.</returns>
    public static string? EnclosingClass(SyntaxNode expression)
    {
        SyntaxNode? parent = expression;

        while (parent is not null && parent is not BaseTypeDeclarationSyntax)
        {
            parent = parent.Parent;
        }

        return (parent as BaseTypeDeclarationSyntax)?.Identifier.ValueText;
    }
}
