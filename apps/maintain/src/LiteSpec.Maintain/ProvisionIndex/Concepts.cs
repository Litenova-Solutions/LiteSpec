using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LiteSpec.Maintain.Validate;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.ProvisionIndex;

/// <summary>
///     The concept table: each glossary entry names the page that owns the concept in its Home line, and
///     the table lists every concept with its home and the provisions that page holds, so a reader finds
///     the one authoritative page for a concept.
/// </summary>
/// <follows>litespec/core/model#render-generated-regions-from-the-model</follows>
static partial class Concepts
{
    /// <summary>The glossary whose entries name their home pages, relative to the repository root.</summary>
    public const string GlossaryPath = "docs/reference/glossary.md";

    /// <summary>The generated concept page, relative to the repository root.</summary>
    public const string ConceptsPath = "docs/reference/concepts.md";

    /// <summary>One glossary entry: its term, the home page its Home line names (empty when it names none), and its line.</summary>
    /// <param name="Term">The term, from the entry heading.</param>
    /// <param name="Home">The home page path, relative to the repository root, or empty.</param>
    /// <param name="Line">The line of the entry heading.</param>
    public sealed record Entry(string Term, string Home, int Line);

    /// <summary>Builds the generated concept page.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The page path and the expected page text.</returns>
    /// <exception cref="InvalidOperationException">An entry lacks a Home line or names a page that does not exist.</exception>
    public static (string Path, string Text) Output(string root) => (ConceptsPath, Build(root));

    /// <summary>Reports each glossary entry that lacks a Home line or names a home page that does not exist.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="add">The diagnostic sink, taking the relative path, line, code, and message.</param>
    public static void Check(string root, Action<string, int, string, string> add)
    {
        foreach (var entry in Read(root))
        {
            if (entry.Home.Length == 0)
            {
                add(GlossaryPath, entry.Line, MaintainerDiagnostics.Codes.GlossaryHomeMissing, $"glossary entry '{entry.Term}' has no Home line; name the page that owns the concept");
            }
            else if (!File.Exists(RepositoryWalk.JoinRoot(root, entry.Home)))
            {
                add(GlossaryPath, entry.Line, MaintainerDiagnostics.Codes.GlossaryHomeUnknown, $"Home of glossary entry '{entry.Term}' names '{entry.Home}', which is not a page");
            }
        }
    }

    /// <summary>Reads every glossary entry with its Home line.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The entries in glossary order; empty when the glossary is missing.</returns>
    public static List<Entry> Read(string root)
    {
        var file = RepositoryWalk.JoinRoot(root, GlossaryPath);
        var entries = new List<Entry>();

        if (!File.Exists(file))
        {
            return entries;
        }

        var lines = FileText.ReadText(file).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            if (!lines[index].StartsWith("### ", StringComparison.Ordinal))
            {
                continue;
            }

            var home = "";

            // An entry runs to the next heading of any level.
            for (var scan = index + 1; scan < lines.Length && !lines[scan].StartsWith('#'); scan++)
            {
                var match = HomePattern().Match(lines[scan]);

                if (match.Success)
                {
                    home = match.Groups[1].Value.Trim().Trim('`').Trim();
                    break;
                }
            }

            entries.Add(new Entry(lines[index][4..].Trim(), home, index + 1));
        }

        return entries;
    }

    /// <summary>Builds the concept page text from the glossary and the standards pages.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The page text, with a final newline.</returns>
    /// <exception cref="InvalidOperationException">An entry lacks a Home line or names a page that does not exist.</exception>
    static string Build(string root)
    {
        var entries = Read(root);
        var problems = new List<string>();

        foreach (var entry in entries)
        {
            if (entry.Home.Length == 0)
            {
                problems.Add($"glossary entry '{entry.Term}' has no Home line");
            }
            else if (!File.Exists(RepositoryWalk.JoinRoot(root, entry.Home)))
            {
                problems.Add($"Home of glossary entry '{entry.Term}' names '{entry.Home}', which is not a page");
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", problems) + $"\nFix the Home lines in {GlossaryPath}.");
        }

        var provisions = Provisions.Collect(root);
        var text = new StringBuilder();
        text.Append("# Concepts\n\n");
        text.Append("Each glossary concept is explained on one page, its home. This generated table names that page and the provisions it holds; other pages link to the home page instead of restating the rule.\n\n");
        text.Append("## Reference\n\n");
        text.Append("| Concept | Home | Provisions |\n|:---|:---|:---|\n");

        foreach (var entry in entries.OrderBy(entry => entry.Term, StringComparer.OrdinalIgnoreCase))
        {
            var ids = provisions.Where(row => row.Page == entry.Home).Select(row => row.Id).ToList();
            var listed = ids.Count == 0 ? "None" : string.Join(", ", ids.Select(id => $"`{id}`"));
            text.Append(CultureInfo.InvariantCulture, $"| {Cell(entry.Term)} | `{entry.Home}` | {Cell(listed)} |\n");
        }

        text.Append("\n## Notes\n\nThe `litespec-maintain provisions` command writes this page from the Home line of each glossary entry, and `litespec-maintain provisions --check` fails when it differs. Edit the glossary, not the page.\n");
        return text.ToString();
    }

    /// <summary>Escapes a table cell so a pipe in the text does not start a new column.</summary>
    /// <param name="value">The cell text.</param>
    /// <returns>The escaped cell text.</returns>
    static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    /// <summary>Gets the pattern that matches a Home line of a glossary entry, capturing its value.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^Home:\s*(.+?)\s*$")]
    private static partial Regex HomePattern();
}
