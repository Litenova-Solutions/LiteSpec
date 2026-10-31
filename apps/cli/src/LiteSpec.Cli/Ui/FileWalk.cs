namespace LiteSpec.Ui;

/// <summary>
///     Walks the consumer's files for the UI checks: existence, containment,
///     and every file under a directory that matches, in a stable order.
/// </summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class FileWalk
{
    /// <summary>The directory names the walk never enters.</summary>
    static readonly HashSet<string> IgnoredDirectories = new(StringComparer.Ordinal)
    {
        "node_modules", ".next", "out", "dist", "build", ".output", ".svelte-kit", "coverage",
        "android", "ios", ".tanstack", "test-results", "playwright-report",
    };

    /// <summary>Whether a path exists as a file or a directory.</summary>
    /// <param name="path">The path to test.</param>
    /// <returns>True when the path exists.</returns>
    public static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    /// <summary>Whether a candidate path stays inside a root, compared by text.</summary>
    /// <param name="rootPath">The root path.</param>
    /// <param name="candidate">The candidate path.</param>
    /// <returns>True when the candidate stays inside the root.</returns>
    public static bool Within(string rootPath, string candidate)
    {
        var baseDir = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(candidate).StartsWith(baseDir, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Every file under a directory that matches, skipping build output and native
    ///     runtime folders in ordinal name order so the order is stable on every OS.
    /// </summary>
    /// <param name="directory">The directory to walk.</param>
    /// <param name="predicate">Whether a file is collected.</param>
    /// <returns>The collected files.</returns>
    public static List<string> Walk(string directory, Func<string, bool> predicate)
    {
        var result = new List<string>();
        WalkInto(directory, predicate, result);
        return result;
    }

    /// <summary>
    ///     Collects the matching files under one directory into the result.
    /// </summary>
    /// <param name="directory">The directory to walk.</param>
    /// <param name="predicate">Whether a file is collected.</param>
    /// <param name="result">The collected files.</param>
    static void WalkInto(string directory, Func<string, bool> predicate, List<string> result)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        // Build output is generated, not authored. Scanning it reports the bundler's
        // own CSS as a source violation, which no consumer can fix. Native runtime
        // directories hold copied web assets rather than authored source.
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            if (IgnoredDirectories.Contains(entry.Name))
            {
                continue;
            }

            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                if (predicate(entry.FullName))
                {
                    result.Add(entry.FullName);
                }

                continue;
            }

            if ((entry.Attributes & FileAttributes.Directory) != 0)
            {
                WalkInto(entry.FullName, predicate, result);
            }
            else if (predicate(entry.FullName))
            {
                result.Add(entry.FullName);
            }
        }
    }
}
