using System.Collections.Concurrent;
using LiteSpec.Cli.Model.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>
///     The semantic model of one backend read: the compilation built from the parsed trees with
///     framework and package references, so checks read what the compiler knows instead of
///     guessing from syntax. A null holder wherever one is kept means the syntax-only path ran.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
sealed class BackendSemantics
{
    /// <summary>The syntax-only reader mode: no compilation was built.</summary>
    public const string SyntaxMode = "syntax";

    /// <summary>The semantic reader mode: a compilation was built from build output.</summary>
    public const string SemanticMode = "semantic";

    /// <summary>The environment variable forcing a reader mode: syntax or semantic, anything else for automatic.</summary>
    const string ModeVariable = "LITESPEC_BACKEND_READER";

    /// <summary>The cached trusted-list framework references, shared by every read in the process.</summary>
    static List<MetadataReference>? framework;

    /// <summary>The gate guarding the framework cache.</summary>
    static readonly object FrameworkGate = new();

    /// <summary>The compilation built from the read trees.</summary>
    readonly CSharpCompilation compilation;

    /// <summary>The read trees by file path.</summary>
    readonly Dictionary<string, SyntaxTree> trees;

    /// <summary>The semantic model by tree path, built once per tree.</summary>
    readonly ConcurrentDictionary<string, SemanticModel> models = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="BackendSemantics" /> class.</summary>
    /// <param name="compilation">The compilation built from the read trees.</param>
    /// <param name="evidence">The build output the read was built from.</param>
    /// <param name="frameworkCount">The framework references the compilation was built from.</param>
    /// <param name="packageCount">The package references the compilation was built from.</param>
    BackendSemantics(CSharpCompilation compilation, IReadOnlyList<string> evidence, int frameworkCount, int packageCount)
    {
        this.compilation = compilation;
        trees = compilation.SyntaxTrees.ToDictionary(tree => tree.FilePath, StringComparer.Ordinal);
        Evidence = evidence;
        FrameworkReferenceCount = frameworkCount;
        PackageReferenceCount = packageCount;
    }

    /// <summary>Gets the build output the read was built from: the project assets files and binaries found.</summary>
    public IReadOnlyList<string> Evidence { get; }

    /// <summary>Gets the framework references the compilation was built from.</summary>
    public int FrameworkReferenceCount { get; }

    /// <summary>Gets the package references the compilation was built from.</summary>
    public int PackageReferenceCount { get; }

    /// <summary>Gets the reference counts the mode line prints: the framework and package references the compilation was built from.</summary>
    public string ReferenceSummary => $"{FrameworkReferenceCount} framework references, {PackageReferenceCount} package references";

    /// <summary>The semantic model of one tree, or null when the tree is not part of the read.</summary>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <returns>The semantic model of the tree, or null.</returns>
    public SemanticModel? ModelFor(SyntaxTree tree)
    {
        if (!trees.TryGetValue(tree.FilePath, out var owned))
        {
            return null;
        }

        return models.GetOrAdd(tree.FilePath, _ => compilation.GetSemanticModel(owned));
    }

    /// <summary>The semantic model of one file, or null when the file is not part of the read.</summary>
    /// <param name="file">The file path.</param>
    /// <returns>The semantic model of the file, or null.</returns>
    public SemanticModel? ModelFor(string file) =>
        trees.TryGetValue(file, out var tree) ? ModelFor(tree) : null;

    /// <summary>Whether the file is part of the read, with every declaration of the type it holds.</summary>
    /// <param name="file">The file to read.</param>
    /// <param name="typeName">The simple type name.</param>
    /// <param name="parts">Each declaration with its tree, or empty when the file is not part of the read.</param>
    /// <returns>True when the file is part of the read; otherwise false.</returns>
    public bool TryTypesIn(string file, string typeName, out IReadOnlyList<(SyntaxTree Tree, TypeDeclarationSyntax Declared)> parts)
    {
        parts = [];

        if (!trees.TryGetValue(file, out var tree))
        {
            return false;
        }

        parts =
        [
            .. tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Where(declared => declared.Identifier.ValueText == typeName)
                .Select(declared => (tree, declared)),
        ];

        return true;
    }

    /// <summary>
    ///     Builds the semantics of one read: a compilation when the backend has build output,
    ///     else null for the syntax-only path. The mode variable forces either path.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <returns>The read semantics, or null when the syntax-only path runs.</returns>
    public static BackendSemantics? TryCreate(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees) =>
        TryCreate(config, trees, CliHost.Variable(ModeVariable));

