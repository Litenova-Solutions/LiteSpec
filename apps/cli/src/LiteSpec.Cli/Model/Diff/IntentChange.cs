namespace LiteSpec.Model.Diff;

/// <summary>One record's change: what moved, in intent words, with its breaking reasons.</summary>
/// <param name="Id">The record identifier.</param>
/// <param name="Title">The record title.</param>
/// <param name="Type">The record type.</param>
/// <param name="Family">The record family: goals-requirements, domain, access or operations.</param>
/// <param name="Qualifier">The parenthetical after the title, such as a kind or an outcome.</param>
/// <param name="Change">Whether the record was removed, modified or added.</param>
/// <param name="Breaking">Whether the change breaks approved intent.</param>
/// <param name="Reasons">The named breaking reasons, empty when the change keeps intent.</param>
/// <param name="Details">What changed, one intent-words line each.</param>
/// <param name="View">Which view shows the change: intent, behavior, or both.</param>
/// <param name="Fingerprint">The stable fingerprint of this change.</param>
sealed record IntentChange(
    string Id,
    string Title,
    string Type,
    string Family,
    string Qualifier,
    ChangeKind Change,
    bool Breaking,
    IReadOnlyList<BreakingReason> Reasons,
    IReadOnlyList<string> Details,
    string View = "both",
    string Fingerprint = "");
