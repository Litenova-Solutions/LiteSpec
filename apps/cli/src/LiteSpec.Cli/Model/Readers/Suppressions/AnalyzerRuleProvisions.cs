using System.Text.RegularExpressions;

namespace LiteSpec.Model.Readers.Suppressions;

/// <summary>
///     The provision each LiteSpec analyzer rule enforces, read from the rule pages
///     of the carried release: every <c>docs/analyzers/rules/ls*.md</c> page states
///     the provision it enforces in its What the Rule Enforces section, and that
///     statement is the map. Empty when no LiteSpec release is found, or when its
///     rule pages cannot be read.
/// </summary>
/// <follows>litespec/core/departures#record-a-project-wide-lowering-as-a-departure</follows>
static class AnalyzerRuleProvisions
{
    /// <summary>The provision identifier stated in backticks on a rule page.</summary>
    private static readonly Regex ProvisionId = new(@"`(litespec/[^`]+)`", RegexOptions.Compiled);

    /// <summary>Reads the provision each analyzer rule enforces, by rule identifier such as LS1001.</summary>
    /// <returns>The provision identifier each rule enforces, by rule identifier; empty when no LiteSpec release or rule page is found.</returns>
    public static IReadOnlyDictionary<string, string> Read()
    {
        var provisions = new Dictionary<string, string>(StringComparer.Ordinal);
        var home = LiteSpecHome.Root;

        if (home is null)
        {
            return provisions;
        }

        var folder = Path.Combine(home, "docs", "analyzers", "rules");

        if (!Directory.Exists(folder))
        {
            return provisions;
        }

        List<string> files;

        try
        {
            files = Directory.GetFiles(folder, "ls*.md").ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return provisions;
        }

        files.Sort(StringComparer.Ordinal);

        foreach (var file in files)
        {
            string text;

            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var enforces = text.IndexOf("enforces", StringComparison.OrdinalIgnoreCase);

            if (enforces < 0)
            {
                continue;
            }

            var match = ProvisionId.Match(text, enforces);

            if (match.Success)
            {
                provisions[Path.GetFileNameWithoutExtension(file).ToUpperInvariant()] = match.Groups[1].Value;
            }
        }

        return provisions;
    }
}
