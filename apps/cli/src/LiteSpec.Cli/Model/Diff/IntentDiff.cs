using System.Security.Cryptography;
using System.Text;

namespace LiteSpec.Model.Diff;

/// <summary>The intent diff between two models: every change in review order.</summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
sealed class IntentDiff
{
    /// <summary>Initializes a new instance of the <see cref="IntentDiff"/> class.Creates the intent diff between two models.</summary>
    /// <param name="baseLabel">The base name, as the caller passed it.</param>
    /// <param name="headLabel">The head name, as the caller passed it.</param>
    /// <param name="changes">Every change, in review order.</param>
    /// <param name="securityQueue">The security queue, or null for none.</param>
    public IntentDiff(
        string baseLabel,
        string headLabel,
        IReadOnlyList<IntentChange> changes,
        IReadOnlyList<SecurityQueueEntry>? securityQueue = null)
    {
        BaseLabel = baseLabel;
        HeadLabel = headLabel;
        Changes = changes;
        SecurityQueue = securityQueue ?? [];
    }

    /// <summary>Gets the base name, as the caller passed it.</summary>
    public string BaseLabel { get; }

    /// <summary>Gets the head name: the branch, or the working tree.</summary>
    public string HeadLabel { get; }

    /// <summary>Gets every change: removed, then modified, then added.</summary>
    public IReadOnlyList<IntentChange> Changes { get; }

    /// <summary>Gets the security queue: tenant scope widenings, new outside calls, new personal data.</summary>
    public IReadOnlyList<SecurityQueueEntry> SecurityQueue { get; }

    /// <summary>The changes one view shows, in review order.</summary>
    /// <param name="view">The view to show.</param>
    /// <returns>Only the changes whose view matches, kept in review order.</returns>
    public IReadOnlyList<IntentChange> ChangesIn(DiffView view) =>
        view == DiffView.Both
            ? Changes
            : Changes.Where(change => change.View == "both" || change.View == ViewName(view)).ToList();

    /// <summary>How many changes in one view break approved intent.</summary>
    /// <param name="view">The view to count.</param>
    /// <returns>The number of entries in that view whose Breaking flag is set.</returns>
    public int BreakingCountIn(DiffView view) => ChangesIn(view).Count(change => change.Breaking);

    /// <summary>Gets how many changes break approved intent.</summary>
    public int BreakingCount => Changes.Count(change => change.Breaking);

    /// <summary>Gets how many records the head no longer holds.</summary>
    public int RemovedCount => Changes.Count(change => change.Change == ChangeKind.Removed);

    /// <summary>Gets how many records changed intent.</summary>
    public int ModifiedCount => Changes.Count(change => change.Change == ChangeKind.Modified);

    /// <summary>Gets how many records the base did not hold.</summary>
    public int AddedCount => Changes.Count(change => change.Change == ChangeKind.Added);

    /// <summary>The family a record type belongs to.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>Goals-requirements, domain, access, or operations, according to the record type.</returns>
    public static string FamilyOf(string type) => type switch
    {
        "product" or "goal" or "problem" or "feature" or "topic" or "constraint"
            or "research" or "decision" or "requirement" or "journey" => "goals-requirements",
        "module" or "aggregate" or "rule" or "event" or "integration-event" or "use-case"
            or "result" or "scenario" or "reaction" or "job" or "notification" or "setting" => "domain",
        "actor" or "role" or "permission" or "entry-point" or "external-system" => "access",
        _ => "operations",
    };

    /// <summary>The view name a <see cref="DiffView"/> filters on.</summary>
    /// <param name="view">The view.</param>
    /// <returns>The view name the view filters on.</returns>
    public static string ViewName(DiffView view) => view switch
    {
        DiffView.Intent => "intent",
        DiffView.Behavior => "behavior",
        _ => "both",
    };

    /// <summary>
    ///     The view showing a record type: Intent records, Behavior records, or both
    ///     for governance records, terms without a layer, and unknown types.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <returns>The view showing the record type.</returns>
    public static string ViewOf(Record record) => record.Type switch
    {
        "product" or "goal" or "problem" or "feature" or "topic" or "requirement"
            or "constraint" or "decision" or "research" or "journey" or "actor" => "intent",
        "module" or "aggregate" or "rule" or "event" or "integration-event" or "use-case"
            or "result" or "scenario" or "reaction" or "job" or "notification" or "setting"
            or "role" or "permission" or "entry-point" or "external-system" or "runbook" => "behavior",
        "term" => record.Field("layer") is "intent" or "behavior" ? record.Field("layer")! : "both",
        _ => "both",
    };

    /// <summary>The display name of a family.</summary>
    /// <param name="family">The family.</param>
    /// <returns>Goals and requirements, Domain, Access, or Operations.</returns>
    public static string FamilyName(string family) => family switch
    {
        "goals-requirements" => "Goals and requirements",
        "domain" => "Domain",
        "access" => "Access",
        _ => "Operations",
    };

    /// <summary>The review order of a family.</summary>
    /// <param name="family">The family.</param>
    /// <returns>Zero for goals-requirements, then domain, access, and operations in that order.</returns>
    public static int FamilyOrder(string family) => family switch
    {
        "goals-requirements" => 0,
        "domain" => 1,
        "access" => 2,
        _ => 3,
    };

    /// <summary>The display name of a record type, such as Use case.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>The display name of a record type.</returns>
    public static string TypeName(string type) =>
        type.Length == 0 ? type : char.ToUpperInvariant(type[0]) + type[1..].Replace('-', ' ');

    /// <summary>
    ///     The stable fingerprint of one change, so a pull request comment can be
    ///     amended rather than reposted while the change it describes stays the same.
    /// </summary>
    /// <param name="change">The change kind.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="reasonCodes">The breaking reason codes.</param>
    /// <param name="details">The detail lines.</param>
    /// <returns>The stable fingerprint of one change.</returns>
    public static string FingerprintOf(
        ChangeKind change, string id, IEnumerable<string> reasonCodes, IEnumerable<string> details)
    {
        var canonical = string.Join(
            "\n",
            new[] { change.ToString(), id }
                .Concat(reasonCodes.OrderBy(code => code, StringComparer.Ordinal))
                .Concat(details.OrderBy(line => line, StringComparer.Ordinal)));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));

        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
