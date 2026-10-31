using System.Globalization;
using System.Text.Json;

namespace LiteSpec.Model;
















































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
        RecordTypes.Constraint, RecordTypes.Product, RecordTypes.Requirement, RecordTypes.Rule, RecordTypes.UseCase, RecordTypes.Result, RecordTypes.Scenario,
    ];

    /// <summary>Computes the review queue from the model, in review order.</summary>
    /// <param name="model">The built project model to compute the queue from.</param>
    /// <returns>The review batches in review order.</returns>
    public static IReadOnlyList<ReviewBatch> Compute(ProjectModel model) =>
        Compute(model, FindingsBySubject(model));

    /// <summary>
    ///     Computes the review queue with open-finding counts already taken, so a run
    ///     approving many batches checks the model once rather than once per batch.
    /// </summary>
    /// <param name="model">The built project model to compute the queue from.</param>
    /// <param name="findings">The open finding count per record, already taken.</param>
    /// <returns>The review batches in review order, computed with the given finding counts.</returns>
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
    /// <param name="records">Every written record, derived records included.</param>
    /// <param name="effectiveStatus">The effective status of each record, by id.</param>
    /// <param name="proves">The computed proves links.</param>
    /// <param name="stale">The ids of scenarios whose proof is stale.</param>
    /// <param name="findings">The open finding count per record.</param>
    /// <param name="config">The consumer configuration of the project.</param>
    /// <param name="today">The current date, for open-question expiry.</param>
    /// <returns>The review batches in review order.</returns>
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
            .Where(record => !record.Derived && (record.Status == RecordStatuses.Draft || IsReopened(record)))
            .ToList();
        var groups = Assign(records, queued, config.BehaviorDocs);
        var batches = groups
            .Select(group => Describe(records, effectiveStatus, proves, stale, group, findings, config, today))
            .ToList();

        batches.Sort(CompareBatches);

        return batches;
    }

    /// <summary>Keeps the batches holding a record that matches every given filter.</summary>
    /// <param name="batches">The batches to filter.</param>
    /// <param name="decider">The decider to keep, or null for any.</param>
    /// <param name="tier">The tier to keep, or null for any.</param>
    /// <param name="module">The batch module to keep, or null for any.</param>
    /// <param name="type">The record type to keep, or null for any.</param>
    /// <returns>The batches holding a record that matches every given filter.</returns>
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
    /// <param name="model">The built project model the record belongs to.</param>
    /// <param name="record">The record to describe.</param>
    /// <param name="findings">The open finding count per record.</param>
    /// <param name="today">The current date, for open-question expiry.</param>
    /// <returns>The queue entry for the record with its evidence.</returns>
    public static ReviewEntry Describe(
        ProjectModel model, Record record, IReadOnlyDictionary<string, int> findings, DateOnly today) =>
        Describe(model.Records, model.EffectiveStatus, model.Proves, model.Stale, record, findings, model.Config, today);

    /// <summary>Describes one record with its queue evidence, from the queue inputs.</summary>
    /// <param name="records">Every written record, derived records included.</param>
    /// <param name="effectiveStatus">The effective status of each record, by id.</param>
    /// <param name="proves">The computed proves links.</param>
    /// <param name="stale">The ids of scenarios whose proof is stale.</param>
    /// <param name="record">The record to describe.</param>
    /// <param name="findings">The open finding count per record.</param>
    /// <param name="config">The consumer configuration of the project.</param>
    /// <param name="today">The current date, for open-question expiry.</param>
    /// <returns>The queue entry built from the given queue inputs.</returns>
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
                : record.Status ?? RecordStatuses.Draft,
            IsReopened(record),
            IsFullReRead(record),
            AwaitingOf(records, record),
            OpenQuestionCount(record, config, today),
            ScenariosOf(records, proves, stale, record),
            findings.TryGetValue(record.Id, out var count) ? count : 0);

    /// <summary>Counts the open findings by subject record id.</summary>
    /// <param name="model">The built project model to check.</param>
    /// <returns>The open finding count for each subject record id.</returns>
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
    /// <param name="record">The record whose layer is wanted.</param>
    /// <returns>Intent, behavior or governance, as the record type or its own layer field names it.</returns>
    public static string LayerOf(Record record)
    {
        // Terms span both layers, so each term names its own.
        if (record.Type == RecordTypes.Term)
        {
            return record.Field(RecordFields.Layer) == "behavior" ? "behavior" : "intent";
        }

        return RecordTypes.LayerByType.TryGetValue(record.Type, out var layer) ? layer : "intent";
    }

    /// <summary>
    ///     The batch module: the behavior module holding the record's page, governance
    ///     for governance records, and an Intent record's identifier home (intent when it has none).
    /// </summary>
    /// <param name="model">The built project model holding the behavior docs root.</param>
    /// <param name="record">The record whose batch module is wanted.</param>
    /// <returns>The batch module of the record.</returns>
    public static string ModuleOf(ProjectModel model, Record record) =>
        ModuleOf(model.Config.BehaviorDocs, record);

    /// <summary>The batch module, from the behavior docs root.</summary>
    /// <param name="behaviorDocs">The behavior docs root the module folders sit under.</param>
    /// <param name="record">The record whose batch module is wanted.</param>
    /// <returns>The module folder name, governance, the record home, or intent.</returns>
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
        // holds back only the records it can change. A journey has no identifier home, so it
        // batches with the goal it serves, its parent.
        if (record.Type == RecordTypes.Journey
            && record.Ids(RecordFields.Serves) is { Count: > 0 } serves && serves[0] is { } goal
            && Identifiers.TrySplit(goal, out var prefix, out _, out var goalName)
            && prefix == RecordTypes.Goal)
        {
            return goalName;
        }

        return Identifiers.TrySplit(record.Id, out _, out var home, out _) && home is { Length: > 0 }
            ? home
            : "intent";
    }

    /// <summary>
    ///     The approval tier: 1 when the record carries any risk value, else 3.
    ///     A result or a scenario takes its use case's tier.
    /// </summary>
    /// <param name="model">The built project model the record belongs to.</param>
    /// <param name="record">The record whose tier is wanted.</param>
    /// <returns>1 when the record or its holding use case carries risk, otherwise 3.</returns>
    public static int TierOf(ProjectModel model, Record record) =>
        TierOf(model.Records, record);

    /// <summary>
    ///     The approval tier from the records: 1 when the record carries any risk
    ///     value, else 3. A result or a scenario takes its use case's tier.
    /// </summary>
    /// <param name="records">Every written record, for the holding use case lookup.</param>
    /// <param name="record">The record whose tier is wanted.</param>
    /// <returns>1 when the record or the use case holding it carries risk, otherwise 3.</returns>
    static int TierOf(IReadOnlyList<Record> records, Record record)
    {
        if (record.Type is RecordTypes.Result or RecordTypes.Scenario
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && UseCaseOf(records, home) is { } holder)
        {
            return HasRisk(holder) ? 1 : 3;
        }

        return HasRisk(record) ? 1 : 3;
    }

    /// <summary>The use case a result or scenario home names, when one holds it.</summary>
    /// <param name="records">Every written record to scan.</param>
    /// <param name="home">The use-case name the home segment carries.</param>
    /// <returns>The first use case carrying the name, or null when none does.</returns>
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

            if (record.Type == RecordTypes.UseCase
                && Identifiers.TrySplit(record.Id, out _, out _, out var name)
                && name == home)
            {
                return record;
            }
        }

        return null;
    }

    /// <summary>Whether the record carries any risk value, which makes it tier 1.</summary>
    /// <param name="record">The record to test.</param>
    /// <returns>True when the record carries a non-empty risk value.</returns>
    public static bool HasRisk(Record record) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(RecordFields.Risk, out var risk)
        && ((risk.ValueKind == JsonValueKind.Array && risk.GetArrayLength() > 0)
            || (risk.ValueKind == JsonValueKind.String && risk.GetString() is { Length: > 0 }));

    /// <summary>
    ///     Whether the record is reopened: approved or reviewed, with an approval hash
    ///     present that no longer matches the semantic hash.
    /// </summary>
    /// <param name="record">The record to test.</param>
    /// <returns>True when the record is approved or reviewed and its hash no longer matches.</returns>
    public static bool IsReopened(Record record) =>
        record.Status is RecordStatuses.Approved or RecordStatuses.Reviewed
        && record.ApprovedHash is { Length: > 0 }
        && Checker.SemanticHash(record) != record.ApprovedHash;

    /// <summary>
    ///     Whether a reopened record needs a full re-read: it carries a non-empty risk,
    ///     an access object, or a values field, so the hash cannot say which part moved.
    /// </summary>
    /// <param name="record">The record to test.</param>
    /// <returns>True when a reopened record carries risk, access or values a hash cannot locate.</returns>
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

        if (record.Raw.TryGetProperty(RecordFields.Access, out var access)
            && access.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return record.Raw.TryGetProperty(RecordFields.Values, out var values) && Present(values);
    }

    /// <summary>Whether a values field carries anything.</summary>
    /// <param name="value">The values field content to test.</param>
    /// <returns>True when the value is a non-empty string, array or object, or any other kind.</returns>
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
    /// <param name="model">The built project model the record belongs to.</param>
    /// <param name="record">The record whose waiting drafts are wanted.</param>
    /// <returns>The ids of the drafts waiting on the record, in ordinal order.</returns>
    public static IReadOnlyList<string> AwaitingOf(ProjectModel model, Record record) =>
        AwaitingOf(model.Records, record);

    /// <summary>
    ///     The drafts waiting on the record, from the records: the drafts a draft
    ///     decision decides, a draft requirement they fulfill, or a draft journey a
    ///     transcribed record serves.
    /// </summary>
    /// <param name="records">Every written record to scan for waiting drafts.</param>
    /// <param name="record">The record whose waiting drafts are wanted.</param>
    /// <returns>The waiting draft ids sorted ordinally, from the given records.</returns>
    static List<string> AwaitingOf(IReadOnlyList<Record> records, Record record)
    {
        if (record.Status != RecordStatuses.Draft)
        {
            return [];
        }

        var waiting = new List<string>();

        foreach (var draft in records)
        {
            if (draft.Derived || draft.Status != RecordStatuses.Draft || draft.Id == record.Id || draft.Id.Length == 0)
            {
                continue;
            }

            if (record.Type == RecordTypes.Decision && record.Ids(RecordFields.Decides).Contains(draft.Id, StringComparer.Ordinal))
            {
                waiting.Add(draft.Id);
            }
            else if (record.Type == RecordTypes.Requirement && draft.Ids(RecordFields.Fulfills).Contains(record.Id, StringComparer.Ordinal))
            {
                waiting.Add(draft.Id);
            }
            else if (record.Type == RecordTypes.Journey
                && draft.Field(RecordFields.Provenance) == "transcribed"
                && draft.Ids(RecordFields.Serves).Any(target =>
                    target == record.Id || target.StartsWith(record.Id + "#", StringComparison.Ordinal)))
            {
                waiting.Add(draft.Id);
            }
        }

        waiting.Sort(StringComparer.Ordinal);

        return waiting;
    }

    /// <summary>Counts the record's open questions: entries that are neither answered nor auto-approved.</summary>
    /// <param name="record">The record whose open questions are counted.</param>
    /// <param name="config">The consumer configuration holding the veto window.</param>
    /// <param name="today">The current date, for auto-approval expiry.</param>
    /// <returns>The number of question entries neither answered nor auto-approved.</returns>
    public static int OpenQuestionCount(Record record, ConsumerConfig config, DateOnly today)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Questions, out var questions)
            || questions.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return questions.EnumerateArray().Count(entry => !Answered(entry) && !AutoApproved(entry, record, config, today));
    }

    /// <summary>
    ///     The tier of a question entry: tier 1 when the parent record carries
    ///     risk, else tier 3. A question carries no tier of its own.
    /// </summary>
    /// <param name="parent">The record carrying the question.</param>
    /// <returns>1 when the parent carries risk, otherwise 3.</returns>
    static int QuestionTier(Record parent) => HasRisk(parent) ? 1 : 3;

    /// <summary>Whether the question entry is answered: it carries an answer or an answerer.</summary>
    /// <param name="element">The question entry to test.</param>
    /// <returns>True when the entry carries an answer or an answerer.</returns>
    static bool Answered(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && ((element.TryGetProperty(RecordFields.Answer, out var answer)
                && answer.ValueKind == JsonValueKind.String
                && answer.GetString() is { Length: > 0 })
            || (element.TryGetProperty(RecordFields.AnsweredBy, out var by)
                && by.ValueKind == JsonValueKind.String
                && by.GetString() is { Length: > 0 }));

    /// <summary>Whether the question entry carries a recommended answer.</summary>
    /// <param name="element">The question entry to test.</param>
    /// <returns>True when the entry carries a non-empty recommended answer.</returns>
    static bool HasRecommendation(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(RecordFields.Recommendation, out var value)
        && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
        && (value.ValueKind != JsonValueKind.String || value.GetString() is { Length: > 0 })
        && (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0);

    /// <summary>
    ///     Whether the question entry counts as auto-approved: tier 3 with a recommended
    ///     answer, past its decide-by date plus the veto window.
    /// </summary>
    /// <param name="element">The question entry to test.</param>
    /// <param name="parent">The record carrying the question.</param>
    /// <param name="config">The consumer configuration holding the veto window.</param>
    /// <param name="today">The current date to compare against the decide-by date.</param>
    /// <returns>True when the entry is tier 3, recommended, and past its veto window.</returns>
    static bool AutoApproved(JsonElement element, Record parent, ConsumerConfig config, DateOnly today) =>
        QuestionTier(parent) == 3
        && HasRecommendation(element)
        && element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(RecordFields.DecideBy, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } text
        && ParseDay(text, out var decideBy)
        && today > decideBy.AddDays(config.VetoWindowDays);

    /// <summary>Parses an ISO day, or a timestamp read as its day.</summary>
    /// <param name="text">The date text, an ISO day or a timestamp.</param>
    /// <param name="day">The parsed day when the text parses.</param>
    /// <returns>True when the text parses as a day or a timestamp.</returns>
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
    /// <param name="model">The built project model the record belongs to.</param>
    /// <param name="record">The record whose scenario evidence is wanted.</param>
    /// <returns>The total, confirmed, passing and stale scenario counts for the record.</returns>
    public static ReviewScenarios ScenariosOf(ProjectModel model, Record record) =>
        ScenariosOf(model.Records, model.Proves, model.Stale, record);

    /// <summary>
    ///     The scenario evidence from the queue inputs: a use case, reaction or job
    ///     counts its scenarios, a scenario counts itself, and every other record
    ///     counts none.
    /// </summary>
    /// <param name="records">Every written record, for the owned scenario lookup.</param>
    /// <param name="proves">The computed proves links.</param>
    /// <param name="staleIds">The ids of scenarios whose proof is stale.</param>
    /// <param name="record">The record whose scenario evidence is wanted.</param>
    /// <returns>The scenario counts computed from the given queue inputs.</returns>
    static ReviewScenarios ScenariosOf(
        IReadOnlyList<Record> records,
        IReadOnlyList<ProvesLink> proves,
        IReadOnlySet<string> staleIds,
        Record record)
    {
        List<Record> own = record.Type switch
        {
            RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job => OwnedScenarios(records, record),
            RecordTypes.Scenario => [record],
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
    /// <param name="records">Every written record to scan.</param>
    /// <param name="holder">The use case, reaction or job owning the scenarios.</param>
    /// <returns>The written scenarios the holder owns, in record order.</returns>
    static List<Record> OwnedScenarios(IReadOnlyList<Record> records, Record holder)
    {
        if (holder.Type is RecordTypes.Reaction or RecordTypes.Job)
        {
            return records
                .Where(scenario => !scenario.Derived
                    && scenario.Type == RecordTypes.Scenario
                    && scenario.PageId == holder.Id)
                .ToList();
        }

        if (!Identifiers.TrySplit(holder.Id, out _, out _, out var name))
        {
            return [];
        }

        return records
            .Where(scenario => !scenario.Derived
                && scenario.Type == RecordTypes.Scenario
                && Identifiers.TrySplit(scenario.Id, out _, out var home, out _)
                && home == name)
            .ToList();
    }

    /// <summary>The reaction or job whose page carries the scenario, when one holds it.</summary>
    /// <param name="records">Every written record to scan.</param>
    /// <param name="scenario">The scenario whose page holder is wanted.</param>
    /// <returns>The reaction or job on the scenario page, or null when no page holds it.</returns>
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

            if (record.Id == pageId && record.Type is RecordTypes.Reaction or RecordTypes.Job)
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
    /// <param name="records">Every written record, for holder lookups.</param>
    /// <param name="queued">The queued draft and reopened records to assign.</param>
    /// <param name="behaviorDocs">The behavior docs root, for module and type batches.</param>
    /// <returns>The assignment groups, one per batch.</returns>
    static List<Group> Assign(
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
            if (record.Type == RecordTypes.UseCase)
            {
                For("use-case/" + record.Id, record.Id, RecordTypes.UseCase).Members.Add(record);
            }
            else if (record.Type is RecordTypes.Reaction or RecordTypes.Job)
            {
                For(record.Type + "/" + record.Id, record.Id, record.Type).Members.Add(record);
            }
            else if (record.Type == RecordTypes.Scenario
                && PageHolder(records, record) is { } page)
            {
                For(page.Type + "/" + page.Id, page.Id, page.Type).Members.Add(record);
            }
            else if (record.Type is RecordTypes.Result or RecordTypes.Scenario
                && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                && home is not null
                && UseCaseOf(records, home) is { } holder)
            {
                For("use-case/" + holder.Id, holder.Id, RecordTypes.UseCase).Members.Add(record);
            }
            else if (record.Type == RecordTypes.Decision)
            {
                For("decision/" + record.Id, "decision/" + record.Id, RecordTypes.Decision).Members.Add(record);
            }
            else if (Deciding(records, queued, record) is { } decider)
            {
                For("decision/" + decider.Id, "decision/" + decider.Id, RecordTypes.Decision).Members.Add(record);
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
    /// <param name="records">Every written record to scan for decisions.</param>
    /// <param name="queued">The queued records, whose deciding decisions win.</param>
    /// <param name="record">The record whose deciding decision is wanted.</param>
    /// <returns>The deciding decision, or null when no decision decides the record.</returns>
    static Record? Deciding(IReadOnlyList<Record> records, IReadOnlyList<Record> queued, Record record)
    {
        var queuedIds = queued.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);

        return records
            .Where(decision => !decision.Derived
                && decision.Type == RecordTypes.Decision
                && decision.Ids(RecordFields.Decides).Contains(record.Id, StringComparer.Ordinal))
            .OrderBy(decision => queuedIds.Contains(decision.Id) ? 0 : 1)
            .ThenBy(decision => decision.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>Describes one group as a batch, with its records in approval order.</summary>
    /// <param name="records">Every written record, derived records included.</param>
    /// <param name="effectiveStatus">The effective status of each record, by id.</param>
    /// <param name="proves">The computed proves links.</param>
    /// <param name="stale">The ids of scenarios whose proof is stale.</param>
    /// <param name="group">The assignment group to describe.</param>
    /// <param name="findings">The open finding count per record.</param>
    /// <param name="config">The consumer configuration of the project.</param>
    /// <param name="today">The current date, for open-question expiry.</param>
    /// <returns>The batch describing the group, with entries in approval order.</returns>
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
    /// <param name="group">The batch group whose anchor orders first.</param>
    /// <param name="left">The first entry to compare.</param>
    /// <param name="right">The second entry to compare.</param>
    /// <returns>Negative when the left entry orders before the right, positive when after, zero when equal.</returns>
    internal static int CompareRecords(Group group, ReviewEntry left, ReviewEntry right) =>
        CompareRecords(AnchorOf(group), left.Record, right.Record);

    /// <summary>Orders two records for approval: the anchor first, then authority order.</summary>
    /// <param name="anchor">The batch anchor id ordering first, or null.</param>
    /// <param name="left">The first record to compare.</param>
    /// <param name="right">The second record to compare.</param>
    /// <returns>Negative when the left record orders before the right, positive when after, zero when equal.</returns>
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
    /// <param name="group">The batch group whose anchor is wanted.</param>
    /// <returns>The anchor record id for an anchored batch kind, or null for a module and type batch.</returns>
    static string? AnchorOf(Group group) => group.Kind switch
    {
        RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job => group.Selector,
        RecordTypes.Decision => group.Selector["decision/".Length..],
        _ => null,
    };

    /// <summary>Intent before Governance before Behavior.</summary>
    /// <param name="record">The record whose layer rank is wanted.</param>
    /// <returns>0 for intent, 1 for governance and 2 for behavior.</returns>
    static int LayerRank(Record record) => LayerOf(record) switch
    {
        "governance" => 1,
        "behavior" => 2,
        _ => 0,
    };

    /// <summary>The ladder position, after every ladder type when the type has none.</summary>
    /// <param name="type">The record type to rank.</param>
    /// <returns>The ladder position of the type, or the ladder length when the type has none.</returns>
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
    /// <param name="left">The first batch to compare.</param>
    /// <param name="right">The second batch to compare.</param>
    /// <returns>Negative when the left batch orders before the right, positive when after, zero when equal.</returns>
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
    /// <param name="batch">The batch whose authority rank is wanted.</param>
    /// <returns>The lowest layer rank among the batch records, or 0 for an empty batch.</returns>
    static int AuthorityRank(ReviewBatch batch) =>
        batch.Records.Select(entry => LayerRank(entry.Record)).DefaultIfEmpty(0).Min();

    /// <summary>Writes the queue as one JSON object with a batches array.</summary>
    /// <param name="batches">The batches to serialize.</param>
    /// <returns>The queue as a JSON object with a batches array.</returns>
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
    /// <param name="batches">The batches to print.</param>
    /// <returns>One block per batch, or the no-work message when the queue is empty.</returns>
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
    /// <param name="entry">The queue entry whose flags are printed.</param>
    /// <returns>The space-joined flags, or a dash when the entry carries none.</returns>
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
