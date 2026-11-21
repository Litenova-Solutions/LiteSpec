using System.Collections.Immutable;
using System.Text;
using LiteSpec.Analyzers.Generation;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Rules.Tests;
using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Generation;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Claims;

/// <summary>Tests that the analyzers and the generator follow the record layers the project claims.</summary>
public sealed class LayerClaimTests
{
    /// <summary>The running .NET's own assemblies, enough to compile any BCL-only test source.</summary>
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>A layer rule runs nothing when the project claims no Implementation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "A layer rule runs nothing when the project claims no implementation")]
    public async Task NoImplementationClaimRunsNoLayerRule()
    {
        var diagnostics = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Spending/Domain/Source.cs", "Shop.Spending", Claim(implementation: ""), []);

        Assert.Empty(diagnostics);
    }

    /// <summary>A traceability rule runs nothing when the project claims no Implementation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "A traceability rule runs nothing when the project claims no implementation")]
    public async Task NoImplementationClaimRunsNoTraceabilityRule()
    {
        var diagnostics = await AnalyzeAsync<MarkStyleAnalyzer>(
            HandlerSource(), "Shop.Spending/Application/Source.cs", "Shop.Spending", Claim(implementation: ""), [ProjectFile(), UseCase()]);

        Assert.Empty(diagnostics);
    }

    /// <summary>The generator writes no source and reports no diagnostic when the project claims no Implementation.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "The generator writes no source when the project claims no implementation")]
    public void NoImplementationClaimGeneratesNothing()
    {
        var result = RunGenerator(Claim(implementation: ""));

        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>With own Implementation, a layer rule stays silent and a traceability rule reports.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "An own implementation runs only the traceability rules")]
    public async Task OwnImplementationRunsOnlyTraceability()
    {
        var layer = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Spending/Domain/Source.cs", "Shop.Spending", Claim(implementation: "own"), []);
        var traceability = await AnalyzeAsync<MarkStyleAnalyzer>(
            HandlerSource(), "Shop.Spending/Application/Source.cs", "Shop.Spending", Claim(implementation: "own"), [ProjectFile(), UseCase()]);

        Assert.Empty(layer);
        Assert.Equal(new[] { "LS5004" }, traceability.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>With litespec Implementation, a layer rule and a traceability rule both report.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "A litespec implementation runs every rule")]
    public async Task LiteSpecImplementationRunsEveryRule()
    {
        var layer = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Spending/Domain/Source.cs", "Shop.Spending", Claim(implementation: "litespec"), []);
        var traceability = await AnalyzeAsync<MarkStyleAnalyzer>(
            HandlerSource(), "Shop.Spending/Application/Source.cs", "Shop.Spending", Claim(implementation: "litespec"), [ProjectFile(), UseCase()]);

        Assert.Equal(new[] { "LS1002" }, layer.Select(diagnostic => diagnostic.Id).ToArray());
        Assert.Equal(new[] { "LS5004" }, traceability.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>A build with no claim reads every layer as litespec, as a project file with no layers does.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "A build with no claim reads every layer as litespec")]
    public async Task AbsentClaimRunsEveryRule()
    {
        var layer = await AnalyzeAsync<DomainEnumAnalyzer>(EnumSource(), "Shop.Spending/Domain/Source.cs", "Shop.Spending", Claim(), []);
        var traceability = await AnalyzeAsync<MarkStyleAnalyzer>(
            HandlerSource(), "Shop.Spending/Application/Source.cs", "Shop.Spending", Claim(), [ProjectFile(), UseCase()]);

        Assert.Equal(new[] { "LS1002" }, layer.Select(diagnostic => diagnostic.Id).ToArray());
        Assert.Equal(new[] { "LS5004" }, traceability.Select(diagnostic => diagnostic.Id).ToArray());
    }

    /// <summary>The build property decides the claim, whatever the project file says.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "The build property overrides the implementation the project file names")]
    public async Task BuildPropertyOverridesTheProjectFile()
    {
        var diagnostics = await AnalyzeAsync<DomainEnumAnalyzer>(
            EnumSource(), "Shop.Spending/Domain/Source.cs", "Shop.Spending", Claim(implementation: "own"), [LayersFile("litespec")]);

        Assert.Empty(diagnostics);
    }

    /// <summary>The mark rule on tests runs when the project claims Proof, and stays silent when it claims none.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "The mark rule on tests runs when the project claims proof")]
    public async Task ProofClaimRunsTheMarkRuleOnTests()
    {
        var claimed = await AnalyzeAsync<TestAssertionAnalyzer>(
            UnassertedTestSource(), "Shop.Orders.Tests/Source.cs", "Shop.Orders.Tests", Claim(implementation: "", proof: "litespec"), []);
        var unclaimed = await AnalyzeAsync<TestAssertionAnalyzer>(
            UnassertedTestSource(), "Shop.Orders.Tests/Source.cs", "Shop.Orders.Tests", Claim(implementation: "", proof: ""), []);

        Assert.Equal(new[] { "LS4001" }, claimed.Select(diagnostic => diagnostic.Id).ToArray());
        Assert.Empty(unclaimed);
    }

    /// <summary>The generator writes source and reports its traceability diagnostic with own Implementation.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "The generator writes source and reports its traceability diagnostic with an own implementation")]
    public void OwnImplementationGenerates()
    {
        var result = RunGenerator(Claim(implementation: "own"));

        Assert.NotEmpty(result.GeneratedTrees);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LS5009");
    }

    /// <summary>The generator writes source when the build claims no layers, as a project file with no layers does.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact(DisplayName = "The generator writes source when the build claims no layers")]
    public void AbsentClaimGenerates()
    {
        var result = RunGenerator(Claim());

        Assert.NotEmpty(result.GeneratedTrees);
    }

    /// <summary>The Domain source with an enum a layer rule reports under a litespec implementation.</summary>
    /// <returns>The C# source text.</returns>
    private static string EnumSource() => """
        namespace Shop.Orders;

        public enum OrderStatus { Draft, Placed }
        """;

    /// <summary>The Application source with a handler that carries an attribute mark.</summary>
    /// <returns>The C# source text.</returns>
    private static string HandlerSource() => """
        using System;

        namespace Claimwick.Spending;

        public interface ICommandHandler<TCommand, TResult>;

        public sealed class SubmitExpenseCommand;

        public sealed class SubmitExpenseCommandResult;

        internal sealed class ImplementsAttribute : Attribute
        {
            public ImplementsAttribute(string id) { }
        }

        [Implements("use-case/spending.submit-expense")]
        internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, SubmitExpenseCommandResult>
        {
        }
        """;

    /// <summary>A test source with a test method that asserts nothing.</summary>
    /// <returns>The C# source text.</returns>
    private static string UnassertedTestSource() => """
        using System;

        namespace Shop.Orders.Tests;

        public sealed class FactAttribute : Attribute;

        public sealed class Order
        {
            public void Place() { }
        }

        public sealed class OrderTests
        {
            [Fact]
            public void PlaceWhenDraftMarksPlaced()
            {
                var order = new Order();
                order.Place();
            }
        }
        """;

    /// <summary>A build claim: the implementation and proof values, each when the test names one.</summary>
    /// <param name="implementation">The Implementation value, or null when the build sets none.</param>
    /// <param name="proof">The Proof value, or null when the build sets none.</param>
    /// <returns>The build properties.</returns>
    private static IReadOnlyDictionary<string, string> Claim(string? implementation = null, string? proof = null)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        if (implementation is not null)
        {
            properties[ClaimGate.ImplementationKey] = implementation;
        }

        if (proof is not null)
        {
            properties[ClaimGate.ProofKey] = proof;
        }

        return properties;
    }

    /// <summary>A project file whose layers block names the given Implementation value.</summary>
    /// <param name="implementation">The Implementation value the file names.</param>
    /// <returns>The path and source text of the additional file.</returns>
    private static (string Path, string Source) LayersFile(string implementation) =>
        (ProjectPath, $$"""
            schemaVersion: 1
            layers:
              intent: litespec
              behavior: litespec
              implementation: {{implementation}}
            """);

    /// <summary>Runs one analyzer over a source under the build properties.</summary>
    /// <typeparam name="TAnalyzer">The analyzer under test.</typeparam>
    /// <param name="source">The C# source text to run the analyzer over.</param>
    /// <param name="path">The file path, whose layer folder places the file.</param>
    /// <param name="assemblyName">The assembly name the test source compiles under.</param>
    /// <param name="build">The build properties the analyzers read.</param>
    /// <param name="additionalFiles">The additional files, each with its path and text.</param>
    /// <returns>The diagnostics the analyzer reported.</returns>
    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync<TAnalyzer>(
        string source,
        string path,
        string assemblyName,
        IReadOnlyDictionary<string, string> build,
        IReadOnlyList<(string Path, string Source)> additionalFiles)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse);
        var tree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8), parseOptions, path);
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
        var options = new AnalyzerOptions(additional, new ClaimOptionsProvider(build));

        return await compilation
            .WithAnalyzers([new TAnalyzer()], options)
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Runs the generator over a placeholder source under the build properties.</summary>
    /// <param name="build">The build properties the generator reads.</param>
    /// <returns>The generator run result.</returns>
    private static GeneratorDriverRunResult RunGenerator(IReadOnlyDictionary<string, string> build)
    {
        var compilation = GeneratorVerify.CreateCompilation(
            [("Source.cs", "namespace Shop.Spending; internal sealed class Placeholder;")], "Shop.Spending");
        var additional = RecordPages.Files(
                "attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/a-b", "permission/a--b")))
            .Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Text))
            .ToImmutableArray();
        var driver = CSharpGeneratorDriver.Create(
            [new LiteSpecGenerator().AsSourceGenerator()],
            additionalTexts: additional,
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
            optionsProvider: new ClaimOptionsProvider(build));

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

    /// <summary>Supplies the build properties a test sets; a property the test leaves out is absent.</summary>
    private sealed class ClaimOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public ClaimOptionsProvider(IReadOnlyDictionary<string, string> build)
        {
            GlobalOptions = new BuildOptions(build);
        }

        /// <inheritdoc />
        public override AnalyzerConfigOptions GlobalOptions { get; }

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        /// <summary>Answers each build property with the value the test set, and reports an unset one as absent.</summary>
        private sealed class BuildOptions : AnalyzerConfigOptions
        {
            private readonly IReadOnlyDictionary<string, string> build;

            public BuildOptions(IReadOnlyDictionary<string, string> build)
            {
                this.build = build;
            }

            /// <inheritdoc />
            public override bool TryGetValue(string key, out string value)
            {
                if (build.TryGetValue(key, out var found))
                {
                    value = found;
                    return true;
                }

                value = string.Empty;
                return false;
            }
        }
    }

    /// <summary>An additional file by path and text, for record pages and the project file.</summary>
    private sealed class TestAdditionalText : AdditionalText
    {
        /// <summary>The text the file holds.</summary>
        private readonly SourceText text;

        public TestAdditionalText(string path, string source)
        {
            Path = path;
            text = SourceText.From(source, Encoding.UTF8);
        }

        /// <inheritdoc />
        public override string Path { get; }

        /// <inheritdoc />
        public override SourceText? GetText(CancellationToken cancellationToken = default) => text;
    }
}
