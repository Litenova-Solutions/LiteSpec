using LiteSpec.Cli.Files;

namespace LiteSpec.Cli.Model.Ui;

/// <summary>
///     Walks the consumer's files for the UI checks: existence, containment,
///     and every file under a directory that matches, in a stable order.
/// </summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
/// <follows>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</follows>
/// <follows>litespec/frontend/web/ui#track-source-changes</follows>
static class FileWalk
{
    /// <summary>
    ///     The native app folders a web frontend may hold beside its source. The UI walk leaves
    ///     them out on top of the folders every walk skips; the code readers still read them.
    /// </summary>
    static readonly string[] NativeAppFolders = ["android", "ios"];

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
    public static List<string> Walk(string directory, Func<string, bool> predicate) =>
        SourceTree.Match(directory, ["**/*"], Excludes(), StringComparison.Ordinal)
            .Where(predicate)
            .ToList();

    /// <summary>
    ///     The glob excludes that leave out every ignored directory at any depth.
    ///     Build output is generated, not authored: scanning it reports the bundler's
    ///     own CSS as a source violation, which no consumer can fix. Native runtime
    ///     directories hold copied web assets rather than authored source.
    /// </summary>
    /// <returns>The exclude patterns.</returns>
    static string[] Excludes() =>
        [.. SourceTree.SkippedFolders.Concat(NativeAppFolders).SelectMany(name => new[] { "**/" + name, "**/" + name + "/**/*" })];
}
