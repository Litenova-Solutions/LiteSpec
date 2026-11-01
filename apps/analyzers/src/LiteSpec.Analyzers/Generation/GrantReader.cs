using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// Reads each use case's access grant from one record file. The record index exposes top-level
/// string fields only, so this reader walks the file with the core YAML reader for
/// the one nested field generation needs, and nothing else.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class GrantReader
{
    /// <summary>
    /// Reads the use-case grants in one record file: the page itself when it is a use case, then its
    /// entries in order. A file that does not parse, or whose top level is no object,
    /// yields nothing. The first grant wins when two entries carry one identifier.
    /// </summary>
    /// <param name="text">The record file's text.</param>
    /// <returns>The grants in read order.</returns>
    public static IReadOnlyList<KeyValuePair<string, string>> Read(string text)
    {
        var found = new List<KeyValuePair<string, string>>();

        JsonText root;

        try
        {
            root = YamlText.Parse(text);
        }
        catch (JsonTextException)
        {
            return found;
        }

        if (root.Kind != JsonKind.Object)
        {
            return found;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        CollectUseCase(root, found, seen);

        if (root.TryGetProperty("records", out var entries) && entries.Kind == JsonKind.Array)
        {
            foreach (var entry in entries.Items)
            {
                if (entry.Kind == JsonKind.Object)
                {
                    CollectUseCase(entry, found, seen);
                }
            }
        }

        return found;
    }

    /// <summary>Collects the use case.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <param name="found">The grants collected so far, by use-case identifier.</param>
    /// <param name="seen">The identifiers already collected, used to keep the first occurrence.</param>
    private static void CollectUseCase(
        JsonText node, List<KeyValuePair<string, string>> found, HashSet<string> seen)
    {
        if (!node.TryGetProperty("type", out var type)
            || type.GetString() != "use-case")
        {
            return;
        }

        if (!node.TryGetProperty("id", out var id)
            || id.GetString() is not string identifier
            || identifier.Length == 0)
        {
            return;
        }

        if (!node.TryGetProperty("access", out var access)
            || access.Kind != JsonKind.Object)
        {
            return;
        }

        if (!access.TryGetProperty("grant", out var grant)
            || grant.GetString() is not string value
            || value.Length == 0)
        {
            return;
        }

        if (seen.Add(identifier))
        {
            found.Add(new KeyValuePair<string, string>(identifier, value));
        }
    }
}
