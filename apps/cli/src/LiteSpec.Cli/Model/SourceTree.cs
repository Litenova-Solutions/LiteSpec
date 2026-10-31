namespace LiteSpec.Model;

/// <summary>
///     Every file under a directory, with build output and installed packages left out.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class SourceTree
{
    /// <summary>
    ///     The text of a file, read again after a short wait while another process holds it open.
    /// </summary>
    /// <param name="file">The path of the file to read.</param>
    /// <returns>The full text of the file.</returns>
    public static string Read(string file)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return File.ReadAllText(file);
            }
            catch (IOException) when (attempt < 10 && File.Exists(file))
            {
                Thread.Sleep(200 * attempt);
            }
        }
    }

    /// <summary>Directory names the walk leaves out: build output, installed packages and tool folders.</summary>
    private static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "TestResults", "node_modules", ".git", ".model",
        ".svelte-kit", "build", "dist", "coverage",
    };

    /// <summary>
    ///     Every file under a directory, recursively, in a stable order.
    /// </summary>
    /// <param name="directory">The directory to walk, recursively.</param>
    /// <returns>Every file under the directory in ordinal path order, or an empty list when the directory does not exist.</returns>
    public static IReadOnlyList<string> Files(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var found = new List<string>();

        Walk(directory, found);
        found.Sort(StringComparer.Ordinal);

        return found;
    }

    /// <summary>
    ///     Every file matching one of several extensions under a directory.
    /// </summary>
    /// <param name="directory">The directory to walk, recursively.</param>
    /// <param name="extensions">The file extensions to keep, matched without regard to case.</param>
    /// <returns>Every file under the directory whose name ends in one of the extensions, in ordinal path order.</returns>
    public static IReadOnlyList<string> Files(string directory, params string[] extensions) =>
    [
        .. Files(directory)
           .Where(file => extensions.Any(extension =>
                                             file.EndsWith(extension, StringComparison.OrdinalIgnoreCase))),
    ];

    /// <summary>Adds every file under the directory to the list, skipping build output and tool folders.</summary>
    /// <param name="directory">The directory to walk recursively.</param>
    /// <param name="found">The file paths collected so far, appended in place.</param>
    private static void Walk(string directory, List<string> found)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            found.Add(file);
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            if (Skipped.Contains(Path.GetFileName(child)))
            {
                continue;
            }

            Walk(child, found);
        }
    }
}
