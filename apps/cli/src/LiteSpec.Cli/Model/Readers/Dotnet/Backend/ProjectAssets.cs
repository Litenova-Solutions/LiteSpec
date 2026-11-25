using System.Text.Json;
using LiteSpec.Cli.Model.Configuration;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>The build output of a consumer backend: the project assets a restore wrote and the binaries a build wrote.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class ProjectAssets
{
    /// <summary>Every build output proving a backend was built: each project assets file with one binary per root.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The project assets files and binaries found.</returns>
    public static IReadOnlyList<string> Evidence(ConsumerConfig config)
    {
        var found = new List<string>();

        foreach (var root in SourceRoots.BackendRoots(config))
        {
            var assets = Path.Combine(root, "obj", "project.assets.json");

            if (File.Exists(assets))
            {
                found.Add(assets);
            }

            var bin = Path.Combine(root, "bin");

            if (!Directory.Exists(bin))
            {
                continue;
            }

            try
            {
                foreach (var binary in Directory.EnumerateFiles(bin, "*.dll", SearchOption.AllDirectories))
                {
                    found.Add(binary);
                    break;
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }
        }

        return found;
    }

    /// <summary>Every package binary the project assets files name that exists on disk, as metadata references.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The package references that exist on disk.</returns>
    public static IReadOnlyList<MetadataReference> PackageReferences(ConsumerConfig config)
    {
        var found = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in SourceRoots.BackendRoots(config))
        {
            var assets = Path.Combine(root, "obj", "project.assets.json");

            if (!File.Exists(assets))
            {
                continue;
            }

            foreach (var binary in PackageBinaries(assets))
            {
                if (!seen.Add(binary) || !File.Exists(binary))
                {
                    continue;
                }

                try
                {
                    found.Add(MetadataReference.CreateFromFile(binary));
                }
                catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or BadImageFormatException)
                {
                    continue;
                }
            }
        }

        return found;
    }

    /// <summary>Every package binary one assets file names: one compile asset per package, resolved through its package folder.</summary>
    /// <param name="assetsFile">The project assets file.</param>
    /// <returns>The package binaries the file names, whether or not they exist on disk.</returns>
    static List<string> PackageBinaries(string assetsFile)
    {
        var found = new List<string>();
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(File.ReadAllText(assetsFile));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return found;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("packageFolders", out var folders)
                || folders.ValueKind != JsonValueKind.Array
                || !document.RootElement.TryGetProperty("targets", out var targets)
                || targets.ValueKind != JsonValueKind.Object)
            {
                return found;
            }

            var folderPaths = new List<string>();

            foreach (var folder in folders.EnumerateArray())
            {
                if (folder.ValueKind == JsonValueKind.Object
                    && folder.TryGetProperty("path", out var path)
                    && path.ValueKind == JsonValueKind.String
                    && path.GetString() is { Length: > 0 } folderPath)
                {
                    folderPaths.Add(folderPath.Replace('/', Path.DirectorySeparatorChar));
                }
            }

            // Every target restores the same packages, so the first target names them all.
            foreach (var target in targets.EnumerateObject())
            {
                if (target.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var library in target.Value.EnumerateObject())
                    {
                        if (library.Value.ValueKind == JsonValueKind.Object
                            && CompileAsset(library.Value) is { } asset)
                        {
                            foreach (var folderPath in folderPaths)
                            {
                                found.Add(Path.Combine(folderPath, asset));
                            }
                        }
                    }
                }

                break;
            }
        }

        return found;
    }

    /// <summary>One binary of a package library: a reference asset first, else the first compile or runtime asset.</summary>
    /// <param name="library">The package library entry of the assets target.</param>
    /// <returns>The binary path under its package folder, or null when the entry names none.</returns>
    static string? CompileAsset(JsonElement library)
    {
        if (!library.TryGetProperty("type", out var kind)
            || kind.ValueKind != JsonValueKind.String
            || kind.GetString() != "package"
            || !library.TryGetProperty("path", out var packagePath)
            || packagePath.ValueKind != JsonValueKind.String
            || packagePath.GetString() is not { Length: > 0 } package)
        {
            return null;
        }

        foreach (var table in new[] { "compile", "runtime" })
        {
            if (!library.TryGetProperty(table, out var assets) || assets.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? first = null;

            foreach (var asset in assets.EnumerateObject())
            {
                if (asset.Name.EndsWith("/_._", StringComparison.Ordinal))
                {
                    continue;
                }

                if (asset.Name.StartsWith("ref/", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.Combine(package.Replace('/', Path.DirectorySeparatorChar), asset.Name.Replace('/', Path.DirectorySeparatorChar));
                }

                first ??= asset.Name;
            }

            if (first is not null)
            {
                return Path.Combine(package.Replace('/', Path.DirectorySeparatorChar), first.Replace('/', Path.DirectorySeparatorChar));
            }
        }

        return null;
    }
}
