using System.Collections.Immutable;
using System.Text;
using LiteSpec.Analyzers.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.Support;

/// <summary>The outcome of one generator run.</summary>
/// <param name="Sources">The generated sources by hint name.</param>
/// <param name="ReportedDiagnostics">The diagnostics the generator reported.</param>
/// <param name="CompileErrors">The errors of the user code with the generated sources.</param>
/// <param name="RunResult">The full run result, with the tracked steps.</param>
internal sealed record GeneratorRun(
    IReadOnlyDictionary<string, string> Sources,
    ImmutableArray<Diagnostic> ReportedDiagnostics,
    IReadOnlyList<Diagnostic> CompileErrors,
    GeneratorDriverRunResult RunResult);

/// <summary>
/// Runs the LiteSpec source generator over C# sources and additional files and returns the
/// generated sources with the diagnostics and the final compilation errors. Sources parse with
/// documentation, as a build with a documentation file does.
/// </summary>
internal static class GeneratorVerify
{
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>Runs the generator once over the sources and additional files.</summary>
    /// <param name="sources">The C# sources with their file paths.</param>
    /// <param name="additionalFiles">The additional files with their file paths.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="layer">The declared LiteSpecLayer property, or null to read the assembly name.</param>
    /// <param name="extraReferences">Further metadata references, such as xUnit.</param>
    /// <returns>The run outcome.</returns>
    public static GeneratorRun Run(
        IReadOnlyList<(string Path, string Source)> sources,
        IReadOnlyList<(string Path, string Text)> additionalFiles,
        string assemblyName = "Shop.Domain",
        string? layer = null,
        IEnumerable<MetadataReference>? extraReferences = null)
    {
        var compilation = CreateCompilation(sources, assemblyName, extraReferences);
        var result = RunToResult(CreateDriver(additionalFiles, layer), compilation);
        var generated = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var item in result.Results)
        {
            foreach (var source in item.GeneratedSources)
            {
                generated[source.HintName] = source.SourceText.ToString();
            }
        }

        var errors = new List<Diagnostic>();

        foreach (var diagnostic in compilation.AddSyntaxTrees(result.GeneratedTrees).GetDiagnostics())
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                errors.Add(diagnostic);
            }
        }

        return new GeneratorRun(generated, result.Diagnostics, errors, result);
    }

    /// <summary>Creates the compilation the generator runs over, parsed with documentation.</summary>
    /// <param name="sources">The C# sources with their file paths.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <param name="extraReferences">Further metadata references, such as xUnit.</param>
    /// <returns>The compilation.</returns>
    public static CSharpCompilation CreateCompilation(
        IReadOnlyList<(string Path, string Source)> sources,
        string assemblyName,
        IEnumerable<MetadataReference>? extraReferences = null)
    {
        var trees = new List<SyntaxTree>();

        foreach (var (path, source) in sources)
        {
            trees.Add(CSharpSyntaxTree.ParseText(
                SourceText.From(source, Encoding.UTF8),
                new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
                path));
        }

        var references = new List<MetadataReference>(References);

        if (extraReferences is not null)
        {
            references.AddRange(extraReferences);
        }

        return CSharpCompilation.Create(
            assemblyName,
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>Creates the generator driver with the additional files and the declared layer.</summary>
    /// <param name="additionalFiles">The additional files with their file paths.</param>
    /// <param name="layer">The declared LiteSpecLayer property, or null to read the assembly name.</param>
    /// <returns>The driver.</returns>
    public static GeneratorDriver CreateDriver(
        IReadOnlyList<(string Path, string Text)> additionalFiles, string? layer)
    {
        var texts = new List<AdditionalText>();

        foreach (var file in additionalFiles)
        {
            texts.Add(new TestAdditionalText(file.Path, file.Text));
        }

        return CSharpGeneratorDriver.Create(
            [new LiteSpecGenerator().AsSourceGenerator()],
            additionalTexts: texts,
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
            optionsProvider: new GlobalOptionsProvider(layer),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    /// <summary>Runs the driver over the compilation and returns the run result.</summary>
    /// <param name="driver">The driver.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The run result.</returns>
    public static GeneratorDriverRunResult RunToResult(GeneratorDriver driver, Compilation compilation) =>
        driver.RunGenerators(compilation).GetRunResult();

    /// <summary>The running .NET's own assemblies, enough to compile any BCL-only test source.</summary>
    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return [.. paths
            .Where(path => Path.GetFileName(path) is var name
                && (name.StartsWith("System.", StringComparison.Ordinal) || name is "System.dll" or "netstandard.dll" or "mscorlib.dll"))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    /// <summary>An additional file held in memory.</summary>
    private sealed class TestAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText source = SourceText.From(text, Encoding.UTF8);

        /// <inheritdoc />
        public override string Path { get; } = path;

        /// <inheritdoc />
        public override SourceText? GetText(CancellationToken cancellationToken = default) => source;
    }

    /// <summary>Supplies <c>build_property.LiteSpecLayer</c> when a test sets a layer.</summary>
    private sealed class GlobalOptionsProvider(string? layer) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(layer);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class Options(string? layer) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
            {
                value = layer ?? string.Empty;
                return layer is not null && key == "build_property.LiteSpecLayer";
            }
        }
    }
}
