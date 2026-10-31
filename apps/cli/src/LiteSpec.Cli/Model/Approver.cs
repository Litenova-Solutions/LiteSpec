using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Model;

/// <summary>
///     Writes approval data onto records: who approved or reviewed each one, when, and the
///     semantic hash of what they approved, so a later change inside it reopens the approval.
/// </summary>
/// <follows>litespec/core/model#record-approval-as-data</follows>
static class Approver
{
    static readonly JsonSerializerOptions Write = new()
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    ///     Computes the approval hash over the record's semantic frontmatter fields.
    ///     The hash covers every written field except identity, workflow and approval
    ///     data (<c>type</c>, <c>id</c>, <c>title</c>, <c>owner</c>, <c>status</c>,
    ///     <c>decider</c>, <c>decideBy</c>, <c>approvedBy</c>, <c>approvedOn</c>,
    ///     <c>approvedHash</c> and <c>provenance</c>), canonicalized and hashed to eight hex
    ///     characters. Page prose bodies and rendered consequences summaries never feed
    ///     the hash, so rewording prose reopens no approval. The semantic fields per
    ///     record type include: product (boundary, exclusions, addresses); goal
    ///     (statement, measure, target, dueBy); problem (affects, statement); feature
    ///     (lifecycle, refusedBecause, solves, advances, plannedFor); requirement (kind,
    ///     statement, rationale, values, measure, dueBy, cites, except, setBy, risk);
    ///     constraint (authority, kind, applies, appliesBecause); decision (options,
    ///     reopenedWhen, choice, decides); research (sources, question,
    ///     conclusion); topic (reviewBy); journey (actor, serves, cites, finishedWhen, steps); actor (kind); term (term, meaning, layer, aliases); module
    ///     (dependsOn); aggregate (kind, states, transitions, data, dataClasses, steps,
    ///     exits, finishedWhen, drivenBy, cursor, owner); rule (kind, statement, governs,
    ///     binds, refusal, releasedBy, frozenWhen, history, computedBy); use-case (kind,
    ///     serves, fulfills, access, reachedThrough, input, output, background, paging,
    ///     builtAsynchronously, changes, reads, consistency, effects, raises,
    ///     idempotency, audit, risk); result (outcome, code, meaning, recorded, codes);
    ///     scenario (given, when, then, result, examples, exercises); event (meaning, carries);
    ///     integration-event (direction, eventType, source, subject, payload, dataschema,
    ///     addresses, trust, correlation, translates); reaction (triggeredBy, skippedWhen,
    ///     effects, reads, changes, fulfills, serves, idempotency, onFailure, risk);
    ///     job (kind, schedule, selection, batchSize, lease, cursor, runbook, runsWhere,
    ///     guard, serviceLevel, evidence, effects, reads, changes, fulfills, serves,
    ///     idempotency, onFailure, risk); notification (template, addresses, channel, consent, frequency,
    ///     dedupKey, params, locale, suppressedWhen, claims, fulfills, sender); setting
    ///     (valueType, default, range, unit, setBy, atBound, secret, meaning, runbook,
    ///     fulfills); role (scope, allows); permission (meaning); entry-point (kind,
    ///     auth, path, contentType, cacheControl, versioning, fulfills, protocol);
    ///     external-system (actor, operations, erasure, acceptedBy); runbook (trigger,
    ///     steps); exception (kind, covers, subject, paths, reason, expiresOn);
    ///     plus questions, terms and replaces where a record carries them.
    /// </summary>
    public static string SemanticHash(Record record) => Checker.SemanticHash(record);

    /// <summary>
    ///     Plans an approval without writing: each target with no approvedHash, or whose
    ///     semantic fields no longer match it. Returns the record with its reason: either
    ///     <c>no approvedHash</c> or <c>approvedHash differs</c>.
    /// </summary>
    public static IReadOnlyList<(Record Record, string Reason)> DryRun(IReadOnlyList<Record> targets)
    {
        var planned = new List<(Record, string)>();

        foreach (var record in targets)
        {
            if (record.ApprovedHash is not { Length: > 0 })
            {
                planned.Add((record, "no approvedHash"));
            }
            else if (SemanticHash(record) != record.ApprovedHash)
            {
                planned.Add((record, "approvedHash differs"));
            }
        }

        return planned;
    }

    /// <summary>
    ///     Stamps each record: a draft takes its layer status, reviewed for behavior
    ///     and approved otherwise, and the approver, the date and the semantic hash
    ///     are always written, so re-approval carries the new approver and date.
    ///     Returns the records stamped and the ids no written record holds.
    /// </summary>
    public static (IReadOnlyList<Record> Stamped, IReadOnlyList<string> Missing) Approve(
        ProjectModel model, IReadOnlyList<Record> targets, string? by, string on)
    {
        var stamped = new List<Record>();
        var missing = new List<string>();

        foreach (var group in targets.GroupBy(record => record.File, StringComparer.OrdinalIgnoreCase))
        {
            var text = File.ReadAllText(group.Key);
            var json = group.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
            var (head, node, tail) = json ? ("", JsonNode.Parse(text), "") : SplitFrontmatter(text);

            if (node is null)
            {
                missing.AddRange(group.Select(record => record.Id));
                continue;
            }

            var pageBefore = (node as JsonObject)?.DeepClone() as JsonObject;

            foreach (var record in group)
            {
                if (Find(node, record.Id) is not JsonObject target)
                {
                    missing.Add(record.Id);
                    continue;
                }

                if (record.Status == "draft")
                {
                    target["status"] = ReviewQueue.LayerOf(record) == "behavior" ? "reviewed" : "approved";
                }

                target["approvedBy"] = by ?? record.Decider ?? record.Owner ?? "unknown";
                target["approvedOn"] = on;
                target["approvedHash"] = SemanticHash(record);
                stamped.Add(record);
            }

            KeepInheritedStatus(pageBefore, node as JsonObject, group.Select(record => record.Id).ToHashSet(StringComparer.Ordinal));

            var serialized = node.ToJsonString(Write);
            File.WriteAllText(group.Key, json ? serialized + "\n" : head + serialized + tail);
        }

        return (stamped, missing);
    }

