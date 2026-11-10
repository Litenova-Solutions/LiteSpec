using System.Text.Json.Nodes;
using LiteSpec.Ui;

namespace LiteSpec.Model;

/// <summary>
///     The UI source lock beside the shared package, written by <c>litespec model render</c>:
///     each primitive's digest is computed from its source, and the preset and the CLI pin come
///     from the manifest baseline the lock's generator names. The team's entries (name, address,
///     status, paths and dependencies), the registry, the formatter and the generator are kept
///     as the team wrote them, and a lock whose text is already current is left alone.
/// </summary>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class UiSourceLock
{
    /// <summary>The preset fields the lock records, in the order the schema lists them.</summary>
    static readonly string[] PresetKeys =
    [
        "style", "base", "cssVariables", "baseColor", "theme", "chartColor", "font", "fontHeading",
        "icons", "radius", "menuAccent", "menuColor", "componentsStyle",
    ];

    /// <summary>
    ///     Writes the source lock when the project declares a shared package and the lock file exists.
    ///     A missing lock is not created here: the team creates it when it installs the first component.
    /// </summary>
    /// <param name="model">The built project model holding the consumer root.</param>
    /// <returns>The written lock path, relative to the project root, or nothing when the lock is current or absent.</returns>
    public static IReadOnlyList<string> Write(ProjectModel model)
    {
        var root = model.Config.Root;
        var project = DataFile.Read(ConsumerRoot.ProjectFile(root));
        var lockRelative = TextAt(project, "ui", "package", "sourceLock");
        var packageRelative = TextAt(project, "ui", "package", "path");
        if (lockRelative is null || packageRelative is null)
        {
            return [];
        }

        var lockFile = Path.GetFullPath(Path.Combine(root, lockRelative));
        var packageRoot = Path.GetFullPath(Path.Combine(root, packageRelative));
        if (!File.Exists(lockFile))
        {
            return [];
        }

        var manifestFile = LiteSpecHome.Root is { } home ? Path.Combine(home, LiteSpecHome.ManifestFile) : null;
        var version = LiteSpecHome.Version;
        if (manifestFile is null || version is null || !File.Exists(manifestFile))
        {
            return [];
        }

        var manifest = DataFile.Read(manifestFile);
        if (DataFile.Read(lockFile) is not JsonObject lockObject)
        {
            return [];
        }

        // The generator names the CLI that installed the package, which picks the baseline and the
        // CLI pin the same way the lock check does.
        var svelte = TextAt(lockObject, "generator") == "shadcn-svelte";
        var baseline = JsonRead.Chain(manifest, svelte ? "uiBaselineSvelte" : "uiBaseline");
        var cliPin = JsonRead.Chain(manifest, "packages", "npm", svelte ? "shadcn-svelte" : "shadcn");

        if (JsonRead.Truthy(cliPin))
        {
            lockObject["cli"] = cliPin.Node!.DeepClone();
        }

        if (lockObject["preset"] is JsonObject preset && baseline.Node is JsonObject baselineObject)
        {
            foreach (var key in PresetKeys)
            {
                if (baselineObject[key] is { } value)
                {
                    preset[key] = value.DeepClone();
                }
            }
        }

        if (lockObject["components"] is JsonArray components)
        {
            foreach (var component in components)
            {
                // A component's digest covers every path it records, in order, as the check computes it;
                // a component with a path that cannot be read keeps its recorded digest for the check to report.
                if (component is not JsonObject item || item["paths"] is not JsonArray { Count: > 0 } paths)
                {
                    continue;
                }

                var digests = new List<string>();
                foreach (var path in paths)
                {
                    if (path is not JsonValue pathValue || !pathValue.TryGetValue<string>(out var relative))
                    {
                        digests.Clear();
                        break;
                    }

                    var candidate = Path.GetFullPath(Path.Combine(packageRoot, relative));
                    if (!FileWalk.Within(packageRoot, candidate) || !FileWalk.Exists(candidate) || SourceDigest.Of(candidate) is not { } digest)
                    {
                        digests.Clear();
                        break;
                    }

                    digests.Add(digest);
                }

                if (digests.Count == paths.Count)
                {
                    item["digest"] = JsonValue.Create(SourceDigest.OfComponent(digests));
                }
            }
        }

        var header = $"# yaml-language-server: $schema=https://litespec.dev/{version}/schemas/ui-source-lock.schema.yaml\n";
        var text = header + YamlWriter.Write(DataFile.TextOf(lockObject));
        if (File.ReadAllText(lockFile).Replace("\r\n", "\n", StringComparison.Ordinal) == text)
        {
            return [];
        }

        File.WriteAllText(lockFile, text);
        return [PathText.Relative(root, lockFile)];
    }

    /// <summary>The text at a key path, or null when the path is missing or holds no text.</summary>
    /// <param name="node">The parsed document.</param>
    /// <param name="keys">The keys from the root.</param>
    /// <returns>The text, or null.</returns>
    static string? TextAt(JsonNode? node, params string[] keys) =>
        JsonRead.Chain(node, keys).Node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
