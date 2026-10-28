using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Analyzers.Generation;

/// <summary>
/// Reads each use case's access grant from one record page. The record index exposes top-level
/// string fields only, so this reader walks the page's frontmatter with the core JSON reader for
/// the one nested field generation needs, and nothing else.
/// </summary>
internal static class GrantReader
{
    /// <summary>
    /// Reads the use-case grants on one page: the page itself when it is a use case, then its
    /// entries in order. A page with no frontmatter, or frontmatter that is no JSON object,
    /// yields nothing. The first grant wins when two entries carry one identifier.
    /// </summary>
    /// <param name="text">The page's text.</param>
    /// <returns>The grants in read order.</returns>
    public static IReadOnlyList<KeyValuePair<string, string>> Read(string text)
    {
        var found = new List<KeyValuePair<string, string>>();
        var frontmatter = Frontmatter(text);

        if (frontmatter is null)
        {
            return found;
        }

        JsonText root;

        try
        {
            root = JsonText.Parse(frontmatter);
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

    private static string? Frontmatter(string text)
    {
        var lines = text.Split('\n');

        if (lines.Length < 3 || lines[0].TrimEnd('\r') != "---")
        {
            return null;
        }

        var closing = -1;

        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].TrimEnd('\r') == "---")
            {
                closing = index;
                break;
            }
        }

        if (closing < 0)
        {
            return null;
        }

        return string.Join("\n", lines, 1, closing - 1);
    }
}
