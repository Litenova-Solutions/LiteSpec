using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Rules;

/// <summary>
///     One retrievable provision: a LiteSpec provision from the carried release or a
///     project convention from the consumer's governance folder. A departure exception
///     is not an entry: it replaces the entry its covers names at print time.
/// </summary>
/// <param name="Id">The provision identifier, or the convention identifier.</param>
/// <param name="Title">The provision heading.</param>
/// <param name="Page">The owning page relative to the release root, or empty for a convention.</param>
/// <param name="Kind">The assertion class: rule, convention or pattern.</param>
/// <param name="Strength">The display strength: rule, default, advice or pattern.</param>
/// <param name="Layer">The owning page layer, or empty when the page names none.</param>
/// <param name="AppliesTo">The artifact the provision governs, or empty for a page-scope provision.</param>
/// <param name="Statement">The one-line assertion: the Statement, the Default, or the pattern Default.</param>
/// <param name="Rationale">The reason the provision exists, printed by --full.</param>
/// <param name="Example">What following the provision looks like, printed by --full.</param>
/// <param name="ReviewedFor">The verification question or check a reviewer answers.</param>
/// <param name="Source">Where the entry comes from: litespec or convention.</param>
/// <param name="Conformance">The conformance level the provision belongs to: records, traced or full, or empty when it names none.</param>
/// <param name="Mark">The verification mark: checkable when a tool checks the provision, guidance when a person judges it, or empty.</param>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed record ProvisionEntry(
    string Id,
    string Title,
    string Page,
    string Kind,
    string Strength,
    string Layer,
    string AppliesTo,
    string Statement,
    string Rationale,
    string Example,
    string ReviewedFor,
    string Source,
    string Conformance,
    string Mark)
{
    /// <summary>The source of a LiteSpec provision from the carried release.</summary>
    public const string LiteSpecSource = "litespec";

    /// <summary>The source of a project convention from the governance folder.</summary>
    public const string ConventionSource = "convention";

    /// <summary>The group headings in print order.</summary>
    public static readonly IReadOnlyList<string> GroupOrder = ["Required", "Defaults", "Advisory", "Patterns"];

    /// <summary>Gets the group heading the strength prints under.</summary>
    public string Group => Strength switch
    {
        "rule" => "Required",
        "default" => "Defaults",
        "advice" => "Advisory",
        _ => "Patterns",
    };

    /// <summary>Gets the conformance level the provision belongs to; full when it names none, as a provision without a level runs only at full.</summary>
    public ConformanceLevel Level
    {
        get
        {
            return ConformanceLevels.TryParse(Conformance, out var level) ? level : ConformanceLevel.Full;
        }
    }

    /// <summary>Gets a value indicating whether a tool checks the provision, so the build already enforces it.</summary>
    public bool IsChecked => string.Equals(Mark, "checkable", StringComparison.Ordinal);

    /// <summary>One backticked span in a verification line.</summary>
    static readonly Regex BacktickSpan = new("`([^`]+)`", RegexOptions.Compiled);

    /// <summary>An analyzer or style rule identifier, such as LS2014.</summary>
    static readonly Regex AnalyzerId = new(@"^(?:LS|SA|CS)\d{4}$", RegexOptions.Compiled);

    /// <summary>A validator diagnostic identifier, such as PROSE_NON_ASCII.</summary>
    static readonly Regex DiagnosticId = new(@"^[A-Z][A-Z0-9_]+$", RegexOptions.Compiled);

    /// <summary>Gets the finding codes and analyzer IDs the checking tool reports for the provision, in evidence order.</summary>
    public IReadOnlyList<string> CheckedCodes
    {
        get
        {
            var codes = new List<string>();

            foreach (Match match in BacktickSpan.Matches(ReviewedFor))
            {
                var token = match.Groups[1].Value.Trim();

                if ((AnalyzerId.IsMatch(token) || DiagnosticId.IsMatch(token) || FindingCodes.IsKnown(token))
                    && !codes.Contains(token, StringComparer.Ordinal))
                {
                    codes.Add(token);
                }
            }

            return codes;
        }
    }

    /// <summary>Gets the first backticked checking command in the verification line, or empty when it names none.</summary>
    public string CheckedCommand
    {
        get
        {
            var match = BacktickSpan.Match(ReviewedFor);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }
    }
}
