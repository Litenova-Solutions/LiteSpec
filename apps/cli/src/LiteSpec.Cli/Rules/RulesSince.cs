using LiteSpec.Cli.Model;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The files changed since a revision: the tracked files differing between the revision
///     and the working tree, plus untracked files. The same git reading
///     <c>model check --since</c> uses.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class RulesSince
{
    /// <summary>
    ///     Reads the files changed since the revision. Null with an error when git is
    ///     unavailable, the root sits outside a git repository, or the revision names nothing.
    /// </summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="revision">The git revision to diff the working tree against.</param>
    /// <returns>The changed absolute paths, or a null set with the error that stopped it.</returns>
    public static (HashSet<string>? Files, string Error) ChangedFiles(string root, string revision)
    {
        // The path from the root up to the repository top, such as "../" or "". Git names the top by its
        // resolved path, which differs from the root's own form when the root runs through a symbolic
        // link, such as /var on macOS, so the top is reached from the root and keeps the root's form.
        var up = ProcessRunner.Git(root, "rev-parse", "--show-cdup");

        if (up.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(up, "the root sits outside a git repository")}.");
        }

        var known = ProcessRunner.Git(root, "rev-parse", "--verify", "--quiet", revision + "^{commit}");

        if (known.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': the revision names no commit.");
        }

        var diff = ProcessRunner.Git(root, "diff", revision, "--name-only", "-z");

        if (diff.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(diff, "git diff failed")}.");
        }

        var untracked = ProcessRunner.Git(root, "ls-files", "--others", "--exclude-standard", "-z");

        if (untracked.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(untracked, "git ls-files failed")}.");
        }

        // git diff names a path from the repository top, and git ls-files from the folder it runs in, the root.
        var toplevel = Path.GetFullPath(Path.Combine(root, up.Stdout.Trim()));
        var files = AbsolutePaths(toplevel, diff.Stdout);

        files.UnionWith(AbsolutePaths(root, untracked.Stdout));
        files.RemoveWhere(path => IsToolOutput(root, path));

        return (files, "");
    }

    /// <summary>
    ///     Whether the path lies under the <c>.litespec/model</c> folder the tool writes while it builds:
    ///     a file the check itself wrote is not a change anyone made, so it never widens the scope.
    /// </summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="path">The changed path to test.</param>
    /// <returns>True when the path lies under the tool output folder.</returns>
    static bool IsToolOutput(string root, string path)
    {
        var output = Path.GetFullPath(Path.Combine(root, ".litespec", "model")) + Path.DirectorySeparatorChar;

        return path.StartsWith(output, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The NUL-separated git paths as absolute paths under the repository top.</summary>
    /// <param name="toplevel">The repository top folder the paths are relative to.</param>
    /// <param name="output">The NUL-separated git output to split.</param>
    /// <returns>The absolute paths under the repository top.</returns>
    static HashSet<string> AbsolutePaths(string toplevel, string output)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                paths.Add(Path.GetFullPath(Path.Combine(toplevel, entry)));
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }
        }

        return paths;
    }

    /// <summary>The detail for an error message: the git error text, or the fallback.</summary>
    /// <param name="run">The finished git run.</param>
    /// <param name="fallback">The detail used when git wrote no error.</param>
    /// <returns>The trimmed git error, or the fallback when it is empty.</returns>
    static string Detail(ProcessRunner.Result run, string fallback) =>
        run.Error.Trim().Length > 0 ? run.Error.Trim() : fallback;
}
