using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Settings;

/// <summary>
///     Reads the options classes that bind keyed settings: every class carrying an
///     <c>implements</c> mark naming a <c>setting/</c> record, with its
///     <c>SectionName</c> constant value and its property names.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</follows>
static class OptionsClassReader
{
    /// <summary>Reads every marked options class under the .NET source roots.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>Every marked options class under the .NET source roots.</returns>
    public static IReadOnlyList<OptionsClass> Read(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var root in SourceRoots.BackendRoots(config))
        {
            files.AddRange(SourceTree.Files(root, ".cs"));
        }

        var found = new List<OptionsClass>();
        var gate = new object();

        Parallel.ForEach(files, file =>
        {
            var declared = ReadFile(file);

            if (declared.Count > 0)
            {
                lock (gate)
                {
                    found.AddRange(declared);
                }
            }
        });

        found.Sort((left, right) =>
        {
            var path = string.Compare(left.File, right.File, StringComparison.Ordinal);

            return path != 0 ? path : left.Line.CompareTo(right.Line);
        });

        return found;
    }

    /// <summary>
    ///     Reads the marked options classes one source file declares. A mark names its setting in plain
    ///     text, so a file that holds no <c>setting</c> and no character escape that could spell it is
    ///     skipped without being parsed.
    /// </summary>
    /// <param name="file">The source file path to read.</param>
    /// <returns>The marked options classes the file declares, or an empty list when it declares none.</returns>
    private static List<OptionsClass> ReadFile(string file)
    {
        var found = new List<OptionsClass>();
        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return found;
        }

        if (!text.Contains("setting", StringComparison.Ordinal)
            && !text.Contains(@"\u", StringComparison.OrdinalIgnoreCase)
            && !text.Contains(@"\x", StringComparison.Ordinal))
        {
            return found;
        }

        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview), file);

        foreach (var type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var settings = MarkReader.Read(type)
                .Where(mark => mark.Verb == MarkVerb.Implements
                    && mark.RecordId.StartsWith("setting/", StringComparison.Ordinal)
                    && mark.RecordId.Length > "setting/".Length)
                .Select(mark => mark.RecordId)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            if (settings.Count == 0)
            {
                continue;
            }

            found.Add(new OptionsClass(
                type.Identifier.ValueText,
                file,
                tree.GetLineSpan(type.Identifier.Span).StartLinePosition.Line + 1,
                SectionNameOf(type),
                PropertiesOf(type),
                settings));
        }

        return found;
    }

    /// <summary>The <c>SectionName</c> constant value, or null when the class declares none.</summary>
    /// <param name="type">The options class declaration whose SectionName constant is read.</param>
    /// <returns>The constant value, or null when the class declares no SectionName constant.</returns>
    private static string? SectionNameOf(TypeDeclarationSyntax type)
    {
        foreach (var field in type.Members.OfType<FieldDeclarationSyntax>())
        {
            if (!field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword)))
            {
                continue;
            }

            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Identifier.ValueText == "SectionName"
                    && variable.Initializer?.Value is LiteralExpressionSyntax literal
                    && literal.Token.Value is string value)
                {
                    return value;
                }
            }
        }

        return null;
    }

    /// <summary>The instance property names, in document order.</summary>
    /// <param name="type">The options class declaration whose properties are read.</param>
    /// <returns>The instance property names in document order.</returns>
    private static List<string> PropertiesOf(TypeDeclarationSyntax type) =>
        type.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => !property.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
            .Select(property => property.Identifier.ValueText)
            .ToList();
}