    /// <summary>
    ///     Builds the semantics of one read with an explicit mode: syntax forces the
    ///     syntax-only path, semantic forces a build, and anything else builds only when the
    ///     backend has build output. A build that fails falls back to the syntax-only path.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <param name="mode">The forced mode, or null for automatic.</param>
    /// <returns>The read semantics, or null when the syntax-only path runs.</returns>
    internal static BackendSemantics? TryCreate(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees, string? mode) =>
        TryCreateWithReason(config, trees, mode).Semantics;

    /// <summary>
    ///     Builds the semantics of one read with the reason when semantic mode is unavailable:
    ///     a compilation when the backend has build output and framework references exist,
    ///     else null for the syntax-only path. The mode variable forces either path.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <returns>The read semantics with the reason when semantic mode is unavailable.</returns>
    public static (BackendSemantics? Semantics, string? UnavailableReason) TryCreateWithReason(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees) =>
        TryCreateWithReason(config, trees, CliHost.Variable(ModeVariable));

    /// <summary>
    ///     Builds the semantics of one read with an explicit mode and the reason when semantic
    ///     mode is unavailable: syntax forces the syntax-only path, semantic forces a build, and
    ///     anything else builds only when the backend has build output. A build without framework
    ///     references falls back to the syntax-only path with the reason, rather than running
    ///     semantic with no base library; any other failed build falls back silently.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <param name="mode">The forced mode, or null for automatic.</param>
    /// <returns>The read semantics with the reason when semantic mode is unavailable.</returns>
    internal static (BackendSemantics? Semantics, string? UnavailableReason) TryCreateWithReason(ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees, string? mode)
    {
        if (mode == SyntaxMode)
        {
            return (null, null);
        }

        var evidence = ProjectAssets.Evidence(config);

        if (mode != SemanticMode && evidence.Count == 0)
        {
            return (null, null);
        }

        try
        {
            var (frameworkReferences, unavailableReason) = FrameworkReferences(config);

            if (frameworkReferences.Count == 0)
            {
                return (null, unavailableReason ?? "no framework references were found");
            }

            var packageReferences = ProjectAssets.PackageReferences(config);
            var references = new List<MetadataReference>(frameworkReferences.Count + packageReferences.Count);
            references.AddRange(frameworkReferences);
            references.AddRange(packageReferences);
            var compilation = CSharpCompilation.Create(
                "LiteSpecBackendRead",
                trees.Values,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return (new BackendSemantics(compilation, evidence, frameworkReferences.Count, packageReferences.Count), null);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            return (null, null);
        }
    }

    /// <summary>The framework assemblies the read builds from: the running runtime, else the targeting pack matching the consumer.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The framework references with the reason when none was found.</returns>
    static (IReadOnlyList<MetadataReference> References, string? UnavailableReason) FrameworkReferences(ConsumerConfig config)
    {
        if (framework is not null)
        {
            if (framework.Count > 0)
            {
                return (framework, null);
            }
        }
        else
        {
            lock (FrameworkGate)
            {
                framework ??= LoadTrustedPlatform();
            }

            if (framework.Count > 0)
            {
                return (framework, null);
            }
        }

        return TargetingPack.Load(config);
    }

    /// <summary>
    ///     Loads the two framework assemblies symbol resolution needs that exist on disk, skipping
    ///     the rest: the core library defines the predefined types, and the runtime forwards them.
    ///     Every other framework member a check reads falls back to its syntax guess, which names
    ///     no source file either way. A native process has no trusted list, so this loads nothing
    ///     there and the targeting pack resolves instead.
    /// </summary>
    /// <returns>The core library and runtime references that exist on disk.</returns>
    static List<MetadataReference> LoadTrustedPlatform()
    {
        var found = new List<MetadataReference>();

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string list)
        {
            return found;
        }

        // The trusted list joins with the platform separator: a semicolon on Windows, a
        // colon anywhere else. Splitting on a semicolon finds no framework on Unix.
        // The list names the core library twice, so each file loads once.
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in list.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var name = Path.GetFileName(path);

            if (name is not ("System.Private.CoreLib.dll" or "System.Runtime.dll")
                || !File.Exists(path)
                || !seen.Add(name))
            {
                continue;
            }

            try
            {
                found.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or BadImageFormatException)
            {
                continue;
            }
        }

        return found;
    }
}
