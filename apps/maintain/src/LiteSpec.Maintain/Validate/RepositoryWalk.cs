namespace LiteSpec.Maintain.Validate;

/// <summary>How the validators walk the LiteSpec repository: the files under a folder in stable order, joined paths, and which paths are current standards material.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class RepositoryWalk
{
    /// <summary>Gets the comparer that sorts diagnostics ordinally, so their order is identical on every machine.</summary>
    public static StringComparer SortOrder => StringComparer.Ordinal;

    /// <summary>Joins a slash-separated relative path onto a repository root.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="relative">The slash-separated relative path.</param>
    /// <returns>The combined path.</returns>
    public static string JoinRoot(string root, string relative) => Path.Combine([root, .. relative.Split('/')]);

    /// <summary>Collects every file under a directory that matches a predicate, in stable order.</summary>
    /// <param name="directory">The directory to walk.</param>
    /// <param name="predicate">The file predicate.</param>
    /// <returns>The matching files.</returns>
    public static List<string> Walk(string directory, Func<string, bool> predicate)
    {
        var result = new List<string>();
        if (!Directory.Exists(directory))
        {
            return result;
        }

        // Entries sort ordinally so the walk order is stable on every OS. Diagnostics
        // sort before they return, so only duplicate attribution can observe it.
        foreach (var entry in Directory.GetFileSystemEntries(directory).OrderBy(name => name, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(entry);
            if (name is ".git" or ".opencode" or ".agents" or "node_modules" or "dist" or "build" or "bin" or "obj")
            {
                continue;
            }

            if (Directory.Exists(entry))
            {
                // A folder holding its own .git is another checkout (a nested worktree or
                // repository), never this repository's pages.
                if (Path.Exists(Path.Combine(entry, ".git")))
                {
                    continue;
                }

                result.AddRange(Walk(entry, predicate));
            }
            else if (predicate(entry))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    /// <summary>Reports whether a repository-relative path is current standards material.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <returns>True when the path is current standards material.</returns>
    public static bool IsCurrentStandardsMaterial(string relative)
    {
        // The CLI owns src/ and tests/: its fixtures are consumer-shaped test
        // data that can never satisfy the authoring contract, and its own test
        // suite checks the tool. The changelog and its archives are release history.
        // The authoring checks read the standards material.
        // An example under examples/ is a consumer project: the consumer checks own it,
        // and a test runs them on every example.
        return relative != "CHANGELOG.md"
            && relative != "changelog"
            && !relative.StartsWith("changelog/", StringComparison.Ordinal)
            && !relative.StartsWith("apps/", StringComparison.Ordinal)
            && !relative.StartsWith("packages/", StringComparison.Ordinal)
            && !relative.StartsWith("examples/", StringComparison.Ordinal);
    }
}
