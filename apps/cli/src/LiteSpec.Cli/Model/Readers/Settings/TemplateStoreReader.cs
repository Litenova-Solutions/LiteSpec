using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Settings;

/// <summary>
///     Reads the template store: the keys of the top-level dictionary initializer in
///     the file the stack configuration names, with every <c>{{Name}}</c>
///     placeholder in the string literals beneath each key.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</follows>
static class TemplateStoreReader
{
    /// <summary>One <c>{{Name}}</c> placeholder, with optional surrounding whitespace.</summary>
    private static readonly Regex Placeholder = new(
        @"\{\{\s*([A-Za-z_][A-Za-z0-9_]*)\s*\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    ///     Reads the stored templates, in document order, or null when the file is
    ///     missing or cannot be read.
    /// </summary>
    /// <param name="file">The template store file path to read.</param>
    /// <returns>The stored templates in document order, or null when the file is missing or cannot be read.</returns>
    public static IReadOnlyList<StoredTemplate>? Read(string file)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        SyntaxTree tree;

        try
        {
            tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(LanguageVersion.Preview), file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var root = tree.GetRoot();
        var entries = new Dictionary<string, (int Line, HashSet<string> Placeholders)>(StringComparer.Ordinal);

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                ReadInitializer(tree, variable.Initializer?.Value, entries);
            }
        }

        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            ReadInitializer(tree, property.Initializer?.Value, entries);
        }

        return [.. entries
            .OrderBy(entry => entry.Value.Line)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new StoredTemplate(entry.Key, entry.Value.Line, entry.Value.Placeholders))];
    }

    /// <summary>Reads the entries of one top-level dictionary initializer.</summary>
    /// <param name="tree">The syntax tree that maps keys to source lines.</param>
    /// <param name="value">The dictionary initializer expression, or null when the value is none.</param>
    /// <param name="entries">The template entries collected so far, by key, added to in place.</param>
    private static void ReadInitializer(
        SyntaxTree tree, ExpressionSyntax? value, Dictionary<string, (int Line, HashSet<string> Placeholders)> entries)
    {
        var initializer = value switch
        {
            InitializerExpressionSyntax direct => direct,
            BaseObjectCreationExpressionSyntax creation => creation.Initializer,
            _ => null,
        };

        if (initializer is null)
        {
            return;
        }

        foreach (var entry in initializer.Expressions)
        {
            if (KeyedEntry(entry) is { } keyed)
            {
                var line = tree.GetLineSpan(keyed.Key.Span).StartLinePosition.Line + 1;

                if (!entries.TryGetValue(keyed.Text, out var known))
                {
                    known = (line, new HashSet<string>(StringComparer.Ordinal));
                    entries[keyed.Text] = known;
                }

                foreach (var placeholder in PlaceholdersIn(keyed.Values))
                {
                    known.Placeholders.Add(placeholder);
                }
            }
        }
    }

    /// <summary>One keyed entry: the key literal with the value nodes beneath it.</summary>
    /// <param name="entry">The dictionary entry expression to read a key and values from.</param>
    /// <returns>The key literal, its text, and the value nodes beneath it, or null when the entry has no literal key.</returns>
    private static (LiteralExpressionSyntax Key, string Text, IReadOnlyList<ExpressionSyntax> Values)? KeyedEntry(ExpressionSyntax entry)
    {
        // ["key"] = value
        if (entry is AssignmentExpressionSyntax assignment
            && ElementKey(assignment.Left) is { } element
            && element.Token.Value is string elementText)
        {
            return (element, elementText, [assignment.Right]);
        }

        // { "key", value }
        if (entry is InitializerExpressionSyntax nested
            && nested.Expressions.Count >= 1
            && nested.Expressions[0] is LiteralExpressionSyntax key
            && key.Token.Value is string text)
        {
            return (key, text, [.. nested.Expressions.Skip(1)]);
        }

        return null;
    }

    /// <summary>The key literal of an element access, or null when the access names no literal key.</summary>
    /// <param name="accessed">The element access expression whose bracket key is wanted.</param>
    /// <returns>The key literal, or null when the element access names no literal key.</returns>
    private static LiteralExpressionSyntax? ElementKey(ExpressionSyntax accessed)
    {
        SeparatedSyntaxList<ArgumentSyntax> arguments;

        if (accessed is ImplicitElementAccessSyntax implicitAccess)
        {
            arguments = implicitAccess.ArgumentList.Arguments;
        }
        else if (accessed is ElementAccessExpressionSyntax explicitAccess)
        {
            arguments = explicitAccess.ArgumentList.Arguments;
        }
        else
        {
            return null;
        }

        return arguments is [{ Expression: LiteralExpressionSyntax key }] ? key : null;
    }

    /// <summary>Every <c>{{Name}}</c> placeholder in the string literals beneath the value nodes.</summary>
    /// <param name="values">The value nodes beneath which string literals are searched.</param>
    /// <returns>Every placeholder name used in the string literals beneath the value nodes.</returns>
    private static HashSet<string> PlaceholdersIn(IReadOnlyList<SyntaxNode> values)
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var value in values)
        {
            foreach (var token in value.DescendantTokens())
            {
                var text = token.Kind() switch
                {
                    SyntaxKind.StringLiteralToken
                        or SyntaxKind.Utf8StringLiteralToken
                        or SyntaxKind.SingleLineRawStringLiteralToken
                        or SyntaxKind.MultiLineRawStringLiteralToken
                            when token.Value is string literal => literal,
                    SyntaxKind.InterpolatedStringTextToken => token.ValueText,
                    _ => null,
                };

                if (text is null)
                {
                    continue;
                }

                foreach (Match match in Placeholder.Matches(text))
                {
                    placeholders.Add(match.Groups[1].Value);
                }
            }
        }

        return placeholders;
    }
}
