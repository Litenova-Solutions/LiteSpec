using System.Collections.Immutable;
using System.Text;
using LiteSpec.Analyzers.Generation;
using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Generation;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Conformance;

/// <summary>Tests that the analyzers and the generator follow the conformance level.</summary>
public sealed class ConformanceLevelTests
{
    /// <summary>The running .NET's own assemblies, enough to compile any BCL-only test source.</summary>
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>A layer rule stays silent at the records level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A layer rule stays silent at the records level")]
    public async Task RecordsLevelSilencesALayerRule()
    {
        var diagnostics = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Domain", "records", []);

        Assert.Empty(diagnostics);
    }

    /// <summary>A layer rule stays silent at the traced level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A layer rule stays silent at the traced level")]
    public async Task TracedLevelSilencesALayerRule()
    {
        var diagnostics = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Domain", "traced", []);

        Assert.Empty(diagnostics);
    }

    /// <summary>A layer rule reports at the full level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A layer rule reports at the full level")]
    public async Task FullLevelReportsALayerRule()
    {
        var diagnostics = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Domain", "full", []);

        Assert.Equal(new[] { "LS1002" }, diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>A layer rule reports when the project names no level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A layer rule reports when the project names no level")]
    public async Task MissingLevelReportsALayerRule()
    {
        var diagnostics = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Domain", null, []);

        Assert.Equal(new[] { "LS1002" }, diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>A layer rule reports when the project names an unknown level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A layer rule reports when the project names an unknown level")]
    public async Task UnknownLevelReportsALayerRule()
    {
        var diagnostics = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Domain", "staged", []);

        Assert.Equal(new[] { "LS1002" }, diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>A traceability rule reports at the traced level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A traceability rule reports at the traced level")]
    public async Task TracedLevelRunsATraceabilityRule()
    {
        var diagnostics = await AnalyzeAsync<MarkPresenceAnalyzer>(
            HandlerSource(), "Shop.Application", "traced", [ProjectFile(), UseCase()]);

        Assert.Equal(new[] { "LS5001" }, diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>A traceability rule reports at the full level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A traceability rule reports at the full level")]
    public async Task FullLevelRunsATraceabilityRule()
    {
        var diagnostics = await AnalyzeAsync<MarkPresenceAnalyzer>(
            HandlerSource(), "Shop.Application", "full", [ProjectFile(), UseCase()]);

        Assert.Equal(new[] { "LS5001" }, diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>A traceability rule stays silent at the records level.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "A traceability rule stays silent at the records level")]
    public async Task RecordsLevelSilencesATraceabilityRule()
    {
        var diagnostics = await AnalyzeAsync<MarkPresenceAnalyzer>(
            HandlerSource(), "Shop.Application", "records", [ProjectFile(), UseCase()]);

        Assert.Empty(diagnostics);
    }

    /// <summary>The generator writes no source and reports no diagnostic at the records level.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "The generator writes no source and reports no diagnostic at the records level")]
    public void RecordsLevelGeneratesNothing()
    {
        var result = RunGenerator("records");

        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>The generator writes source and reports its traceability diagnostic at the traced level.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "The generator writes source and reports its traceability diagnostic at the traced level")]
    public void TracedLevelGenerates()
    {
        var result = RunGenerator("traced");

        Assert.NotEmpty(result.GeneratedTrees);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LS5009");
    }

    /// <summary>The generator writes source when the project names an unknown level.</summary>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Fact(DisplayName = "The generator writes source when the project names an unknown level")]
    public void UnknownLevelGenerates()
    {
        var result = RunGenerator("staged");

        Assert.NotEmpty(result.GeneratedTrees);
    }

    /// <summary>The Domain source with an enum a layer rule reports at the full level.</summary>
    /// <returns>The C# source text.</returns>
    private static string EnumSource() => """
        namespace Shop.Orders;

        public enum OrderStatus { Draft, Placed }
        """;

    /// <summary>The Application source with a handler that carries no mark.</summary>
    /// <returns>The C# source text.</returns>
    private static string HandlerSource() => """
        using System;

        namespace Claimwick.Spending;

        public interface ICommandHandler<TCommand, TResult>;

        public sealed class SubmitExpenseCommand;

        public sealed class SubmitExpenseCommandResult;

        internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
        {
        }
        """;

    /// <summary>Runs one analyzer over a source at the named conformance level.</summary>
    /// <typeparam name="TAnalyzer">The analyzer under test.</typeparam>
    /// <param name="source">The C# source text to run the analyzer over.</param>
    /// <param name="assemblyName">The assembly name the test source compiles under.</param>
    /// <param name="conformance">The conformance level the build property names, or null for none.</param>
    /// <param name="additionalFiles">The additional files, each with its path and text.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync<TAnalyzer>(
        string source,
        string assemblyName,
        string? conformance,
        IReadOnlyList<(string Path, string Source)> additionalFiles)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse);
        var tree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), parseOptions, "Source.cs");
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "The test source does not compile:\n" + string.Join("\n", errors));

        var additional = additionalFiles
            .Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Source))
            .ToImmutableArray();
        var options = new AnalyzerOptions(additional, new ConformanceOptionsProvider(conformance));

