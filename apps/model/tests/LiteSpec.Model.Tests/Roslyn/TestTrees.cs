using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Parses C# snippets and builds compilations for the reader tests.</summary>
internal static class TestTrees
{
    /// <summary>Parses source with documentation, as an analyzer host does.</summary>
    /// <param name="code">The C# source text to parse.</param>
    /// <returns>The parsed syntax tree.</returns>
    internal static SyntaxTree Parse(string code) => CSharpSyntaxTree.ParseText(code);

    /// <summary>Parses source without documentation mode, as the CLI does.</summary>
    /// <param name="code">The C# source text to parse.</param>
    /// <returns>The parsed syntax tree.</returns>
    internal static SyntaxTree ParseWithoutDocumentation(string code) =>
        CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.None));

    /// <summary>The first node of its kind in parsed source.</summary>
    /// <typeparam name="T">The syntax node type to find.</typeparam>
    /// <param name="code">The C# source text to parse.</param>
    /// <returns>The first node of the requested kind.</returns>
    internal static T First<T>(string code)
        where T : SyntaxNode =>
        Parse(code).GetRoot().DescendantNodes().OfType<T>().First();

    /// <summary>The first node of its kind in a parsed tree.</summary>
    /// <typeparam name="T">The syntax node type to find.</typeparam>
    /// <param name="tree">The syntax tree to search.</param>
    /// <returns>The first node of the requested kind.</returns>
    internal static T First<T>(SyntaxTree tree)
        where T : SyntaxNode =>
        tree.GetRoot().DescendantNodes().OfType<T>().First();

    /// <summary>Builds a compilation from source snippets, with the core library referenced.</summary>
    /// <param name="sources">The C# source texts to compile.</param>
    /// <returns>The compilation built from the sources.</returns>
    internal static CSharpCompilation Compile(params string[] sources)
    {
        var trees = sources.Select(code => Parse(code)).ToArray();
        var coreLibrary = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        return CSharpCompilation.Create("Tests", trees, [coreLibrary]);
    }
}
