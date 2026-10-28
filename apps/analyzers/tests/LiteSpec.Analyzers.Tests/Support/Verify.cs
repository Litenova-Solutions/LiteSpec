using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.Support;

/// <summary>
///     Runs one analyzer over C# sources and compares its diagnostics with the spans the
///     sources mark. A marked span reads <c>{|LS1002:Status|}</c>: the diagnostic ID, then
///     the exact text the diagnostic must cover. A span may name its severity:
///     <c>{|LS5001:Warning:SubmitExpenseCommandHandler|}</c>.
/// </summary>
internal static partial class Verify
{
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>Asserts the analyzer reports exactly the marked spans in one file.</summary>
    public static Task AnalyzerAsync<TAnalyzer>(
        string source, string assemblyName = "Shop.Domain", string? layer = null, bool documentationFile = true)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        AnalyzerAsync<TAnalyzer>([("Source.cs", source)], assemblyName, layer, documentationFile);

    /// <summary>
    ///     Asserts the analyzer reports exactly the marked spans across several files.
    ///     <paramref name="documentationFile"/> false parses as a build without a documentation
    ///     file does, where <c>///</c> lines are ordinary comments.
    /// </summary>
    public static Task AnalyzerAsync<TAnalyzer>(
        IReadOnlyList<(string Path, string Source)> files,
        string assemblyName = "Shop.Domain",
        string? layer = null,
        bool documentationFile = true)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        AnalyzerWithRecordsAsync<TAnalyzer>(files, [], assemblyName, layer, documentationFile);

    /// <summary>
    ///     Asserts the analyzer reports exactly the marked spans with the additional files
    ///     in reach: the project file and the record pages. <paramref name="editorconfig"/>
    ///     carries <c>dotnet_diagnostic</c> severity lines the analyzer config answers.
    /// </summary>
    public static async Task AnalyzerWithRecordsAsync<TAnalyzer>(
        IReadOnlyList<(string Path, string Source)> files,
        IReadOnlyList<(string Path, string Source)> additionalFiles,
        string assemblyName = "Shop.Domain",
        string? layer = null,
        bool documentationFile = true,
        string? editorconfig = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var expected = new List<string>();
        var trees = new List<SyntaxTree>();
        var withSeverity = files.Any(file => DeclaresSeverity(file.Source));
        foreach (var (path, marked) in files)
        {
            var (text, spans) = ParseMarkup(marked);
            var parseOptions = new CSharpParseOptions(
                LanguageVersion.Latest, documentationFile ? DocumentationMode.Parse : DocumentationMode.None);
            var tree = CSharpSyntaxTree.ParseText(SourceText.From(text, Encoding.UTF8), parseOptions, path);
            trees.Add(tree);
            expected.AddRange(spans.Select(span => Describe(span.Id, span.Severity, tree.GetLineSpan(span.Span), withSeverity)));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
                .WithSpecificDiagnosticOptions(CompilerSeverities(editorconfig)));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "The test source does not compile:\n" + string.Join("\n", errors));

