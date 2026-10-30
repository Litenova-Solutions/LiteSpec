using System.Globalization;
using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>One record's scenario evidence in the review queue.</summary>
/// <param name="Total">The scenarios counted for the record.</param>
/// <param name="Confirmed">The counted scenarios with a confirmed proof.</param>
/// <param name="Passing">The counted scenarios with a passing, current proof.</param>
/// <param name="Stale">The counted scenarios whose proof went stale.</param>
sealed record ReviewScenarios(int Total, int Confirmed, int Passing, int Stale);

/// <summary>One record in the review queue, with its evidence.</summary>
/// <param name="Record">The queued record.</param>
/// <param name="Layer">The record layer: intent, behavior, or governance.</param>
/// <param name="Module">The batch module: the behavior module, intent, or governance.</param>
/// <param name="Tier">The approval tier: 1 needs a named person, 3 is sampled.</param>
/// <param name="EffectiveStatus">The builder's effective status.</param>
/// <param name="Reopened">Whether the approval hash no longer matches.</param>
/// <param name="FullReRead">Whether a reopened record needs a full re-read.</param>
/// <param name="Awaiting">The drafts whose effective status is draft because of this record.</param>
/// <param name="OpenQuestions">The count of open questions on the record.</param>
/// <param name="Scenarios">The scenario evidence.</param>
/// <param name="Findings">The count of open findings about the record.</param>
sealed record ReviewEntry(
    Record Record,
    string Layer,
    string Module,
    int Tier,
    string EffectiveStatus,
    bool Reopened,
    bool FullReRead,
    IReadOnlyList<string> Awaiting,
    int OpenQuestions,
    ReviewScenarios Scenarios,
    int Findings);

/// <summary>One review batch: a use case, a reaction, a job, a decision, or one module and type.</summary>
/// <param name="Selector">The batch selector.</param>
/// <param name="Kind">The batch kind: use-case, reaction, job, decision, record (one tier 1 record on its own), or module-type.</param>
/// <param name="Tier">The batch tier: 1 when any record is tier 1, else 3.</param>
/// <param name="Deciders">The distinct deciders, sorted.</param>
/// <param name="DecideBy">The earliest decide-by date, or null.</param>
/// <param name="Awaiting">The count of distinct drafts awaiting the batch.</param>
/// <param name="Records">The queued records, anchor first, then in authority order.</param>
sealed record ReviewBatch(
    string Selector,
    string Kind,
    int Tier,
    IReadOnlyList<string> Deciders,
    string? DecideBy,
    int Awaiting,
    IReadOnlyList<ReviewEntry> Records);

/// <summary>
///     The review queue: every draft and every reopened record, grouped into the
///     bounded batches one approval covers, ordered so the records others wait on
///     come first.
/// </summary>
/// <follows>litespec/core/release#compute-the-review-queue-from-the-model</follows>
static class ReviewQueue
{
    /// <summary>The authority ladder for records that carry one.</summary>
    static readonly IReadOnlyList<string> Ladder =
    [
        "constraint", "product", "requirement", "rule", "use-case", "result", "scenario",
    ];

    /// <summary>Computes the review queue from the model, in review order.</summary>
    public static IReadOnlyList<ReviewBatch> Compute(ProjectModel model) =>
        Compute(model, FindingsBySubject(model));

