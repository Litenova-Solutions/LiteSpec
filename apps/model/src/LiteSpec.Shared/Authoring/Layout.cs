using System.Text.Json.Nodes;

namespace LiteSpec.Authoring;

/// <summary>
///     The folder-based stack layout: the stacks a manifest ships and the pages a
///     selection applies. Pure computation over the manifest and the docs tree, so the
///     consumer validator and the model builder can call it without console output.
/// </summary>
/// <follows>litespec/core/stacks#apply-the-complete-stack</follows>
public static class Layout
{
    /// <summary>One shipped stack: its id, folder, kind, and pairing rules.</summary>
    /// <param name="Id">The stack id a project names.</param>
    /// <param name="Folder">The stack folder, such as <c>docs/backend/dotnet</c>.</param>
    /// <param name="Kind"><c>backend</c> or <c>frontend</c>, from the folder's area.</param>
    /// <param name="Requires">Stacks a project must also select with this one.</param>
    /// <param name="PairsWith">The only backend stacks a frontend stack works beside.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Layout.Stack names one shipped stack in the manifest model; renaming the public nested type breaks every host that reads the stack list.")]
    public sealed record Stack(string Id, string Folder, string Kind, IReadOnlyList<string> Requires, IReadOnlyList<string> PairsWith);

    /// <summary>Reads the stacks a manifest ships, in manifest order.</summary>
    /// <param name="manifest">The manifest object.</param>
    /// <returns>The stacks the manifest ships, in manifest order.</returns>
    public static IReadOnlyList<Stack> Stacks(JsonObject manifest)
    {
        var result = new List<Stack>();
        if ((manifest["stacks"] as JsonObject) is not { } stacks)
        {
            return result;
        }

        foreach (var (id, node) in stacks)
        {
            var stack = node as JsonObject;
            var folder = Text(stack?["folder"]) ?? "";
            result.Add(new Stack(id, folder, KindFor(folder), Strings(stack?["requires"]), Strings(stack?["pairsWith"])));
        }

        return result;
    }

