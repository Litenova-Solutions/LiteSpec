using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Model.Proof;

/// <summary>
///     Whether each recorded proof is fresh: its run commit is HEAD, or every path
///     changed between the run commit and the working tree is a record or documentation
///     path. Changed paths are read once per distinct run commit and cached.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class ProofFreshness
{
    /// <summary>The consumer root git runs in.</summary>
    readonly string root;

    /// <summary>The absolute documentation roots a record path sits under.</summary>
    readonly IReadOnlyList<string> docsRoots;

    /// <summary>Runs git with the arguments and captures the result.</summary>
    readonly Func<IReadOnlyList<string>, ProcessRunner.Result> git;

    /// <summary>The computed status per run commit.</summary>
    readonly Dictionary<string, ProofStatus> cache = new(StringComparer.Ordinal);

    /// <summary>The checked-out HEAD, read once; null until read, empty when git cannot read it.</summary>
    string? head;

    /// <summary>The repository top, read once; null until read, empty when git cannot read it.</summary>
    string? toplevel;

    /// <summary>Initializes a new instance of the <see cref="ProofFreshness" /> class for one consumer.</summary>
    /// <param name="config">The consumer configuration holding the project root and documentation roots.</param>
    public ProofFreshness(ConsumerConfig config)
        : this(config.Root, config.RecordRoots, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ProofFreshness" /> class with an explicit git runner, for tests.</summary>
    /// <param name="root">The consumer root git runs in.</param>
    /// <param name="docsRoots">The absolute documentation roots a record path sits under.</param>
    /// <param name="runner">Runs git with the arguments, or null to run the installed git.</param>
    internal ProofFreshness(string root, IReadOnlyList<string> docsRoots, Func<IReadOnlyList<string>, ProcessRunner.Result>? runner)
    {
        this.root = root;
        this.docsRoots = docsRoots;
        git = runner ?? (args => ProcessRunner.Git(root, [.. args]));
    }

    /// <summary>The computed status of the run commit, cached per distinct commit.</summary>
    /// <param name="runCommit">The run commit the ledger recorded, or empty when the run recorded none.</param>
    /// <returns>The proof status of the run commit.</returns>
    public ProofStatus StatusOf(string runCommit)
    {
        if (cache.TryGetValue(runCommit, out var known))
        {
            return known;
        }

        var status = Compute(runCommit);
        cache[runCommit] = status;

        return status;
    }

    /// <summary>Whether the absolute path is a record or documentation path: under a documentation root or a Markdown file.</summary>
    /// <param name="absolutePath">The absolute path to test.</param>
    /// <returns>True when the path sits under a documentation root or ends in .md.</returns>
    public bool IsRecordPath(string absolutePath)
    {
        if (absolutePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var docs in docsRoots)
        {
            var folder = docs.EndsWith(Path.DirectorySeparatorChar) ? docs : docs + Path.DirectorySeparatorChar;

            if (absolutePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The short commit the book shows after "last passed at": the first seven characters, or the whole commit when shorter.</summary>
    /// <param name="commit">The full commit identifier, or empty.</param>
    /// <returns>The short commit, or empty when the commit is empty.</returns>
    public static string ShortOf(string commit) =>
        commit.Length <= 7 ? commit : commit[..7];

    /// <summary>Computes the status of the run commit: fresh at HEAD or with only record paths changed, else stale with the reason.</summary>
    /// <param name="runCommit">The run commit the ledger recorded, or empty when the run recorded none.</param>
    /// <returns>The proof status of the run commit.</returns>
    ProofStatus Compute(string runCommit)
    {
        var head = Head();
        var shortCommit = ShortOf(runCommit);

        if (runCommit.Length == 0)
        {
            return new ProofStatus("", "", "stale", 0, head.Length == 0 ? "git is unavailable" : "the run recorded no commit");
        }

        if (head.Length == 0)
        {
            return new ProofStatus(runCommit, shortCommit, "stale", 0, "git is unavailable");
        }

        if (string.Equals(runCommit, head, StringComparison.Ordinal))
        {
            return new ProofStatus(runCommit, shortCommit, "fresh", 0, null);
        }

        if (!Known(runCommit))
        {
            return new ProofStatus(runCommit, shortCommit, "stale", 0, "run commit is unknown to the repository");
        }

        if (ChangedPaths(runCommit) is not { } changed)
        {
            return new ProofStatus(runCommit, shortCommit, "stale", 0, "git is unavailable");
        }

        if (changed.All(IsRecordPath))
        {
            return new ProofStatus(runCommit, shortCommit, "fresh", 0, null);
        }

        return new ProofStatus(runCommit, shortCommit, "stale", CommitsBehind(runCommit), "files outside the record and documentation paths changed");
    }

    /// <summary>The checked-out HEAD, read once; empty when git cannot read it.</summary>
    /// <returns>The full commit identifier, or empty.</returns>
    string Head()
    {
        if (head is null)
        {
            var result = git(["rev-parse", "HEAD"]);
            head = result.Status == 0 ? result.Stdout.Trim() : "";
        }

        return head;
    }

    /// <summary>Whether the repository knows the commit.</summary>
    /// <param name="commit">The commit identifier to test.</param>
    /// <returns>True when the commit names a commit in the repository.</returns>
    bool Known(string commit) =>
        git(["rev-parse", "--verify", "--quiet", commit + "^{commit}"]).Status == 0;

    /// <summary>The paths changed between the commit and the working tree: tracked differences plus untracked files, as absolute paths. Null when git cannot read them.</summary>
    /// <param name="commit">The run commit to diff the working tree against.</param>
    /// <returns>The changed absolute paths, or null.</returns>
    HashSet<string>? ChangedPaths(string commit)
    {
        if (TopLevel() is not { Length: > 0 } top)
        {
            return null;
        }

        var diff = git(["diff", commit, "--name-only", "-z"]);

        if (diff.Status != 0)
        {
            return null;
        }

        var untracked = git(["ls-files", "--others", "--exclude-standard", "-z"]);

        if (untracked.Status != 0)
        {
            return null;
        }

        // git diff names a path from the repository top, and git ls-files from the folder it runs in, the root.
        var files = AbsolutePaths(top, diff.Stdout);
        files.UnionWith(AbsolutePaths(root, untracked.Stdout));

        return files;
    }

    /// <summary>The repository top, read once; empty when git cannot read it.</summary>
    /// <returns>The absolute repository top, or empty.</returns>
    string TopLevel()
    {
        if (toplevel is null)
        {
            var up = git(["rev-parse", "--show-cdup"]);
            toplevel = up.Status == 0 ? Path.GetFullPath(Path.Combine(root, up.Stdout.Trim())) : "";
        }

        return toplevel;
    }

    /// <summary>How many commits HEAD sits ahead of the run commit, or 0 when the count cannot be read.</summary>
    /// <param name="commit">The run commit HEAD sits ahead of.</param>
    /// <returns>The commit count, or 0.</returns>
    int CommitsBehind(string commit)
    {
        var count = git(["rev-list", "--count", commit + "..HEAD"]);

        return count.Status == 0 && int.TryParse(count.Stdout.Trim(), out var behind) && behind >= 0 ? behind : 0;
    }

    /// <summary>The NUL-separated git paths as absolute paths under the folder.</summary>
    /// <param name="folder">The folder the paths are relative to.</param>
    /// <param name="output">The NUL-separated git output to split.</param>
    /// <returns>The absolute paths under the folder.</returns>
    static HashSet<string> AbsolutePaths(string folder, string output)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                paths.Add(Path.GetFullPath(Path.Combine(folder, entry)));
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }
        }

        return paths;
    }
}
