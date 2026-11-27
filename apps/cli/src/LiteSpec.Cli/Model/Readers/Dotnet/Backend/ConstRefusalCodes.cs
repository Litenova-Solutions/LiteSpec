using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>
///     The constant refusal codes of one backend read: every <c>const string</c> field with a
///     resolvable initializer, by declaring type and member, so a declaring position that holds
///     a generated constant reads the same as one that types the literal.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class ConstRefusalCodes
{
    /// <summary>
    ///     Collects every <c>const string</c> field of the read into a map from
    ///     <c>Type.Member</c> to its value: a literal initializer reads directly, and a member
    ///     access or identifier initializer resolves through the map itself, so a constant
    ///     holding a generated constant reads the value it names. Generated files are read
    ///     here even though other readers skip them, since they hold the generated values.
    /// </summary>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <returns>The constant string value by declaring type and member.</returns>
    public static IReadOnlyDictionary<string, string> Collect(IReadOnlyDictionary<string, SyntaxTree> trees)
    {
        var pending = new Dictionary<string, (ExpressionSyntax Value, string Type)>(StringComparer.Ordinal);

        foreach (var file in trees.Keys.Order(StringComparer.Ordinal))
        {
            var root = trees[file].GetRoot();

            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                if (!field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword))
                    || !IsStringType(field.Declaration.Type)
                    || field.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText is not { Length: > 0 } type)
                {
                    continue;
                }

                foreach (var variable in field.Declaration.Variables)
                {
                    if (variable.Initializer?.Value is { } value && variable.Identifier.ValueText.Length > 0)
                    {
                        pending.TryAdd(type + "." + variable.Identifier.ValueText, (value, type));
                    }
                }
            }
        }

        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        var settled = new HashSet<string>(StringComparer.Ordinal);

        for (var pass = 0; pass <= pending.Count; pass++)
        {
            var progress = false;

            foreach (var (key, (value, type)) in pending)
            {
                if (!settled.Add(key))
                {
                    continue;
                }

                if (ResolveInitializer(value, type, resolved) is not { } text)
                {
                    settled.Remove(key);
                    continue;
                }

                resolved[key] = text;
                progress = true;
            }

            if (!progress)
            {
                break;
            }
        }

        return resolved;
    }

    /// <summary>
    ///     Resolves an expression to the refusal code it holds: a string literal reads directly,
    ///     a member access reads through the type and member it names, and a bare identifier
    ///     reads the constant of that name in the enclosing type. Null when the expression
    ///     holds no refusal code.
    /// </summary>
    /// <param name="expression">The expression holding the code.</param>
    /// <param name="enclosingType">The simple name of the enclosing type, for a bare identifier, or null outside any type.</param>
    /// <param name="constants">The constant string value by declaring type and member.</param>
    /// <returns>The refusal code the expression holds, or null when it holds none.</returns>
    public static string? Resolve(ExpressionSyntax expression, string? enclosingType, IReadOnlyDictionary<string, string> constants) =>
        Lookup(expression, enclosingType, constants) is { } value && IsRefusalCode(value) ? value : null;

    /// <summary>
    ///     Reads the constant value an expression holds without checking its shape: a string
    ///     literal reads directly, a member access reads through the type and member it names,
    ///     and a bare identifier reads the constant of that name in the enclosing type. Null
    ///     when the expression holds no constant value.
    /// </summary>
    /// <param name="expression">The expression holding the value.</param>
    /// <param name="enclosingType">The simple name of the enclosing type, for a bare identifier, or null outside any type.</param>
    /// <param name="constants">The constant string value by declaring type and member.</param>
    /// <returns>The constant value the expression holds, or null when it holds none.</returns>
    public static string? Lookup(ExpressionSyntax expression, string? enclosingType, IReadOnlyDictionary<string, string> constants)
    {
        var current = expression;

        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        switch (current)
        {
            case LiteralExpressionSyntax literal when literal.Token.Value is string text:
                return text;
            case MemberAccessExpressionSyntax access when MemberKey(access) is { } key
                && constants.TryGetValue(key, out var held):
                return held;
            case IdentifierNameSyntax identifier when enclosingType is { Length: > 0 }
                && constants.TryGetValue(enclosingType + "." + identifier.Identifier.ValueText, out var named):
                return named;
            default:
                return null;
        }
    }

    /// <summary>
    ///     Resolves an expression to the refusal code it holds through the compiler: the constant
    ///     value of the expression when it is a whole refusal code. Null otherwise.
    /// </summary>
    /// <param name="model">The semantic model of the expression.</param>
    /// <param name="expression">The expression holding the code.</param>
    /// <returns>The refusal code the expression holds, or null when it holds none.</returns>
    public static string? ResolveSemantic(SemanticModel model, ExpressionSyntax expression) =>
        LookupSemantic(model, expression) is { } value && IsRefusalCode(value) ? value : null;

    /// <summary>
    ///     Reads the constant value an expression holds through the compiler, without checking its
    ///     shape. Null when the expression holds no constant value.
    /// </summary>
    /// <param name="model">The semantic model of the expression.</param>
    /// <param name="expression">The expression holding the value.</param>
    /// <returns>The constant value the expression holds, or null when it holds none.</returns>
    public static string? LookupSemantic(SemanticModel model, ExpressionSyntax expression)
    {
        var constant = model.GetConstantValue(expression);

        return constant.HasValue && constant.Value is string text ? text : null;
    }

    /// <summary>Whether the value is a whole refusal code: the refusal-code grammar matches all of it.</summary>
    /// <param name="value">The value under test.</param>
    /// <returns>True when the whole value matches the refusal-code grammar.</returns>
    public static bool IsRefusalCode(string value)
    {
        var match = Identifiers.RefusalCode().Match(value);

        return match.Success && match.Value == value;
    }

    /// <summary>Whether the field type is a string: the <c>string</c> keyword or the <c>String</c> name.</summary>
    /// <param name="type">The field type as written.</param>
    /// <returns>True when the type reads as a string.</returns>
    static bool IsStringType(TypeSyntax type) => type switch
    {
        PredefinedTypeSyntax predefined => predefined.Keyword.IsKind(SyntaxKind.StringKeyword),
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "String",
        _ => false,
    };

    /// <summary>Resolves one constant initializer through the values settled so far: a literal reads directly, a member access or identifier reads through the map.</summary>
    /// <param name="value">The initializer holding the value.</param>
    /// <param name="type">The simple name of the declaring type, for a bare identifier.</param>
    /// <param name="resolved">The constant string value settled so far, by declaring type and member.</param>
    /// <returns>The constant value, or null when it names a value not settled yet.</returns>
    static string? ResolveInitializer(ExpressionSyntax value, string type, Dictionary<string, string> resolved)
    {
        var current = value;

        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        return current switch
        {
            LiteralExpressionSyntax literal when literal.Token.Value is string text => text,
            MemberAccessExpressionSyntax access when MemberKey(access) is { } key
                && resolved.TryGetValue(key, out var held) => held,
            IdentifierNameSyntax identifier when resolved.TryGetValue(type + "." + identifier.Identifier.ValueText, out var named) => named,
            _ => null,
        };
    }

    /// <summary>The map key a member access names: its last two identifiers as <c>Type.Member</c>, so a namespace-qualified access still names its type.</summary>
    /// <param name="access">The member access expression.</param>
    /// <returns>The <c>Type.Member</c> key, or null when the access names none.</returns>
    static string? MemberKey(MemberAccessExpressionSyntax access)
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
}
