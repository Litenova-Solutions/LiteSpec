using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

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

    /// <summary>
    ///     Directory names every walk of a consumer leaves out: build output, installed packages,
    ///     framework caches and test reports. Every walk reads this one list.
    /// </summary>
    public static readonly IReadOnlySet<string> SkippedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "out", "build", "dist", ".output", "coverage", "TestResults", "test-results",
        "playwright-report", "node_modules", ".git", ".svelte-kit", ".next", ".tanstack",
    };

    /// <summary>The one include that collects every file at any depth.</summary>
    private static readonly string[] AllFiles = ["**/*"];

    /// <summary>The glob excludes that leave out every skipped directory at any depth.</summary>
    private static readonly string[] SkippedExcludes = [.. SkippedFolders.Select(name => "**/" + name + "/**/*")];

    /// <summary>
    ///     Every file under a directory, recursively, in a stable order.
    /// </summary>
    /// <param name="directory">The directory to walk, recursively.</param>
    /// <returns>Every file under the directory in ordinal path order, or an empty list when the directory does not exist.</returns>
    public static IReadOnlyList<string> Files(string directory) =>
        Match(directory, AllFiles, SkippedExcludes);

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

    /// <summary>
    ///     Every file under a directory that matches one of the include patterns and none of the exclude patterns, in a stable order.
    /// </summary>
    /// <param name="directory">The directory to walk, recursively.</param>
    /// <param name="includes">The glob patterns a file matches to be collected.</param>
    /// <param name="excludes">The glob patterns that leave a file out, or null for none.</param>
    /// <param name="comparison">How patterns compare against file names.</param>
    /// <returns>Every matching file under the directory in ordinal path order, or an empty list when the directory does not exist.</returns>
    public static IReadOnlyList<string> Match(string directory, IEnumerable<string> includes, IEnumerable<string>? excludes = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var matcher = new Matcher(comparison);
        matcher.AddIncludePatterns(includes);

        if (excludes is not null)
        {
            matcher.AddExcludePatterns(excludes);
        }

        var found = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directory)))
            .Files
            .Select(match => Path.Combine(directory, match.Path.Replace('/', Path.DirectorySeparatorChar)))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        found.Sort(StringComparer.Ordinal);

        return found;
    }
}