        var additional = additionalFiles
            .Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Source))
            .ToImmutableArray();
        var options = new AnalyzerOptions(additional, new GlobalOptionsProvider(layer, editorconfig));
        var diagnostics = await compilation
            .WithAnalyzers([new TAnalyzer()], options)
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        var actual = diagnostics
            .Select(d => Describe(d.Id, d.Severity.ToString(), d.Location.GetLineSpan(), withSeverity))
            .ToList();
        expected.Sort(StringComparer.Ordinal);
        actual.Sort(StringComparer.Ordinal);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Asserts the code fix turns the source into the fixed source: the analyzer's first
    ///     diagnostic in the source, the fix action titled <paramref name="actionTitle"/>
    ///     (or the only action), applied once.
    /// </summary>
    public static Task CodeFixAsync<TAnalyzer, TFix>(
        string source,
        string fixedSource,
        string? actionTitle = null,
        IReadOnlyList<(string Path, string Source)>? additionalFiles = null,
        string assemblyName = "Shop.Domain",
        string? layer = null,
        bool documentationFile = true,
        int? expectedActionCount = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TFix : CodeFixProvider, new() =>
        CodeFixFilesAsync<TAnalyzer, TFix>(
            [("Source.cs", source)], [("Source.cs", fixedSource)], actionTitle,
            additionalFiles, assemblyName, layer, documentationFile, expectedActionCount);

    /// <summary>
    ///     Asserts the code fix turns the sources into the fixed sources, applying the
    ///     titled action (or the only action) to the first diagnostic.
    /// </summary>
    public static async Task CodeFixFilesAsync<TAnalyzer, TFix>(
        IReadOnlyList<(string Path, string Source)> files,
        IReadOnlyList<(string Path, string Source)> fixedFiles,
        string? actionTitle,
        IReadOnlyList<(string Path, string Source)>? additionalFiles,
        string assemblyName = "Shop.Domain",
        string? layer = null,
        bool documentationFile = true,
        int? expectedActionCount = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TFix : CodeFixProvider, new()
    {
        using var workspace = TestWorkspace(files, additionalFiles ?? [], assemblyName, documentationFile);
        var project = workspace.CurrentSolution.Projects.Single();
        var compilation = await project.GetCompilationAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(compilation);

        var errors = compilation!.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "The test source does not compile:\n" + string.Join("\n", errors));

        var options = new AnalyzerOptions(
            (additionalFiles ?? []).Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Source)).ToImmutableArray(),
            new GlobalOptionsProvider(layer, null));
        var diagnostics = (await compilation
            .WithAnalyzers([new TAnalyzer()], options)
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken))
            .Where(d => d.Location.IsInSource)
            .ToList();
        Assert.NotEmpty(diagnostics);

        var provider = new TFix();
        var diagnostic = diagnostics.First(d => provider.FixableDiagnosticIds.Contains(d.Id));
        var document = project.Solution.GetDocument(diagnostic.Location.SourceTree);
        Assert.NotNull(document);

        var actions = new List<CodeAction>();
        await provider.RegisterCodeFixesAsync(new CodeFixContext(
            document!, diagnostic, (action, _) => actions.Add(action), TestContext.Current.CancellationToken));
        Assert.NotEmpty(actions);

        if (expectedActionCount is not null)
        {
            Assert.Equal(expectedActionCount, actions.Count);
        }

        var action = actionTitle is null ? actions.Single() : actions.Single(a => a.Title == actionTitle);

        var changed = await ApplyAsync(action, TestContext.Current.CancellationToken);
        var expected = fixedFiles.ToDictionary(file => file.Path, file => file.Source, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var changedDocument = changed.GetDocumentIdsWithFilePath(file.Path)
                    .Select(changed.GetDocument)
                    .FirstOrDefault(found => found is not null)
                ?? changed.Projects.SelectMany(project => project.Documents)
                    .First(found => found.Name == Path.GetFileName(file.Path));
            var changedText = await changedDocument.GetTextAsync(TestContext.Current.CancellationToken);
            Assert.True(expected.TryGetValue(file.Path, out var fixedSource), $"No fixed source for {file.Path}.");
            Assert.Equal(fixedSource, changedText.ToString());
        }
    }

    /// <summary>
    ///     Asserts Fix All turns the sources into the fixed sources across the solution,
    ///     applying the fix with the equivalence key.
    /// </summary>
    public static async Task CodeFixAllAsync<TAnalyzer, TFix>(
        IReadOnlyList<(string Path, string Source)> files,
        IReadOnlyList<(string Path, string Source)> fixedFiles,
        IReadOnlyList<(string Path, string Source)> additionalFiles,
        string equivalenceKey,
        string assemblyName = "Shop.Domain",
        string? layer = null,
        bool documentationFile = true)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TFix : CodeFixProvider, new()
    {
        using var workspace = TestWorkspace(files, additionalFiles, assemblyName, documentationFile);
        var project = workspace.CurrentSolution.Projects.Single();
        var compilation = await project.GetCompilationAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(compilation);

        var options = new AnalyzerOptions(
            additionalFiles.Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Source)).ToImmutableArray(),
            new GlobalOptionsProvider(layer, null));
        var diagnostics = (await compilation!
            .WithAnalyzers([new TAnalyzer()], options)
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken))
            .Where(d => d.Location.IsInSource)
            .ToList();

        var provider = new TFix();
        var fixAll = provider.GetFixAllProvider();
        Assert.NotNull(fixAll);

        var ids = provider.FixableDiagnosticIds.ToImmutableHashSet(StringComparer.Ordinal);
        var triggered = diagnostics.Where(d => ids.Contains(d.Id)).ToList();
        Assert.NotEmpty(triggered);

        var document = project.Solution.GetDocument(triggered[0].Location.SourceTree);
        Assert.NotNull(document);

        var fixAllContext = new FixAllContext(
            document!, provider, FixAllScope.Solution, equivalenceKey, ids, new FixedDiagnostics(triggered),
            TestContext.Current.CancellationToken);
        var action = await fixAll!.GetFixAsync(fixAllContext);
        Assert.NotNull(action);

        var changed = await ApplyAsync(action!, TestContext.Current.CancellationToken);
        var expected = fixedFiles.ToDictionary(file => file.Path, file => file.Source, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var changedDocument = changed.GetDocumentIdsWithFilePath(file.Path)
                    .Select(changed.GetDocument)
                    .FirstOrDefault(found => found is not null)
                ?? changed.Projects.SelectMany(project => project.Documents)
                    .First(found => found.Name == Path.GetFileName(file.Path));
            var changedText = await changedDocument.GetTextAsync(TestContext.Current.CancellationToken);
            Assert.True(expected.TryGetValue(file.Path, out var fixedSource), $"No fixed source for {file.Path}.");
            Assert.Equal(fixedSource, changedText.ToString());
        }
    }

    private static AdhocWorkspace TestWorkspace(
        IReadOnlyList<(string Path, string Source)> files,
        IReadOnlyList<(string Path, string Source)> additionalFiles,
        string assemblyName,
        bool documentationFile)
    {
        var workspace = new AdhocWorkspace();
        var parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest, documentationFile ? DocumentationMode.Parse : DocumentationMode.None);
        var project = workspace
            .AddProject(ProjectInfo.Create(
                ProjectId.CreateNewId(), VersionStamp.Create(), assemblyName, assemblyName,
                LanguageNames.CSharp, parseOptions: parseOptions))
            .WithCompilationOptions(new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(References);

        foreach (var (path, source) in files)
        {
            project = project.AddDocument(Path.GetFileName(path), SourceText.From(source, Encoding.UTF8), filePath: path).Project;
        }

        foreach (var (path, source) in additionalFiles)
        {
            project = project.AddAdditionalDocument(
                Path.GetFileName(path), SourceText.From(source, Encoding.UTF8), filePath: path).Project;
        }

        workspace.TryApplyChanges(project.Solution);
        return workspace;
    }

    private static async Task<Solution> ApplyAsync(CodeAction action, CancellationToken cancellationToken)
    {
        var operations = await action.GetOperationsAsync(cancellationToken);
        var applied = operations.OfType<ApplyChangesOperation>().Single();
        return applied.ChangedSolution;
    }

    private static string Describe(string id, string? severity, FileLinePositionSpan span, bool withSeverity)
    {
        var position = $"{span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1})-({span.EndLinePosition.Line + 1},{span.EndLinePosition.Character + 1})";

        return withSeverity ? $"{id} {severity} {position}" : $"{id} {position}";
    }

    private static bool DeclaresSeverity(string marked)
    {
        foreach (Match match in Markup().Matches(marked))
        {
            if (SplitSeverity(match.Groups["text"].Value).Severity is not null)
            {
                return true;
            }
        }

        return false;
    }

    private static (string Text, List<(string Id, string? Severity, TextSpan Span)> Spans) ParseMarkup(string marked)
    {
        var text = new StringBuilder();
        var spans = new List<(string Id, string? Severity, TextSpan Span)>();
        var last = 0;
        foreach (Match match in Markup().Matches(marked))
        {
            text.Append(marked, last, match.Index - last);
            var start = text.Length;
            var (severity, content) = SplitSeverity(match.Groups["text"].Value);
            text.Append(content);
            spans.Add((match.Groups["id"].Value, severity, TextSpan.FromBounds(start, text.Length)));
            last = match.Index + match.Length;
        }

        text.Append(marked, last, marked.Length - last);
        return (text.ToString(), spans);
    }

    private static (string? Severity, string Content) SplitSeverity(string text)
    {
        foreach (var severity in new[] { "Error", "Warning", "Info", "Hidden" })
        {
            if (text.StartsWith(severity + ":", StringComparison.Ordinal))
            {
                return (severity, text.Substring(severity.Length + 1));
            }
        }

        return (null, text);
    }

    [GeneratedRegex(@"\{\|(?<id>LS\d{4}):(?<text>.*?)\|\}", RegexOptions.Singleline)]
    private static partial Regex Markup();

    /// <summary>The running .NET's own assemblies, enough to compile any BCL-only test source.</summary>
    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return [.. paths
            .Where(path => Path.GetFileName(path) is var name
                && (name.StartsWith("System.", StringComparison.Ordinal) || name is "System.dll" or "netstandard.dll" or "mscorlib.dll"))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    /// <summary>Supplies <c>build_property.LiteSpecLayer</c> when a test sets a layer.</summary>
    /// <summary>
    ///     The rule severities an .editorconfig sets, as the compiler applies them to every reported
    ///     diagnostic, whatever severity the analyzer reported it with.
    /// </summary>
    private static ImmutableDictionary<string, ReportDiagnostic> CompilerSeverities(string? editorconfig)
    {
        var severities = ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in (editorconfig ?? string.Empty).Split('\n'))
        {
            var match = Regex.Match(line.Trim(), @"^dotnet_diagnostic\.(?<id>[A-Za-z0-9]+)\.severity\s*=\s*(?<value>[a-z]+)");

            if (match.Success)
            {
                severities[match.Groups["id"].Value] = match.Groups["value"].Value switch
                {
                    "error" => ReportDiagnostic.Error,
                    "warning" => ReportDiagnostic.Warn,
                    "suggestion" => ReportDiagnostic.Info,
                    "silent" => ReportDiagnostic.Hidden,
                    "none" => ReportDiagnostic.Suppress,
                    _ => ReportDiagnostic.Default,
                };
            }
        }

        return severities.ToImmutable();
    }

    private sealed class GlobalOptionsProvider(string? layer, string? editorconfig) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(layer, ParseEditorconfig(editorconfig ?? string.Empty));

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private static ImmutableDictionary<string, string> ParseEditorconfig(string editorconfig)
        {
            var severities = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in editorconfig.Split('\n'))
            {
                var trimmed = line.Trim().TrimEnd('\r');
                var key = "dotnet_diagnostic.";
                var infix = ".severity";

                if (trimmed.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    var rest = trimmed.Substring(key.Length);
                    var dot = rest.IndexOf(infix, StringComparison.OrdinalIgnoreCase);

                    if (dot > 0)
                    {
                        var id = rest.Substring(0, dot).Trim();
                        var equals = rest.IndexOf('=', dot + infix.Length);

                        if (equals >= 0)
                        {
                            severities["dotnet_diagnostic." + id + ".severity"] = rest.Substring(equals + 1).Trim();
                        }
                    }
                }
            }

            return severities.ToImmutable();
        }

        private sealed class Options(string? layer, ImmutableDictionary<string, string> severities) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
            {
                if (layer is not null && key == "build_property.LiteSpecLayer")
                {
                    value = layer;
                    return true;
                }

                if (severities.TryGetValue(key, out var severity))
                {
                    value = severity;
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
        private readonly SourceText text = SourceText.From(source, Encoding.UTF8);

        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => text;
    }

    /// <summary>Hands a fix-all run the diagnostics the analyzer reported, per document or project.</summary>
    private sealed class FixedDiagnostics(IReadOnlyList<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return diagnostics.Where(diagnostic => diagnostic.Location.SourceTree == tree).ToList();
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Diagnostic>>(diagnostics.Where(diagnostic => !diagnostic.Location.IsInSource).ToList());

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
    }
}