    /// <summary>
    ///     Computes the pages a selection applies: the always-applied areas, each selected
    ///     stack's folder chain, the named shape's backend pages, and each selected
    ///     extension's page. Returns <c>docs/...</c> relative paths sorted ordinally.
    /// </summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="manifest">The manifest object.</param>
    /// <param name="shape">The shape the project names.</param>
    /// <param name="stacks">The stacks the project selects.</param>
    /// <param name="extensions">The extensions the project selects.</param>
    /// <returns>The applied pages as docs-relative paths, sorted ordinally.</returns>
    public static IReadOnlyList<string> AppliedPages(string root, JsonObject manifest, string shape, IReadOnlyList<string> stacks, IReadOnlyList<string> extensions)
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);

        foreach (var area in new[] { "docs/core", "docs/workspace", "docs/quality" })
        {
            foreach (var page in PagesDirectlyIn(root, area))
            {
                applied.Add(page);
            }
        }

        var shipped = Stacks(manifest).ToDictionary(stack => stack.Id, StringComparer.Ordinal);
        foreach (var id in stacks)
        {
            if (!shipped.TryGetValue(id, out var stack) || stack.Kind.Length == 0)
            {
                continue;
            }

            var area = stack.Kind == "backend" ? "docs/backend" : "docs/frontend";
            foreach (var folder in ChainFolders(stack.Folder, area).SelectMany(folder => CompanionFolders(root, folder).Prepend(folder)))
            {
                foreach (var page in PagesDirectlyIn(root, folder))
                {
                    applied.Add(page);
                }
            }
        }

        if ((manifest["shapes"] as JsonObject)?[shape] as JsonObject is { } named)
        {
            foreach (var page in Strings(named["backendPages"]))
            {
                applied.Add(page);
            }
        }

        var shippedExtensions = manifest["extensions"] as JsonObject;
        foreach (var id in extensions)
        {
            if (Text((shippedExtensions?[id] as JsonObject)?["path"]) is { Length: > 0 } path)
            {
                applied.Add(path);
            }
        }

        return applied.OrderBy(page => page, StringComparer.Ordinal).ToList();
    }

    /// <summary>Derives a stack's kind from its folder's area, or empty when outside both areas.</summary>
    /// <param name="folder">The stack folder.</param>
    /// <returns>The stack kind, or empty when the folder sits outside both areas.</returns>
    public static string KindFor(string folder) =>
        folder == "docs/backend" || folder.StartsWith("docs/backend/", StringComparison.Ordinal) ? "backend"
        : folder == "docs/frontend" || folder.StartsWith("docs/frontend/", StringComparison.Ordinal) ? "frontend"
        : "";

    // A stack applies its folder and every folder up to its area root, so a page
    // directly in any of those folders is reached and anything deeper is not.
    /// <summary>Lists a stack folder and every folder up to its area root.</summary>
    /// <param name="folder">The stack folder.</param>
    /// <param name="area">The area root folder.</param>
    /// <returns>The folder chain from the stack folder up to the area root.</returns>
    static IEnumerable<string> ChainFolders(string folder, string area)
    {
        var current = folder.Replace('\\', '/').TrimEnd('/');
        while (current.Length >= area.Length
            && current.StartsWith(area, StringComparison.Ordinal)
            && (current.Length == area.Length || current[area.Length] == '/'))
        {
            yield return current;
            if (current.Length == area.Length)
            {
                yield break;
            }

            current = current[..current.LastIndexOf('/')];
        }
    }

    /// <summary>
    ///     The folders that apply with <paramref name="folder"/>: each subfolder, at any
    ///     depth, that is not an <c>ext/</c> folder and holds no stack. A stack folder is
    ///     one with a <c>stack.md</c> page, so <c>docs/frontend/patterns</c> applies with
    ///     <c>docs/frontend</c> while <c>docs/frontend/web</c>, which holds stacks, does not.
    /// </summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="folder">The folder that applies.</param>
    /// <returns>The companion folders, in ordinal order.</returns>
    public static IEnumerable<string> CompanionFolders(string root, string folder)
    {
        var directory = Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var sub in Directory.GetDirectories(directory).OrderBy(name => name, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(sub);
            if (name == "ext" || HoldsStack(sub))
            {
                continue;
            }

            var relative = $"{folder.TrimEnd('/')}/{name}";
            yield return relative;

            foreach (var deeper in CompanionFolders(root, relative))
            {
                yield return deeper;
            }
        }
    }

    /// <summary>Whether the folder holds a stack, directly or below it.</summary>
    /// <param name="directory">The folder to test.</param>
    /// <returns>True when the folder holds a stack.</returns>
    static bool HoldsStack(string directory) =>
        File.Exists(Path.Combine(directory, "stack.md"))
        || Directory.GetDirectories(directory).Any(sub => Path.GetFileName(sub) != "ext" && HoldsStack(sub));

    /// <summary>Lists the pages directly in one folder.</summary>
    /// <param name="root">The LiteSpec release root.</param>
    /// <param name="folder">The folder to read.</param>
    /// <returns>The pages directly in the folder, as docs-relative paths.</returns>
    static IEnumerable<string> PagesDirectlyIn(string root, string folder)
    {
        var directory = Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(directory);
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            if (!file.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file) == "README.md")
            {
                continue;
            }

            yield return Slash(Path.GetRelativePath(root, file));
        }
    }

    /// <summary>Reads a JSON node as text.</summary>
    /// <param name="node">The node to read.</param>
    /// <returns>The text, or null when the node holds no string.</returns>
    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Reads a JSON array as a list of strings.</summary>
    /// <param name="node">The node to read.</param>
    /// <returns>The strings in array order; empty when the node holds no array.</returns>
    static List<string> Strings(JsonNode? node) => node is JsonArray array
        ? array.Select(Text).Where(text => text is not null).Select(text => text!).ToList()
        : [];

    /// <summary>Writes a path with forward slashes.</summary>
    /// <param name="value">The path as the operating system writes it.</param>
    /// <returns>The path with forward slashes.</returns>
    static string Slash(string value) => value.Replace('\\', '/');
}
