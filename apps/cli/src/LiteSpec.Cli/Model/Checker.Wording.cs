using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>The wording check on Behavior statements.</summary>
static partial class Checker
{
    /// <summary>The vague words with the word-boundary pattern that finds each one.</summary>
    static readonly (string Word, Regex Pattern)[] VagueWords =
    [
        ("handle", new Regex(@"\bhandle\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("handles", new Regex(@"\bhandles\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("handled", new Regex(@"\bhandled\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("process", new Regex(@"\bprocess\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("processes", new Regex(@"\bprocesses\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("manage", new Regex(@"\bmanage\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("manages", new Regex(@"\bmanages\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("various", new Regex(@"\bvarious\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("etc.", new Regex(@"\betc\.", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("appropriate", new Regex(@"\bappropriate\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("appropriately", new Regex(@"\bappropriately\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("properly", new Regex(@"\bproperly\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("as needed", new Regex(@"\bas\s+needed\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("and so on", new Regex(@"\band\s+so\s+on\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
    ];

    /// <summary>Text in backticks, which holds code rather than prose.</summary>
    static readonly Regex BacktickSpan = new("`[^`]*`", RegexOptions.Compiled);

    /// <summary>Reports statement-vague-word.</summary>
    private static void CheckStatementWording(ProjectModel model, List<Finding> findings)
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
                    "statement-vague-word",
                    $"{label} '{record.Id}' {field} uses '{word}', which hides behavior.",
                    record.File,
                    record.Line,
                    SubjectOf(record)));
            }
        }
    }
}
