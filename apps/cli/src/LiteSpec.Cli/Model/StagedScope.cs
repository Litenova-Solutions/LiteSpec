
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The git scope for <c>model check --since</c>: the records on changed
///     pages, plus their direct links in both directions and the records
///     changed code implements. A finding is in scope when its path changed
///     or its subject is.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class StagedScope
{
    /// <summary>One computed scope: its record ids, its changed paths and its file count.</summary>
    /// <param name="RecordIds">The changed records plus their direct links, by id.</param>
    /// <param name="Files">The changed absolute paths.</param>
    /// <param name="ChangedFiles">How many files changed.</param>
    public sealed record Scope(
        IReadOnlySet<string> RecordIds,
        IReadOnlySet<string> Files,
        int ChangedFiles);

    /// <summary>
    ///     Computes the scope from what changed since the revision: the tracked files
    ///     differing between the revision and the working tree, plus untracked files.
    ///     Null with an error when git is unavailable, the root sits outside a git
    ///     repository, or the revision names nothing.
    /// </summary>
    /// <param name="model">The built project model whose records are scoped.</param>
    /// <param name="revision">The git revision to diff the working tree against.</param>
    /// <returns>The computed scope, or a null scope with the error that stopped it.</returns>
    public static (Scope? Scope, string Error) Since(ProjectModel model, string revision)
    {
        // The path from the root up to the repository top, such as "../" or "". Git names the top by its
        // resolved path, which differs from the root's own form when the root runs through a symbolic
        // link, such as /var on macOS, so the top is reached from the root and keeps the root's form.
        var up = ProcessRunner.Git(model.Config.Root, "rev-parse", "--show-cdup");

        if (up.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(up, "the root sits outside a git repository")}.");
        }

        var known = ProcessRunner.Git(model.Config.Root, "rev-parse", "--verify", "--quiet", revision + "^{commit}");

        if (known.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': the revision names no commit.");
        }

        var diff = ProcessRunner.Git(model.Config.Root, "diff", revision, "--name-only", "-z");

        if (diff.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(diff, "git diff failed")}.");
        }

        var untracked = ProcessRunner.Git(model.Config.Root, "ls-files", "--others", "--exclude-standard", "-z");

        if (untracked.Status != 0)
        {
            return (null, $"Cannot read changes since '{revision}': {Detail(untracked, "git ls-files failed")}.");
        }

        // git diff names a path from the repository top, and git ls-files from the folder it runs in, the root.
        var toplevel = Path.GetFullPath(Path.Combine(model.Config.Root, up.Stdout.Trim()));
        var files = AbsolutePaths(toplevel, diff.Stdout);

        files.UnionWith(AbsolutePaths(model.Config.Root, untracked.Stdout));
        files.RemoveWhere(path => IsToolOutput(model, path));

        return (BuildScope(model, files), "");
    }

    /// <summary>
    ///     Whether the path lies under the <c>.litespec/model</c> folder the tool writes while it builds,
    ///     such as the frontend reader's fact cache: a file the check itself wrote is not a change
    ///     anyone made, so it never widens the scope.
    /// </summary>
    /// <param name="model">The built project model holding the project root.</param>
    /// <param name="path">The changed path to test.</param>
    /// <returns>True when the path lies under the tool output folder.</returns>
    static bool IsToolOutput(ProjectModel model, string path)
    {
        var output = Path.GetFullPath(Path.Combine(model.Config.Root, ".litespec", "model")) + Path.DirectorySeparatorChar;

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

    /// <summary>Builds the scope from the changed absolute paths.</summary>
    /// <param name="model">The built project model whose records are scoped.</param>
    /// <param name="changed">The changed absolute paths.</param>
    /// <returns>The scope holding the changed records, their direct links and the changed paths.</returns>
    static Scope BuildScope(ProjectModel model, HashSet<string> changed)
    {
        var touched = model.Records
            .Where(record => record.Id.Length > 0 && changed.Contains(record.File))
            .Select(record => record.Id)
            .ToHashSet(StringComparer.Ordinal);

        var scope = new HashSet<string>(touched, StringComparer.Ordinal);
        var links = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Id.Length == 0)
            {
                continue;
            }

            var targets = ModelBuilder.ResolvedLinks(model, record).Values
                .SelectMany(target => target)
                .Select(Identifiers.BaseId)
                .ToHashSet(StringComparer.Ordinal);

            links[record.Id] = targets;
        }

        foreach (var id in touched)
        {
            if (links.TryGetValue(id, out var targets))
            {
                scope.UnionWith(targets);
            }
        }

        foreach (var (id, targets) in links)
        {
            if (targets.Overlaps(touched))
            {
                scope.Add(id);
            }
        }

        var relative = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in changed)
        {
            try
            {
                relative.Add(PathText.Relative(model.Config.Root, path));
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }
        }

        // Changed code pulls its records into scope through the implements links.
        foreach (var link in model.Implements)
        {
            if (relative.Contains(link.Path))
            {
                scope.Add(link.To);
            }
        }

        return new Scope(scope, changed, changed.Count);
    }

    /// <summary>Whether the finding touches the scope: a changed path or a scoped subject.</summary>
    /// <param name="scope">The computed scope to test against.</param>
    /// <param name="finding">The finding to test.</param>
    /// <returns>True when the finding path changed or its subject is in scope.</returns>
    public static bool InScope(Scope scope, Finding finding) =>
        scope.Files.Contains(finding.Path)
        || (finding.Subject is not null && scope.RecordIds.Contains(Identifiers.BaseId(finding.Subject)));

    /// <summary>The detail for an error message: the git error text, or the fallback.</summary>
    /// <param name="run">The finished git run.</param>
    /// <param name="fallback">The detail used when git wrote no error.</param>
    /// <returns>The trimmed git error, or the fallback when it is empty.</returns>
    static string Detail(ProcessRunner.Result run, string fallback) =>
        run.Error.Trim().Length > 0 ? run.Error.Trim() : fallback;
}
