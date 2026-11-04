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
    string Source)
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
}
