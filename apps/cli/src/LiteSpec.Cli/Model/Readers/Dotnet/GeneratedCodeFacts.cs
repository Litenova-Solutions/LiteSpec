using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>Whether a file or a declaration is generated code the reader leaves out.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class GeneratedCodeFacts
{
    /// <summary>Whether the file name is one the compiler treats as generated.</summary>
    /// <param name="file">The file path.</param>
    /// <returns>True when the file name is one the compiler treats as generated.</returns>
    public static bool IsGeneratedFileName(string file)
    {
        var name = file.Replace('\\', '/');
        var slash = name.LastIndexOf('/');
        var shortName = slash < 0 ? name : name.Substring(slash + 1);

        return shortName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || shortName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether the declaration carries a <c>GeneratedCode</c> attribute.</summary>
    /// <param name="declared">The type declaration.</param>
    /// <returns>True when the declaration carries a <c>GeneratedCode</c> attribute.</returns>
    public static bool HasGeneratedCode(TypeDeclarationSyntax declared)
    {
        foreach (var list in declared.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                if (AttributeName(attribute.Name) == "GeneratedCode")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>The simple attribute name without qualification or the <c>Attribute</c> suffix.</summary>
    /// <param name="name">The attribute name syntax.</param>
    /// <returns>The rightmost name segment, with a trailing Attribute removed when a longer name carries it.</returns>
    public static string AttributeName(NameSyntax name)
    {
        var simple = name switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            _ => name.ToString(),
        };

        const string suffix = "Attribute";

        return simple.Length > suffix.Length && simple.EndsWith(suffix, StringComparison.Ordinal)
            ? simple[..^suffix.Length]
            : simple;
    }
}
