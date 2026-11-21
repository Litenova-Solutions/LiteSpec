using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Rules;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The project convention checks: a convention sits where <c>rules</c> reads it, its
///     <c>Applies to</c> is a form <c>rules</c> can match, and it reaches at least one file or record.
/// </summary>
/// <follows>litespec/core/agent#replace-only-declared-conventions</follows>
/// <follows>litespec/core/agent#scope-every-convention-to-files-it-governs</follows>
internal static class ConventionChecks
{
    /// <summary>The finding code for a convention outside <c>governance/conventions/</c>.</summary>
    internal const string FolderInvalidCode = "convention-folder-invalid";

    /// <summary>The finding code for an <c>Applies to</c> that is neither a glob nor the scope vocabulary.</summary>
    internal const string ScopeInvalidCode = "convention-scope-invalid";

    /// <summary>The finding code for an <c>Applies to</c> that matches no file in the project tree and no record.</summary>
    internal const string UnreachedCode = "convention-unreached";

    /// <summary>Reports convention-folder-invalid for each convention-shaped file outside the conventions folder.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunFolder(ProjectModel model, List<Finding> findings)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in model.Config.RecordRoots)
        {
            var home = Path.Combine(root, "governance", "conventions");

            foreach (var file in SourceTree.Files(root, ".md"))
            {
                if (!seen.Add(file) || UnderHome(file, home))
                {
                    continue;
                }

                string text;

                try
                {
                    text = SourceTree.Read(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (ConventionIdOf(text) is { } held)
                {
                    findings.Add(new Finding(
                        FolderInvalidCode,
                        $"Convention '{held.Id}' sits in '{PathText.Relative(model.Config.Root, file)}', but rules reads conventions from '{PathText.Relative(model.Config.Root, home)}'.",
                        file,
                        held.Line,
                        held.Id));
                }
            }
        }
    }

    /// <summary>Reports convention-scope-invalid for each convention whose <c>Applies to</c> matches nothing.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunScope(ProjectModel model, List<Finding> findings)
    {
        foreach (var convention in ConventionFiles.LoadFiles(model.Config))
        {
            if (ConventionFiles.IsScopeForm(convention.Entry.AppliesTo) || ConventionFiles.IsGlobForm(convention.Entry.AppliesTo))
            {
                continue;
            }

            findings.Add(convention.Entry.AppliesTo.Length == 0
                ? new Finding(
                    ScopeInvalidCode,
                    $"Convention '{convention.Entry.Id}' states no Applies to, so rules never returns it.",
                    convention.File,
                    convention.AppliesToLine,
                    convention.Entry.Id)
                : new Finding(
                    ScopeInvalidCode,
                    $"Convention '{convention.Entry.Id}' states Applies to '{convention.Entry.AppliesTo}', which is neither a path glob nor the scope vocabulary, so rules never returns it.",
                    convention.File,
                    convention.AppliesToLine,
                    convention.Entry.Id));
        }
    }

    /// <summary>Reports convention-unreached for each convention whose <c>Applies to</c> matches no file in the project tree and no record.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunUnreached(ProjectModel model, List<Finding> findings)
    {
        var conventions = ConventionFiles.LoadFiles(model.Config)
            .Where(convention => ConventionFiles.IsScopeForm(convention.Entry.AppliesTo)
                || ConventionFiles.IsGlobForm(convention.Entry.AppliesTo))
            .ToList();

        if (conventions.Count == 0)
        {
            return;
        }

        var targets = LoadTargets(model);

        foreach (var convention in conventions)
        {
            if (targets.Any(target => Reaches(convention.Entry.AppliesTo, model.Config, target)))
            {
                continue;
            }

            findings.Add(new Finding(
                UnreachedCode,
                $"Convention '{convention.Entry.Id}' states Applies to '{convention.Entry.AppliesTo}', which matches no file in the project tree and no record, so rules never returns it.",
                convention.File,
                convention.AppliesToLine,
                convention.Entry.Id));
        }
    }

    /// <summary>Every <c>rules</c> target in the project: each file and folder under the root, and each written record.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <returns>The resolved target of each file, folder and record.</returns>
    static List<PathContext> LoadTargets(ProjectModel model)
    {
        var config = model.Config;
        var backendStack = BackendStackOf(config);
        var targets = new List<PathContext>();

        foreach (var file in SourceTree.Files(config.Root))
        {
            if (PathContext.Resolve(config, backendStack, file) is { } target
                && (config.ReadsCode || !target.IsCodePath))
            {
                targets.Add(target);
            }
        }

        foreach (var folder in FoldersOf(config.Root))
        {
            if (PathContext.Resolve(config, backendStack, folder) is { } target
                && (config.ReadsCode || !target.IsCodePath))
            {
                targets.Add(target);
            }
        }

        foreach (var id in model.Records
            .Select(record => record.Id)
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal))
        {
            if (PathContext.Resolve(config, backendStack, id) is { IsRecord: true } target)
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    /// <summary>Whether the convention reaches the target through the matcher <c>rules</c> uses.</summary>
    /// <param name="appliesTo">The convention Applies to text in scope or glob form.</param>
    /// <param name="config">The consumer configuration.</param>
    /// <param name="target">The rules target.</param>
    /// <returns>True when the scope fields meet the target, or the glob meets one of its path spellings.</returns>
    static bool Reaches(string appliesTo, ConsumerConfig config, PathContext target)
    {
        if (ConventionFiles.IsScopeForm(appliesTo))
        {
            return ConventionFiles.MatchesScope(appliesTo, target.Stack, target.Concern, target.Artifact, target.Marked);
        }

        return target.GlobCandidates(config).Any(candidate => Glob.Matches(appliesTo, candidate));
    }

    /// <summary>The selected backend stack, read the way <c>rules</c> reads it.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The backend stack id, or empty when the project selects none.</returns>
    static string BackendStackOf(ConsumerConfig config)
    {
        try
        {
            if (LiteSpecHome.Root is { } home)
            {
                var backend = StackSelection.Stacks(LiteSpecHome.ReadManifest(home))
                    .FirstOrDefault(stack => stack.Kind == "backend" && config.Stacks.Contains(stack.Id, StringComparer.Ordinal));

                if (backend is not null)
                {
                    return backend.Id;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonTextException)
        {
            // Without a readable manifest the stack falls back to the declared selection below.
        }

        return config.Stacks.Contains("dotnet", StringComparer.Ordinal) ? "dotnet" : "";
    }

    /// <summary>Every folder under the root, leaving out the folders no source walk reads.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The folders in ordinal order.</returns>
    static List<string> FoldersOf(string root)
    {
        var folders = new List<string>();

        if (!Directory.Exists(root))
        {
            return folders;
        }

        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string[] children;

            try
            {
                children = Directory.GetDirectories(pending.Pop());
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var child in children.OrderBy(name => name, StringComparer.Ordinal))
            {
                if (SourceTree.SkippedFolders.Contains(Path.GetFileName(child)))
                {
                    continue;
                }

                folders.Add(child);
                pending.Push(child);
            }
        }

        folders.Sort(StringComparer.Ordinal);
        return folders;
    }

    /// <summary>Whether the file sits under the conventions folder.</summary>
    /// <param name="file">The full path of the file under test.</param>
    /// <param name="home">The conventions folder.</param>
    /// <returns>True when the file sits under the conventions folder; otherwise false.</returns>
    static bool UnderHome(string file, string home)
    {
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return file.StartsWith(home + Path.DirectorySeparatorChar, comparison);
    }

    /// <summary>The convention identifier of the file's first identifier line, or null when it names no convention.</summary>
    /// <param name="text">The file text.</param>
    /// <returns>The convention identifier with its 1-based line, or null.</returns>
    static (string Id, int Line)? ConventionIdOf(string text)
    {
        var lines = text.Split('\n');
        var fenced = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                continue;
            }

            if (fenced || !line.StartsWith("**", StringComparison.Ordinal) || !line.Contains(":**", StringComparison.Ordinal))
            {
                continue;
            }

            var end = line.IndexOf(":**", StringComparison.Ordinal);

            if (!line[2..end].Equals("ID", StringComparison.Ordinal))
            {
                continue;
            }

            var id = line[(end + 3)..].Trim().Trim('`').Trim();

            return ConventionIds.IsConventionId(id) ? (id, index + 1) : null;
        }

        return null;
    }
}