    /// <summary>One batch stamp: the record with the status and approval it takes.</summary>
    /// <param name="Record">The record to stamp.</param>
    /// <param name="Status">The status to write: approved or reviewed.</param>
    /// <param name="By">The approver to write.</param>
    /// <param name="On">The approval date to write.</param>
    public sealed record BatchStamp(Record Record, string Status, string By, string On);

    /// <summary>
    ///     Stamps each record of a batch approval: the target status, the approver, the
    ///     date, and the semantic hash, always. Returns the records stamped.
    /// </summary>
    public static IReadOnlyList<Record> StampBatch(IReadOnlyList<BatchStamp> stamps)
    {
        var stamped = new List<Record>();

        foreach (var group in stamps.GroupBy(stamp => stamp.Record.File, StringComparer.OrdinalIgnoreCase))
        {
            var text = File.ReadAllText(group.Key);
            var json = group.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
            var (head, node, tail) = json ? ("", JsonNode.Parse(text), "") : SplitFrontmatter(text);

            if (node is null)
            {
                continue;
            }

            var pageBefore = (node as JsonObject)?.DeepClone() as JsonObject;

            foreach (var stamp in group)
            {
                if (Find(node, stamp.Record.Id) is not JsonObject target)
                {
                    continue;
                }

                target["status"] = stamp.Status;
                target["approvedBy"] = stamp.By;
                target["approvedOn"] = stamp.On;
                target["approvedHash"] = SemanticHash(stamp.Record);
                stamped.Add(stamp.Record);
            }

            KeepInheritedStatus(pageBefore, node as JsonObject, group.Select(stamp => stamp.Record.Id).ToHashSet(StringComparer.Ordinal));

            var serialized = node.ToJsonString(Write);
            File.WriteAllText(group.Key, json ? serialized + "\n" : head + serialized + tail);
        }

        return stamped;
    }

    /// <summary>
    ///     Keeps the status an entry inherited from its page when the page is stamped and the
    ///     entry is not: the entry gains the page's former status, decider and decide-by date
    ///     as its own, so a page approval never promotes an entry its batch did not hold.
    /// </summary>
    static void KeepInheritedStatus(JsonObject? before, JsonObject? page, HashSet<string> stamped)
    {
        if (before is null || page is null || page["records"] is not JsonArray entries)
        {
            return;
        }

        var status = before["status"]?.GetValue<string>();

        if (status is null || status == page["status"]?.GetValue<string>())
        {
            return;
        }

        foreach (var entry in entries.OfType<JsonObject>())
        {
            if (entry["status"] is not null
                || entry["id"]?.GetValue<string>() is not { } id
                || stamped.Contains(id))
            {
                continue;
            }

            entry["status"] = status;

            // A result carries no decider of its own: it is decided with its use case.
            if (entry["type"]?.GetValue<string>() == "result")
            {
                continue;
            }

            foreach (var name in new[] { "decider", "decideBy" })
            {
                if (entry[name] is null && before[name] is { } value)
                {
                    entry[name] = value.DeepClone();
                }
            }
        }
    }

    /// <summary>The object carrying the id: the page itself, an entry, or a nested entry.</summary>
    static JsonNode? Find(JsonNode node, string id)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["id"] is JsonValue value && value.TryGetValue<string>(out var own) && own == id)
                {
                    return obj;
                }

                foreach (var (_, child) in obj)
                {
                    if (child is not null && Find(child, id) is { } found)
                    {
                        return found;
                    }
                }

                return null;

            case JsonArray array:
                foreach (var child in array)
                {
                    if (child is not null && Find(child, id) is { } found)
                    {
                        return found;
                    }
                }

                return null;

            default:
                return null;
        }
    }

    /// <summary>Splits a page into the opening fence, its frontmatter and the rest.</summary>
    static (string Head, JsonNode? Node, string Tail) SplitFrontmatter(string text)
    {
        const string fence = "---\n";
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!normalized.StartsWith(fence, StringComparison.Ordinal))
        {
            return ("", null, "");
        }

        var end = normalized.IndexOf("\n---", fence.Length, StringComparison.Ordinal);

        if (end < 0)
        {
            return ("", null, "");
        }

        var body = normalized[fence.Length..end];
        return (fence, JsonNode.Parse(body), normalized[end..]);
    }
}
