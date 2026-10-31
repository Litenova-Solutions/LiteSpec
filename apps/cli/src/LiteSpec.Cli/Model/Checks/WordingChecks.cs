using System.Text.RegularExpressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The wording check on Behavior statements.</summary>
/// <follows>litespec/core/authoring#state-what-happens-in-every-behavior-statement</follows>
internal static class WordingChecks
{
    /// <summary>
    ///     The vague words with the pattern that finds each one as a whole word; a hyphen counts as part of
    ///     the word, so a vague word inside an identifier such as <c>manage-organizer-access</c> is a name, not prose.
    /// </summary>
    static readonly (string Word, Regex Pattern)[] VagueWords =
    [
        ("handle", new Regex(@"(?<![\w-])handle(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("handles", new Regex(@"(?<![\w-])handles(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("handled", new Regex(@"(?<![\w-])handled(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("process", new Regex(@"(?<![\w-])process(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("processes", new Regex(@"(?<![\w-])processes(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("manage", new Regex(@"(?<![\w-])manage(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("manages", new Regex(@"(?<![\w-])manages(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("various", new Regex(@"(?<![\w-])various(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("etc.", new Regex(@"(?<![\w-])etc\.", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("appropriate", new Regex(@"(?<![\w-])appropriate(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("appropriately", new Regex(@"(?<![\w-])appropriately(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("properly", new Regex(@"(?<![\w-])properly(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("as needed", new Regex(@"(?<![\w-])as\s+needed(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("and so on", new Regex(@"(?<![\w-])and\s+so\s+on(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
    ];

    /// <summary>Reports statement-word-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            switch (record.Type)
            {
                case "rule":
                    CheckWording(record, "statement", "Rule", findings);
                    break;

                case "scenario":
                    CheckWording(record, "given", "Scenario", findings);
                    CheckWording(record, "when", "Scenario", findings);
                    CheckWording(record, "then", "Scenario", findings);
                    break;

                case "result":
                    CheckWording(record, "meaning", "Result", findings);
                    break;
            }
        }
    }

    /// <summary>Reports each vague word the field uses outside backticks.</summary>
    /// <param name="record">The record whose field text is inspected for vague words.</param>
    /// <param name="field">The name of the field on the record that holds the prose under inspection.</param>
    /// <param name="label">The record kind shown at the start of the finding message.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWording(Record record, string field, string label, List<Finding> findings)
    {
        if (record.Field(field) is not { Length: > 0 } text)
        {
            return;
        }

        var prose = BacktickSpan.Replace(text, " ");

        foreach (var (word, pattern) in VagueWords)
        {
            if (pattern.IsMatch(prose))
            {
                findings.Add(new Finding(
                    "statement-word-invalid",
                    $"{label} '{record.Id}' {field} uses '{word}', which hides behavior.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }
}