    /// <summary>
    ///     Computes the review queue with open-finding counts already taken, so a run
    ///     approving many batches checks the model once rather than once per batch.
    /// </summary>
    public static IReadOnlyList<ReviewBatch> Compute(ProjectModel model, IReadOnlyDictionary<string, int> findings)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return ComputeBatches(
            model.Records,
            model.EffectiveStatus,
            model.Proves,
            model.Stale,
            findings,
            model.Config,
            today);
    }

    /// <summary>
    ///     Computes the review batches from the queue inputs, in review order: every
    ///     draft and every reopened record, grouped into the bounded batches one
    ///     approval covers. The model command passes the checker's open-finding counts;
    ///     the book passes the counts from the findings the model records.
    /// </summary>
    internal static IReadOnlyList<ReviewBatch> ComputeBatches(
        IReadOnlyList<Record> records,
        IReadOnlyDictionary<string, string> effectiveStatus,
        IReadOnlyList<ProvesLink> proves,
        IReadOnlySet<string> stale,
        IReadOnlyDictionary<string, int> findings,
        ConsumerConfig config,
        DateOnly today)
    {
        var queued = records
            .Where(record => !record.Derived && (record.Status == "draft" || IsReopened(record)))
            .ToList();
        var groups = Assign(records, queued, config.BehaviorDocs);
        var batches = groups
            .Select(group => Describe(records, effectiveStatus, proves, stale, group, findings, config, today))
            .ToList();

        batches.Sort(CompareBatches);

        return batches;
    }

    /// <summary>Keeps the batches holding a record that matches every given filter.</summary>
    public static IReadOnlyList<ReviewBatch> Filter(
        IReadOnlyList<ReviewBatch> batches, string? decider, int? tier, string? module, string? type) =>
        batches
            .Where(batch => batch.Records.Any(entry =>
                (decider is null || entry.Record.Decider == decider)
                && (tier is null || entry.Tier == tier)
                && (module is null || entry.Module == module)
                && (type is null || entry.Record.Type == type)))
            .ToList();

    /// <summary>Describes one record with its queue evidence.</summary>
    public static ReviewEntry Describe(
        ProjectModel model, Record record, IReadOnlyDictionary<string, int> findings, DateOnly today) =>
        Describe(model.Records, model.EffectiveStatus, model.Proves, model.Stale, record, findings, model.Config, today);

    /// <summary>Describes one record with its queue evidence, from the queue inputs.</summary>
    static ReviewEntry Describe(
        IReadOnlyList<Record> records,
        IReadOnlyDictionary<string, string> effectiveStatus,
        IReadOnlyList<ProvesLink> proves,
        IReadOnlySet<string> stale,
        Record record,
        IReadOnlyDictionary<string, int> findings,
        ConsumerConfig config,
        DateOnly today) =>
        new(
            record,
            LayerOf(record),
            ModuleOf(config.BehaviorDocs, record),
            TierOf(records, record),
            effectiveStatus.TryGetValue(record.Id, out var effective)
                ? effective
                : record.Status ?? "draft",
            IsReopened(record),
            IsFullReRead(record),
            AwaitingOf(records, record),
            OpenQuestionCount(record, config, today),
            ScenariosOf(records, proves, stale, record),
            findings.TryGetValue(record.Id, out var count) ? count : 0);

    /// <summary>Counts the open findings by subject record id.</summary>
    public static IReadOnlyDictionary<string, int> FindingsBySubject(ProjectModel model)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var (findings, _) = Checker.Check(model);

        foreach (var finding in findings)
        {
            if (finding.Subject is { Length: > 0 } subject)
            {
                counts[subject] = counts.TryGetValue(subject, out var count) ? count + 1 : 1;
            }
        }

        return counts;
    }

    /// <summary>The record layer: intent, behavior, or governance.</summary>
    public static string LayerOf(Record record)
    {
        // Terms span both layers, so each term names its own.
        if (record.Type == "term")
        {
            return record.Field("layer") == "behavior" ? "behavior" : "intent";
        }

        return RecordTypes.LayerByType.TryGetValue(record.Type, out var layer) ? layer : "intent";
    }

    /// <summary>
    ///     The batch module: the behavior module holding the record's page, governance
    ///     for governance records, and an Intent record's identifier home (intent when it has none).
    /// </summary>
    public static string ModuleOf(ProjectModel model, Record record) =>
        ModuleOf(model.Config.BehaviorDocs, record);

    /// <summary>The batch module, from the behavior docs root.</summary>
    static string ModuleOf(string behaviorDocs, Record record)
    {
        var modules = behaviorDocs + Path.DirectorySeparatorChar
            + "modules" + Path.DirectorySeparatorChar;
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (record.File.StartsWith(modules, comparison))
        {
            var rest = record.File[modules.Length..];
            var end = rest.IndexOf(Path.DirectorySeparatorChar);

            if (end > 0)
            {
                return rest[..end];
            }
        }

        if (LayerOf(record) == "governance")
        {
            return "governance";
        }

        // An Intent record batches with the records of its own home, so one open question
        // holds back only the records it can change.
        return Identifiers.TrySplit(record.Id, out _, out var home, out _) && home is { Length: > 0 }
            ? home
            : "intent";
    }

    /// <summary>
    ///     The approval tier: 1 when the record carries any risk value, else 3.
    ///     A result or a scenario takes its use case's tier.
    /// </summary>
    public static int TierOf(ProjectModel model, Record record) =>
        TierOf(model.Records, record);

    /// <summary>
    ///     The approval tier from the records: 1 when the record carries any risk
    ///     value, else 3. A result or a scenario takes its use case's tier.
    /// </summary>
    static int TierOf(IReadOnlyList<Record> records, Record record)
    {
        if (record.Type is "result" or "scenario"
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && UseCaseOf(records, home) is { } holder)
        {
            return HasRisk(holder) ? 1 : 3;
        }

        return HasRisk(record) ? 1 : 3;
    }

    /// <summary>The use case a result or scenario home names, when one holds it.</summary>
    static Record? UseCaseOf(IReadOnlyList<Record> records, string home)
    {
        // ProjectModel.ById keeps the first record per id; the scan below keeps the same one.
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (record.Id.Length == 0 || !seen.Add(record.Id))
            {
                continue;
            }

            if (record.Type == "use-case"
                && Identifiers.TrySplit(record.Id, out _, out _, out var name)
                && name == home)
            {
                return record;
            }
        }

        return null;
    }

    /// <summary>Whether the record carries any risk value, which makes it tier 1.</summary>
    public static bool HasRisk(Record record) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty("risk", out var risk)
        && ((risk.ValueKind == JsonValueKind.Array && risk.GetArrayLength() > 0)
            || (risk.ValueKind == JsonValueKind.String && risk.GetString() is { Length: > 0 }));

    /// <summary>
    ///     Whether the record is reopened: approved or reviewed, with an approval hash
    ///     present that no longer matches the semantic hash.
    /// </summary>
    public static bool IsReopened(Record record) =>
        record.Status is "approved" or "reviewed"
        && record.ApprovedHash is { Length: > 0 }
        && Checker.SemanticHash(record) != record.ApprovedHash;

    /// <summary>
    ///     Whether a reopened record needs a full re-read: it carries a non-empty risk,
    ///     an access object, or a values field, so the hash cannot say which part moved.
    /// </summary>
    public static bool IsFullReRead(Record record)
    {
        if (!IsReopened(record))
        {
            return false;
        }

        if (HasRisk(record))
        {
            return true;
        }

        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (record.Raw.TryGetProperty("access", out var access)
            && access.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return record.Raw.TryGetProperty("values", out var values) && Present(values);
    }

    /// <summary>Whether a values field carries anything.</summary>
    static bool Present(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => false,
        JsonValueKind.String => value.GetString() is { Length: > 0 },
        JsonValueKind.Array => value.GetArrayLength() > 0,
        JsonValueKind.Object => value.EnumerateObject().Any(),
        _ => true,
    };

    /// <summary>
    ///     The drafts whose effective status is draft because of this record: the drafts
    ///     a draft decision decides, a draft requirement they fulfill, or a draft journey
    ///     a transcribed record serves.
    /// </summary>
    public static IReadOnlyList<string> AwaitingOf(ProjectModel model, Record record) =>
        AwaitingOf(model.Records, record);

    /// <summary>
    ///     The drafts waiting on the record, from the records: the drafts a draft
    ///     decision decides, a draft requirement they fulfill, or a draft journey a
    ///     transcribed record serves.
    /// </summary>
    static IReadOnlyList<string> AwaitingOf(IReadOnlyList<Record> records, Record record)
    {
        if (record.Status != "draft")
        {
            return [];
        }

        var waiting = new List<string>();

        foreach (var draft in records)
        {
            if (draft.Derived || draft.Status != "draft" || draft.Id == record.Id || draft.Id.Length == 0)
            {
                continue;
            }

            if (record.Type == "decision" && record.Ids("decides").Contains(draft.Id, StringComparer.Ordinal))
            {
                waiting.Add(draft.Id);
            }
            else if (record.Type == "requirement" && draft.Ids("fulfills").Contains(record.Id, StringComparer.Ordinal))
            {
                waiting.Add(draft.Id);
            }
            else if (record.Type == "journey"
                && draft.Field("source") == "transcribed"
                && draft.Ids("serves").Any(target =>
                    target == record.Id || target.StartsWith(record.Id + "#", StringComparison.Ordinal)))
            {
                waiting.Add(draft.Id);
            }
        }

        waiting.Sort(StringComparer.Ordinal);

        return waiting;
    }

    /// <summary>Counts the record's open questions: entries that are not auto-approved.</summary>
    public static int OpenQuestionCount(Record record, ConsumerConfig config, DateOnly today)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty("questions", out var questions)
            || questions.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return questions.EnumerateArray().Count(entry => !AutoApproved(entry, record, config, today));
    }

    /// <summary>
    ///     The tier of a question entry: its own tier field, else tier 1 when the parent
    ///     record carries risk, else tier 3.
    /// </summary>
    static int QuestionTier(JsonElement element, Record parent)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("tier", out var tier))
        {
            if (tier.ValueKind == JsonValueKind.Number
                && tier.TryGetInt32(out var number)
                && number is 1 or 3)
            {
                return number;
            }

            if (tier.ValueKind == JsonValueKind.String
                && tier.GetString() is { } text)
            {
                var folded = text.Trim().ToLowerInvariant();

                if (folded is "1" or "tier-1" or "tier1")
                {
                    return 1;
                }

                if (folded is "3" or "tier-3" or "tier3")
                {
                    return 3;
                }
            }
        }

        return HasRisk(parent) ? 1 : 3;
    }

    /// <summary>Whether the question entry carries a recommended answer.</summary>
    static bool Recommended(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("recommended", out var value)
        && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
        && (value.ValueKind != JsonValueKind.String || value.GetString() is { Length: > 0 })
        && (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0);

    /// <summary>
    ///     Whether the question entry counts as auto-approved: tier 3 with a recommended
    ///     answer, past its decide-by date plus the veto window.
    /// </summary>
    static bool AutoApproved(JsonElement element, Record parent, ConsumerConfig config, DateOnly today) =>
        QuestionTier(element, parent) == 3
        && Recommended(element)
        && element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("decideBy", out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } text
        && ParseDay(text, out var decideBy)
        && today > decideBy.AddDays(config.VetoWindowDays);

    /// <summary>Parses an ISO day, or a timestamp read as its day.</summary>
    static bool ParseDay(string text, out DateOnly day)
    {
        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
        {
            return true;
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment))
        {
            day = DateOnly.FromDateTime(moment.UtcDateTime);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     The scenario evidence: a use case, reaction or job counts its scenarios,
    ///     a scenario counts itself, and every other record counts none.
    /// </summary>
    public static ReviewScenarios ScenariosOf(ProjectModel model, Record record) =>
        ScenariosOf(model.Records, model.Proves, model.Stale, record);

    /// <summary>
    ///     The scenario evidence from the queue inputs: a use case, reaction or job
    ///     counts its scenarios, a scenario counts itself, and every other record
    ///     counts none.
    /// </summary>
    static ReviewScenarios ScenariosOf(
        IReadOnlyList<Record> records,
        IReadOnlyList<ProvesLink> proves,
        IReadOnlySet<string> staleIds,
        Record record)
    {
        IReadOnlyList<Record> own = record.Type switch
        {
            "use-case" or "reaction" or "job" => OwnedScenarios(records, record),
            "scenario" => [record],
            _ => [],
        };

        var confirmed = 0;
        var passing = 0;
        var stale = 0;

        foreach (var scenario in own)
        {
            var links = proves.Where(link => link.Claim == scenario.Id).ToList();

            if (links.Any(link => link.Confirmed))
            {
                confirmed++;
            }

            if (staleIds.Contains(scenario.Id))
            {
                stale++;
            }
            else if (links.Any(link => link.Confirmed && link.Passing == true))
            {
                passing++;
            }
        }

        return new ReviewScenarios(own.Count, confirmed, passing, stale);
    }

    /// <summary>The written scenarios the holder owns: by home for a use case, by page for a reaction or job.</summary>
    static IReadOnlyList<Record> OwnedScenarios(IReadOnlyList<Record> records, Record holder)
    {
        if (holder.Type is "reaction" or "job")
        {
            return records
                .Where(scenario => !scenario.Derived
                    && scenario.Type == "scenario"
                    && scenario.PageId == holder.Id)
                .ToList();
        }

        if (!Identifiers.TrySplit(holder.Id, out _, out _, out var name))
        {
            return [];
        }

        return records
            .Where(scenario => !scenario.Derived
                && scenario.Type == "scenario"
                && Identifiers.TrySplit(scenario.Id, out _, out var home, out _)
                && home == name)
            .ToList();
    }

    /// <summary>The reaction or job whose page carries the scenario, when one holds it.</summary>
    static Record? PageHolder(IReadOnlyList<Record> records, Record scenario)
    {
        if (scenario.PageId is not { Length: > 0 } pageId)
        {
            return null;
        }

        // ProjectModel.ById keeps the first record per id; the scan below keeps the same one.
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (record.Id.Length == 0 || !seen.Add(record.Id))
            {
                continue;
            }

            if (record.Id == pageId && record.Type is "reaction" or "job")
            {
                return record;
            }
        }

        return null;
    }

    /// <summary>One assignment group: the anchor and the queued records it holds.</summary>
    internal sealed record Group(string Key, string Selector, string Kind, List<Record> Members);

    /// <summary>
    ///     Assigns every queued record to exactly one batch: a use case anchors the batch
    ///     holding it with its results and scenarios, a reaction or job anchors the batch
    ///     holding it with the scenarios on its page, a decision anchors the batch holding
    ///     it with the records it decides, and every other record falls into the batch of
    ///     its module and type.
    /// </summary>
    static IReadOnlyList<Group> Assign(
        IReadOnlyList<Record> records, IReadOnlyList<Record> queued, string behaviorDocs)
    {
        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);

        Group For(string key, string selector, string kind)
        {
            if (!groups.TryGetValue(key, out var group))
            {
                group = new Group(key, selector, kind, []);
                groups[key] = group;
            }

            return group;
        }

        foreach (var record in queued)
        {
            if (record.Type == "use-case")
            {
                For("use-case/" + record.Id, record.Id, "use-case").Members.Add(record);
            }
            else if (record.Type is "reaction" or "job")
            {
                For(record.Type + "/" + record.Id, record.Id, record.Type).Members.Add(record);
            }
            else if (record.Type == "scenario"
                && PageHolder(records, record) is { } page)
            {
                For(page.Type + "/" + page.Id, page.Id, page.Type).Members.Add(record);
            }
            else if (record.Type is "result" or "scenario"
                && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                && home is not null
                && UseCaseOf(records, home) is { } holder)
            {
                For("use-case/" + holder.Id, holder.Id, "use-case").Members.Add(record);
            }
            else if (record.Type == "decision")
            {
                For("decision/" + record.Id, "decision/" + record.Id, "decision").Members.Add(record);
            }
            else if (Deciding(records, queued, record) is { } decider)
            {
                For("decision/" + decider.Id, "decision/" + decider.Id, "decision").Members.Add(record);
            }
            else if (TierOf(records, record) == 1)
            {
                // A tier 1 record no use case, reaction, job or decision holds is read on its own:
                // a module batch is sampled, and a tier 1 record is never left to a sample.
                For("record/" + record.Id, record.Id, "record").Members.Add(record);
            }
            else
            {
                var module = ModuleOf(behaviorDocs, record);

                For("module-type/" + module + "/" + record.Type, module + "/" + record.Type, "module-type")
                    .Members.Add(record);
            }
        }

        return groups.Values.ToList();
    }

    /// <summary>
    ///     The decision whose batch holds the record: a queued decision deciding it when
    ///     one exists, else the deciding decision with the lowest id.
    /// </summary>
    static Record? Deciding(IReadOnlyList<Record> records, IReadOnlyList<Record> queued, Record record)
    {
        var queuedIds = queued.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);

        return records
            .Where(decision => !decision.Derived
                && decision.Type == "decision"
                && decision.Ids("decides").Contains(record.Id, StringComparer.Ordinal))
            .OrderBy(decision => queuedIds.Contains(decision.Id) ? 0 : 1)
            .ThenBy(decision => decision.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>Describes one group as a batch, with its records in approval order.</summary>
    static ReviewBatch Describe(
        IReadOnlyList<Record> records,
        IReadOnlyDictionary<string, string> effectiveStatus,
        IReadOnlyList<ProvesLink> proves,
        IReadOnlySet<string> stale,
        Group group,
        IReadOnlyDictionary<string, int> findings,
        ConsumerConfig config,
        DateOnly today)
    {
        var entries = group.Members
            .Select(record => Describe(records, effectiveStatus, proves, stale, record, findings, config, today))
            .ToList();

        entries.Sort((left, right) => CompareRecords(group, left, right));

        var deciders = entries
            .Select(entry => entry.Record.Decider)
            .OfType<string>()
            .Where(decider => decider.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(decider => decider, StringComparer.Ordinal)
            .ToList();
        var decideBy = entries
            .Select(entry => entry.Record.DecideBy)
            .OfType<string>()
            .Where(date => date.Length > 0)
            .OrderBy(date => date, StringComparer.Ordinal)
            .FirstOrDefault();
        var awaiting = entries
            .SelectMany(entry => entry.Awaiting)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return new ReviewBatch(
            group.Selector,
            group.Kind,
            entries.Any(entry => entry.Tier == 1) ? 1 : 3,
            deciders,
            decideBy,
            awaiting,
            entries);
    }

    /// <summary>
    ///     Orders records inside a batch: the anchor first, then in authority order with
    ///     Intent before Governance before Behavior, then by type, then by id.
    /// </summary>
    internal static int CompareRecords(Group group, ReviewEntry left, ReviewEntry right) =>
        CompareRecords(AnchorOf(group), left.Record, right.Record);

    /// <summary>Orders two records for approval: the anchor first, then authority order.</summary>
    public static int CompareRecords(string? anchor, Record left, Record right)
    {
        if (anchor is not null)
        {
            var leftAnchor = left.Id == anchor ? 0 : 1;
            var rightAnchor = right.Id == anchor ? 0 : 1;

            if (leftAnchor != rightAnchor)
            {
                return leftAnchor.CompareTo(rightAnchor);
            }
        }

        var layer = LayerRank(left).CompareTo(LayerRank(right));

        if (layer != 0)
        {
            return layer;
        }

        var ladder = LadderRank(left.Type).CompareTo(LadderRank(right.Type));

        if (ladder != 0)
        {
            return ladder;
        }

        var type = string.Compare(left.Type, right.Type, StringComparison.Ordinal);

        return type != 0 ? type : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    /// <summary>The batch anchor: the use case, reaction, job or decision id, else null.</summary>
    static string? AnchorOf(Group group) => group.Kind switch
    {
        "use-case" or "reaction" or "job" => group.Selector,
        "decision" => group.Selector["decision/".Length..],
        _ => null,
    };

    /// <summary>Intent before Governance before Behavior.</summary>
    static int LayerRank(Record record) => LayerOf(record) switch
    {
        "governance" => 1,
        "behavior" => 2,
        _ => 0,
    };

    /// <summary>The ladder position, after every ladder type when the type has none.</summary>
    static int LadderRank(string type)
    {
        var rank = Ladder
            .Select((name, index) => (name, index))
            .Where(pair => pair.name == type)
            .Select(pair => (int?)pair.index)
            .FirstOrDefault();

        return rank ?? Ladder.Count;
    }

    /// <summary>
    ///     Orders batches: holding a full re-read, then holding another reopened record,
    ///     then by awaiting count, most first, then in authority order, then by the
    ///     earliest decide-by, then by selector.
    /// </summary>
    static int CompareBatches(ReviewBatch left, ReviewBatch right)
    {
        var reRead = right.Records.Any(entry => entry.FullReRead)
            .CompareTo(left.Records.Any(entry => entry.FullReRead));

        if (reRead != 0)
        {
            return reRead;
        }

        var reopened = right.Records.Any(entry => entry.Reopened)
            .CompareTo(left.Records.Any(entry => entry.Reopened));

        if (reopened != 0)
        {
            return reopened;
        }

        var awaiting = right.Awaiting.CompareTo(left.Awaiting);

        if (awaiting != 0)
        {
            return awaiting;
        }

        var authority = AuthorityRank(left).CompareTo(AuthorityRank(right));

        if (authority != 0)
        {
            return authority;
        }

        var decideBy = string.Compare(
            left.DecideBy ?? "\uffff", right.DecideBy ?? "\uffff", StringComparison.Ordinal);

        return decideBy != 0
            ? decideBy
            : string.Compare(left.Selector, right.Selector, StringComparison.Ordinal);
    }

    /// <summary>The batch authority: the lowest layer rank of its records.</summary>
    static int AuthorityRank(ReviewBatch batch) =>
        batch.Records.Select(entry => LayerRank(entry.Record)).DefaultIfEmpty(0).Min();

    /// <summary>Writes the queue as one JSON object with a batches array.</summary>
    public static string Json(IReadOnlyList<ReviewBatch> batches) =>
        CliJson.Serialize(new
        {
            batches = batches.Select(batch => new
            {
                selector = batch.Selector,
                kind = batch.Kind,
                tier = batch.Tier,
                deciders = batch.Deciders,
                decideBy = batch.DecideBy,
                awaiting = batch.Awaiting,
                records = batch.Records.Select(entry => new
                {
                    id = entry.Record.Id,
                    type = entry.Record.Type,
                    layer = entry.Layer,
                    tier = entry.Tier,
                    decider = entry.Record.Decider,
                    decideBy = entry.Record.DecideBy,
                    status = entry.Record.Status,
                    effectiveStatus = entry.EffectiveStatus,
                    reopened = entry.Reopened,
                    fullReRead = entry.FullReRead,
                    awaiting = entry.Awaiting,
                    openQuestions = entry.OpenQuestions,
                    scenarios = new
                    {
                        total = entry.Scenarios.Total,
                        confirmed = entry.Scenarios.Confirmed,
                        passing = entry.Scenarios.Passing,
                        stale = entry.Scenarios.Stale,
                    },
                    findings = entry.Findings,
                }).ToList(),
            }).ToList(),
        });

    /// <summary>Prints one block per batch: a header line and one line per record.</summary>
    public static string Human(IReadOnlyList<ReviewBatch> batches)
    {
        if (batches.Count == 0)
        {
            return "No drafts or reopened records.";
        }

        var lines = new List<string>();

        foreach (var batch in batches)
        {
            var records = batch.Records.Count == 1 ? "1 record" : $"{batch.Records.Count} records";

            lines.Add($"{batch.Selector}  tier {batch.Tier}  {records}  awaiting {batch.Awaiting}");

            foreach (var entry in batch.Records)
            {
                lines.Add($"  {entry.Record.Id}  {entry.Record.Status}  {Flags(entry)}");
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>The record flags: reopened, full re-read, tier 1, and open questions.</summary>
    static string Flags(ReviewEntry entry)
    {
        var flags = new List<string>();

        if (entry.Reopened)
        {
            flags.Add("reopened");
        }

        if (entry.FullReRead)
        {
            flags.Add("fullReRead");
        }

        if (entry.Tier == 1)
        {
            flags.Add("tier1");
        }

        if (entry.OpenQuestions > 0)
        {
            flags.Add($"questions={entry.OpenQuestions}");
        }

        return flags.Count > 0 ? string.Join(' ', flags) : "-";
    }
}
