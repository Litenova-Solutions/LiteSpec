using System.Diagnostics;
using System.Text;

namespace LiteSpec.Cli.Review;

/// <summary>
///     The files changed since a git revision: the tracked files differing between the
///     revision and the working tree, plus untracked files. The review commands read
///     their scope here, so they call no model and no network.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
static class ReviewGit
{
    /// <summary>How long a git call may run before the read reports a timeout.</summary>
    static readonly TimeSpan GitTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Reads the changed absolute paths, minus the folders the review commands
    ///     write themselves. Null with an error when git is unavailable, the root
    ///     sits outside a git repository, or the revision names nothing.
    /// </summary>
    /// <param name="root">The repository folder git runs in.</param>
    /// <param name="revision">The git revision to diff the working tree against.</param>
    /// <returns>The changed absolute paths, or a null set with the error that stopped the read.</returns>
    public static (HashSet<string>? Files, string Error) ChangedFiles(string root, string revision)
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
        files.RemoveWhere(path => IsToolOutput(root, path));

        return (files, "");
    }

    /// <summary>
    ///     Reads the unified diff of one file against the revision. An untracked file
    ///     has no git diff, so its content is listed as added lines instead.
    /// </summary>
    /// <param name="root">The repository folder git runs in.</param>
    /// <param name="revision">The git revision to diff against.</param>
    /// <param name="file">The absolute file path.</param>
    /// <returns>The diff text, the file content as added lines, or a one-line note when neither is available.</returns>
    public static string DiffFor(string root, string revision, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var diff = Run(root, "diff", revision, "--", relative);

        if (diff.Status == 0 && diff.Stdout.Trim().Length > 0)
        {
            return diff.Stdout;
        }

        try
        {
            if (!File.Exists(file))
            {
                return $"(removed since {revision}: {relative})";
            }

            var lines = File.ReadAllLines(file);
            var text = new StringBuilder();

            text.AppendLine("--- /dev/null");
            text.AppendLine($"+++ b/{relative}");

            foreach (var line in lines)
            {
                text.AppendLine("+" + line);
            }

            return text.ToString();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return $"(unreadable: {relative})";
        }
    }

    /// <summary>Whether the path lies under a folder the review commands write: review packets, render markers, or render hashes.</summary>
    /// <param name="root">The repository folder the review commands write under.</param>
    /// <param name="path">The changed path to test.</param>
    /// <returns>True when the path lies under a review output folder.</returns>
    public static bool IsToolOutput(string root, string path)
    {
        foreach (var folder in new[] { "review", ".cache", "render" })
        {
            var output = Path.GetFullPath(Path.Combine(root, ".litespec", folder)) + Path.DirectorySeparatorChar;

            if (path.StartsWith(output, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

    /// <summary>Runs one git command in a folder and captures what it writes.</summary>
    /// <param name="workdir">The folder git runs in.</param>
    /// <param name="args">The git arguments, in order.</param>
    /// <returns>The exit status with the standard output and standard error text.</returns>
    static (int Status, string Stdout, string Error) Run(string workdir, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workdir,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(start);

            if (process is null)
            {
                return (1, "", "git is not available.");
            }

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(GitTimeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the wait and the kill.
                }

                return (1, "", "git did not finish in time.");
            }

            return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return (1, "", "git is not available.");
        }
    }
}
