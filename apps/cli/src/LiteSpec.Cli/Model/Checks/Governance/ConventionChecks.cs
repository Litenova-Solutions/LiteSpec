using LiteSpec.Cli.Files;
using LiteSpec.Cli.Rules;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Governance;

/// <summary>
///     The project convention checks: a convention sits where <c>rules</c> reads it, and its
///     <c>Applies to</c> is a form <c>rules</c> can match.
/// </summary>
/// <follows>litespec/core/agent#replace-only-declared-conventions</follows>
internal static class ConventionChecks
{
    /// <summary>The finding code for a convention outside <c>governance/conventions/</c>.</summary>
    internal const string FolderInvalidCode = "convention-folder-invalid";

    /// <summary>The finding code for an <c>Applies to</c> that is neither a glob nor the scope vocabulary.</summary>
    internal const string ScopeInvalidCode = "convention-scope-invalid";

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
