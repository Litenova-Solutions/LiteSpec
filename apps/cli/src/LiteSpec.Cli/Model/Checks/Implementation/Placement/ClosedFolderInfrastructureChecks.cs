using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the infrastructure: the module Infrastructure folder with its persistence, messaging, pipeline and adapter folders, and each adapter folder with its models and parts.</summary>
internal static partial class ClosedFolderChecks
{
    /// <summary>Reports every folder under a module Pipeline folder that holds folders of its own, and the Pipeline folder past 20 files with no protocol folder. The top keeps the module's HTTP pipeline pieces serving no single use case; past 20 files, one folder per protocol holds that protocol's wire types beside its pieces.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="pipeline">The absolute Pipeline folder of one module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPipelineLevel(ConsumerConfig config, string pipeline, List<Finding> findings)
    {
        var children = ChildFolders(pipeline);

        foreach (var child in children)
        {
            CheckUseCaseFilesOnly(config, child, "protocol", findings);
        }

        CheckUngrouped(config, pipeline, children, _ => true, "group them into one folder per protocol", findings, [".cs"]);
    }

    /// <summary>Reports every folder under a module Infrastructure folder that names no Persistence folder, no Messaging folder, no Pipeline folder and no port folder of the module: each adapter folder pairs with one port folder of the same name. An adapter folder naming a provider module is drift: a module reaches another module through its Contracts, not an adapter.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="infrastructure">The absolute Infrastructure folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="portNames">The port folder names under the module Ports folder.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckInfrastructureLevel(ConsumerConfig config, string infrastructure, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<string> portNames, List<string> modules, List<Finding> findings)
    {
        foreach (var child in ChildFolders(infrastructure))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "Persistence", StringComparison.OrdinalIgnoreCase))
            {
                CheckPersistenceLevel(config, child, moduleName, aggregates, findings);
                continue;
            }

            if (string.Equals(name, "Messaging", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCaseFilesOnly(config, child, "folder", findings);
                continue;
            }

            if (string.Equals(name, "Pipeline", StringComparison.OrdinalIgnoreCase))
            {
                CheckPipelineLevel(config, child, findings);
                continue;
            }

            if (modules.Count > 0 && modules.FirstOrDefault(declared => SameName(name, declared)) is { } provider)
            {
                findings.Add(new Finding("code-placement-drift", $"Adapter folder '{PathText.Relative(config.Root, child)}' names the module '{provider}'; a module reaches another module through its Contracts, not an adapter.", child, 1, null));
            }

            CheckAdapterFolder(config, child, findings);

            if (portNames.Any(port => SameName(name, port)))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Adapter folder '{PathText.Relative(config.Root, child)}' names no port folder; its port sits in 'Ports/{name}/'.", child, 1, null));
        }
    }

    /// <summary>
    ///     Reports the shape of one adapter folder: a subfolder outside Models/ and Parts/, a type beside the
    ///     adapter class instead of Models/ or Parts/, and a Models/ or Parts/ type no adapter file names. The top
    ///     keeps the adapter class with its options class; the external system's wire types sit in Models/, and the
    ///     collaborators the adapter needs sit in Parts/. Several adapters of one port sit side by side.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="adapter">The absolute adapter folder under Infrastructure.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckAdapterFolder(ConsumerConfig config, string adapter, List<Finding> findings)
    {
        foreach (var child in ChildFolders(adapter))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "Models", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Parts", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in adapter '{Path.GetFileName(adapter)}', which holds only its Models/ and Parts/ groupings.", child, 1, null));
        }

        var adapterName = Path.GetFileName(adapter);
        var relative = PathText.Relative(config.Root, adapter);
        var types = AdapterTypesOf(adapter);

        // A named type keeps its own parts: the check follows the adapter files one level
        // into the types they name, so a wire part the adapter names only through its response stays.
        var named = AdapterFileIdentifiers(adapter);

        foreach (var type in types)
        {
            if (!IsDirectChild(adapter, type.File) && named.Contains(type.Name))
            {
                named.UnionWith(type.SignatureNames);
            }
        }

        foreach (var type in types.OrderBy(candidate => candidate.File, StringComparer.Ordinal).ThenBy(candidate => candidate.Line))
        {
            if (IsDirectChild(adapter, type.File))
            {
                if (!IsAdapterType(type, adapterName) && !Path.GetFileName(type.File).EndsWith("Options.cs", StringComparison.Ordinal))
                {
                    findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in adapter folder '{relative}' sits beside the adapter; it belongs in 'Models/' when the external system shapes it, or in 'Parts/' when the adapter calls it.", type.File, type.Line, null));
                }
            }
            else if (!named.Contains(type.Name))
            {
                findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in adapter folder '{relative}' is named by no adapter file; move it to Domain/{{Aggregates}}/ or the use case folder that uses it.", type.File, type.Line, null));
            }
        }
    }

    /// <summary>Whether a declared type is one of its adapter folder's adapters: a class implementing the port interface, or a class named for an adapter. Several adapters of one port sit side by side.</summary>
    /// <param name="type">The declared type under test.</param>
    /// <param name="port">The adapter folder name as written on disk.</param>
    /// <returns>True for a class implementing the port interface or named with the Adapter suffix; otherwise false.</returns>
    private static bool IsAdapterType(PortDeclaredType type, string port) =>
        !type.IsInterface
        && (type.Name.EndsWith("Adapter", StringComparison.Ordinal)
            || type.BaseNames.Any(baseName => string.Equals(baseName, "I" + port, StringComparison.OrdinalIgnoreCase)));

    /// <summary>The simple type names the C# files directly in one adapter folder name anywhere in their text: the names the adapter and options files state.</summary>
    /// <param name="adapter">The absolute adapter folder under Infrastructure.</param>
    /// <returns>The identifier names; a file that cannot be read reads as empty.</returns>
    private static HashSet<string> AdapterFileIdentifiers(string adapter)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in ChildFiles(adapter))
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
            {
                continue;
            }

            SyntaxTree tree;

            try
            {
                tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var identifier in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (!identifier.Identifier.IsMissing && identifier.Identifier.Span.Length > 0)
                {
                    names.Add(identifier.Identifier.ValueText);
                }
            }
        }

        return names;
    }

    /// <summary>Reports every folder under a module Persistence folder that names no declared aggregate of the module, every file directly at the root outside module-wide store setup, at any count, every use case message, result or transport model under it, and every adapter file under it. The Persistence root holds only module-wide store setup; each aggregate's repository implementation, stored documents and mapping sit in its own aggregate folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="persistence">The absolute Persistence folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPersistenceLevel(ConsumerConfig config, string persistence, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, List<Finding> findings)
    {
        foreach (var child in ChildFolders(persistence))
        {
            var name = Path.GetFileName(child);

            if (aggregates.Count > 0 && !aggregates.Any(aggregate => SameName(moduleName, aggregate.Module) && SameName(name, aggregate.Expected)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate of module '{moduleName}'.", child, 1, null));
            }
        }

        var setup = moduleName + "StoreContribution.cs";

        foreach (var file in ChildFiles(persistence))
        {
            var fileName = Path.GetFileName(file);

            // A use case model or adapter file at the root is reported by the scan
            // below with its own remedy, so the root stays silent for those shapes.
            if (string.Equals(fileName, setup, StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || UseCaseModelSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal))
                || AdapterSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal)))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Persistence/, which holds only module-wide store setup ('{setup}') and one folder per aggregate.", file, 1, null));
        }

        List<string> files;

        try
        {
            files = Directory.EnumerateFiles(persistence, "*.cs", SearchOption.AllDirectories)
                .Where(file => !PathText.Relative(persistence, file).Split('/').Any(segment => SourceTree.SkippedFolders.Contains(segment)))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (UseCaseModelSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal)))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds a message, result or transport model, which sits in its use case folder, not in Persistence/.", file, 1, null));
            }
            else if (AdapterSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal)))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an adapter, which sits in its Infrastructure/{{Port}}/ folder, not in Persistence/.", file, 1, null));
            }
        }
    }
}
