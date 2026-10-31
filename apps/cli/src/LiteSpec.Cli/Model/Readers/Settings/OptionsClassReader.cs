using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Settings;

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

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            files.AddRange(SourceTree.Files(root, ".cs"));
        }

        var found = new List<OptionsClass>();

        foreach (var file in files)
        {
            ReadFile(file, found);
        }

        found.Sort((left, right) =>
        {
            var path = string.Compare(left.File, right.File, StringComparison.Ordinal);

            return path != 0 ? path : left.Line.CompareTo(right.Line);
        });

        return found;
    }

    /// <summary>
    ///     Reads one source file into the options class collection, adding every marked options class it
    ///     declares.
    /// </summary>
    /// <param name="file">The source file path to read.</param>
    /// <param name="found">The options classes collected so far, added to in place.</param>
    private static void ReadFile(string file, List<OptionsClass> found)
    {
        SyntaxTree tree;

        try
        {
            tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(LanguageVersion.Preview), file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

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
