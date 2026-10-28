using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Parses C# snippets and builds compilations for the reader tests.</summary>
internal static class TestTrees
{
    /// <summary>Parses source with documentation, as an analyzer host does.</summary>
    internal static SyntaxTree Parse(string code) => CSharpSyntaxTree.ParseText(code);

    /// <summary>Parses source without documentation mode, as the CLI does.</summary>
    internal static SyntaxTree ParseWithoutDocumentation(string code) =>
        CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.None));

    /// <summary>The first node of its kind in parsed source.</summary>
    internal static T First<T>(string code)
        where T : SyntaxNode =>
        Parse(code).GetRoot().DescendantNodes().OfType<T>().First();

    /// <summary>The first node of its kind in a parsed tree.</summary>
    internal static T First<T>(SyntaxTree tree)
        where T : SyntaxNode =>
        tree.GetRoot().DescendantNodes().OfType<T>().First();

    /// <summary>Builds a compilation from source snippets, with the core library referenced.</summary>
    internal static CSharpCompilation Compile(params string[] sources)
    {
        var trees = sources.Select(code => Parse(code)).ToArray();
        var coreLibrary = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        return CSharpCompilation.Create("Tests", trees, new[] { coreLibrary });
    }
}