        return await compilation
            .WithAnalyzers([new TAnalyzer()], options)
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Runs the generator over a placeholder source at the named conformance level.</summary>
    /// <param name="conformance">The conformance level the build property names, or null for none.</param>
    /// <returns>The generator run result.</returns>
    private static GeneratorDriverRunResult RunGenerator(string? conformance)
    {
        var compilation = GeneratorVerify.CreateCompilation(
            [("Source.cs", "namespace Shop.Spending; internal sealed class Placeholder;")], "Shop.Domain");
        var additional = RecordPages.Files(
                "attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/a-b", "permission/a--b")))
            .Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Text))
            .ToImmutableArray();
        // Shop.Domain names a layer-projects project; the props always name the layout.
        var driver = CSharpGeneratorDriver.Create(
            [new LiteSpecGenerator().AsSourceGenerator()],
            additionalTexts: additional,
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
            optionsProvider: new ConformanceOptionsProvider(conformance, "layer-projects"));

        return driver.RunGenerators(compilation).GetRunResult();
    }

    /// <summary>The running .NET's own assemblies, enough to compile any BCL-only test source.</summary>
    /// <returns>The metadata references of the running .NET assemblies.</returns>
    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return [.. paths
            .Where(path => Path.GetFileName(path) is var name
                && (name.StartsWith("System.", StringComparison.Ordinal) || name is "System.dll" or "netstandard.dll" or "mscorlib.dll"))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    /// <summary>Supplies <c>build_property.LiteSpecConformance</c> when a test sets a level, and <c>build_property.LiteSpecLayout</c> when a test sets a layout.</summary>
    private sealed class ConformanceOptionsProvider(string? conformance, string? layout = null) : AnalyzerConfigOptionsProvider
    {
        /// <inheritdoc />
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(conformance, layout);

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        /// <summary>Answers the conformance and layout properties with the values the test set.</summary>
        private sealed class Options(string? conformance, string? layout) : AnalyzerConfigOptions
        {
            /// <inheritdoc />
            public override bool TryGetValue(string key, out string value)
            {
                if (conformance is not null && key == "build_property.LiteSpecConformance")
                {
                    value = conformance;
                    return true;
                }

                if (layout is not null && key == "build_property.LiteSpecLayout")
                {
                    value = layout;
                    return true;
                }

                value = string.Empty;
                return false;
            }
        }
    }

    /// <summary>An additional file by path and text, for record pages and the project file.</summary>
    private sealed class TestAdditionalText(string path, string source) : AdditionalText
    {
        /// <summary>The text the file holds.</summary>
        private readonly SourceText text = SourceText.From(source, Encoding.UTF8);

        /// <inheritdoc />
        public override string Path { get; } = path;

        /// <inheritdoc />
        public override SourceText? GetText(CancellationToken cancellationToken = default) => text;
    }
}
