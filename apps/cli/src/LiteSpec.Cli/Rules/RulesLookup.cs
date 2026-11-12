using System.Text.RegularExpressions;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     Lookup of one provision or page by its identifier: a provision prints in full, a page
///     prints its provision list. Both read the carried provisions file, so they work offline.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class RulesLookup
{
    /// <summary>The pattern a page identifier matches: litespec plus its page path.</summary>
    static readonly Regex PageIdPattern = new(@"^litespec\/[a-z0-9]+(?:-[a-z0-9]+)*(?:\/[a-z0-9]+(?:-[a-z0-9]+)*)*$");

    /// <summary>Whether the target is a page identifier, such as litespec/backend/application.</summary>
    /// <param name="target">The rules target.</param>
    /// <returns>True when the target names a page rather than a provision, path or record.</returns>
    public static bool IsPageId(string target) => PageIdPattern.IsMatch(target.Trim());

    /// <summary>The repository-relative page path a page identifier names.</summary>
    /// <param name="pageId">The page identifier.</param>
    /// <returns>The page path, such as docs/backend/application.md.</returns>
    public static string PagePathOf(string pageId) => "docs/" + pageId.Trim()["litespec/".Length..] + ".md";

    /// <summary>Prints one provision in full: its page, strength, scope, statement, rationale, example and verification.</summary>
    /// <param name="entry">The provision.</param>
    public static void PrintProvision(ProvisionEntry entry)
    {
        CliHost.Out.WriteLine($"Rules for {entry.Id}");
        CliHost.Out.WriteLine($"Page: {entry.Page}");
        CliHost.Out.WriteLine($"Strength: {entry.Strength}");

        if (entry.AppliesTo.Length > 0)
        {
            CliHost.Out.WriteLine($"Applies to: {entry.AppliesTo}");
        }

        if (entry.HasWhen)
        {
            CliHost.Out.WriteLine($"When: {entry.When}");
        }

        CliHost.Out.WriteLine($"Statement: {entry.Statement}");

        if (entry.Rationale.Length > 0)
        {
            CliHost.Out.WriteLine($"Rationale: {entry.Rationale}");
        }

        if (entry.Example.Length > 0)
        {
            CliHost.Out.WriteLine($"Example: {entry.Example}");
        }

        CliHost.Out.WriteLine(entry.IsChecked ? $"Checked by: {entry.ReviewedFor}" : $"Reviewed for: {entry.ReviewedFor}");
    }

    /// <summary>Prints a page's provision list in identifier order.</summary>
    /// <param name="pageId">The page identifier.</param>
    /// <param name="entries">The page provisions in identifier order.</param>
    public static void PrintPage(string pageId, IReadOnlyList<ProvisionEntry> entries)
    {
        CliHost.Out.WriteLine($"Rules for {pageId} ({entries.Count} provisions):");

        foreach (var entry in entries)
        {
            CliHost.Out.WriteLine($"- {entry.Id}: {entry.Statement}");
        }
    }
}
