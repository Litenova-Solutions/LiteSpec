using LiteSpec.Cli.Model;

namespace LiteSpec.Cli.Audit.Semantic;

/// <summary>
///     The files changed since a git revision: the tracked files differing between the
///     revision and the working tree, plus untracked files. Every git call runs through
///     <see cref="Runner"/>, so the tests replace the process with canned output.
/// </summary>
/// <follows>litespec/core/agent#review-changed-use-cases-from-an-independent-packet</follows>
static class SemanticGit
{
    /// <summary>Gets or sets the runner for one git call: the folder and the arguments to the exit status, output and error. Null runs the installed git.</summary>
    internal static Func<string, string[], (int Status, string Stdout, string Error)>? Runner { get; set; }

    /// <summary>How long a git call may run before the read reports a timeout.</summary>
    static readonly TimeSpan GitTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Reads the changed absolute paths, minus the audit output the command writes
    ///     itself. Null with an error when git is unavailable, the root sits outside a
    ///     git repository, or the revision names nothing.
    /// </summary>
    /// <param name="root">The consumer root git runs in.</param>
    /// <param name="revision">The git revision to diff the working tree against.</param>
    /// <returns>The changed absolute paths, or a null set with the error that stopped the read.</returns>
    internal static (HashSet<string>? Files, string Error) ChangedFiles(string root, string revision)
    {
        // The path from the root up to the repository top, such as "../" or "". Git names the top by its
        // resolved path, which differs from the root's own form when the root runs through a symbolic
        // link, such as /var on macOS, so the top is reached from the root and keeps the root's form.
        var up = Run(root, "rev-parse", "--show-cdup");

        if (up.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(up.Error, "the root sits outside a git repository")}.");
        }

        var known = Run(root, "rev-parse", "--verify", "--quiet", revision + "^{commit}");

        if (known.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': the revision names no commit.");
        }

        var diff = Run(root, "diff", revision, "--name-only", "-z");

        if (diff.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(diff.Error, "git diff failed")}.");
        }

        var untracked = Run(root, "ls-files", "--others", "--exclude-standard", "-z");

        if (untracked.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(untracked.Error, "git ls-files failed")}.");
        }

        // git diff names a path from the repository top, and git ls-files from the folder it runs in, the root.
        var toplevel = Path.GetFullPath(Path.Combine(root, up.Stdout.Trim()));
        var files = AbsolutePaths(toplevel, diff.Stdout);

        files.UnionWith(AbsolutePaths(root, untracked.Stdout));
        files.RemoveWhere(path => IsAuditOutput(root, path));

        return (files, "");
    }

    /// <summary>Whether the path lies under the audit output the command writes.</summary>
    /// <param name="root">The consumer root the audit output sits under.</param>
    /// <param name="path">The changed path to test.</param>
    /// <returns>True when the path sits under the audit output folder.</returns>
    static bool IsAuditOutput(string root, string path)
    {
        var output = Path.GetFullPath(Path.Combine(root, ".litespec", "audit")) + Path.DirectorySeparatorChar;

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
    /// <param name="error">The git standard error text.</param>
    /// <param name="fallback">The detail used when git wrote no error.</param>
    /// <returns>The trimmed git error, or the fallback when it is empty.</returns>
    static string Detail(string error, string fallback) =>
        error.Trim().Length > 0 ? error.Trim() : fallback;

    /// <summary>Runs one git command in a folder through the runner, or through the installed git.</summary>
    /// <param name="workdir">The folder git runs in.</param>
    /// <param name="args">The git arguments, in order.</param>
    /// <returns>The exit status with the standard output and standard error text.</returns>
    static (int Status, string Stdout, string Error) Run(string workdir, params string[] args)
    {
        if (Runner is not null)
        {
            return Runner(workdir, args);
        }

        var result = ProcessRunner.Git(workdir, args);

        return (result.Status, result.Stdout, result.Error);
    }
}
