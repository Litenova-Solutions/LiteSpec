using System.Globalization;

namespace LiteSpec.Model;

/// <summary>One record's approval in a batch: what changes and what it carries.</summary>
/// <param name="Record">The batch record.</param>
/// <param name="Action">The action: promoted, re-stamped, or held.</param>
/// <param name="TargetStatus">The status the record holds after the batch.</param>
/// <param name="Hash">The semantic hash at approval time.</param>
/// <param name="Tier">The approval tier.</param>
/// <param name="Scenarios">The scenario evidence.</param>
/// <param name="Findings">The count of open findings about the record.</param>
/// <param name="HeldGuards">The guards holding the record, empty unless a module and type batch held it.</param>
sealed record BatchAction(
    Record Record,
    string Action,
    string TargetStatus,
    string Hash,
    int Tier,
    ReviewScenarios Scenarios,
    int Findings,
    IReadOnlyList<string> HeldGuards);

/// <summary>One batch plan: the ordered approvals, the left-out records, the guard failures, and the sample.</summary>
/// <param name="Batch">The queued batch.</param>
/// <param name="Ordered">Every batch record in approval order, held records included.</param>
/// <param name="LeftOut">The tier 1 records a module and type batch leaves out.</param>
/// <param name="Failures">The guard failures, each naming its record and guard.</param>
/// <param name="Sample">The sampled tier 3 ids.</param>
sealed record BatchPlan(
    ReviewBatch Batch,
    IReadOnlyList<BatchAction> Ordered,
    IReadOnlyList<Record> LeftOut,
    IReadOnlyList<(string Id, string Guard)> Failures,
    IReadOnlyList<string> Sample);

/// <summary>
///     Batch approval: promotes each draft in one batch, re-stamps each reopened record,
///     holds the records whose guards fail in a module and type batch, refuses any other
///     batch whole when one guard fails, and writes the batch manifest.
/// </summary>
/// <follows>litespec/core/release#approve-drafts-in-bounded-batches</follows>
static class BatchApprover
{
    /// <summary>Finds the batch the selector names, or null.</summary>
    public static ReviewBatch? Find(IReadOnlyList<ReviewBatch> batches, string selector) =>
        batches.FirstOrDefault(batch =>
            string.Equals(batch.Selector, selector, StringComparison.Ordinal));

    /// <summary>
    ///     Plans the batch: the approvals in order, the records a module and type batch
    ///     leaves out, the guard failures, and the tier 3 sample. A module and type batch
    ///     holds each record its record guards fail and approves the rest, and fails whole
    ///     only when no approver is named or no record passes; any other batch fails whole
    ///     when one guard fails.
    /// </summary>
    public static BatchPlan Plan(
        ProjectModel model,
        ReviewBatch batch,
        string? by,
        string? secondReviewer,
        string? reviewReport,
        IReadOnlyDictionary<string, int>? findings = null,
        string? on = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        findings ??= ReviewQueue.FindingsBySubject(model);
        var members = MembersOf(model, batch);
        var anchor = batch.Kind switch
        {
            RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job => batch.Selector,
            RecordTypes.Decision => batch.Selector["decision/".Length..],
            "record" => batch.Selector,
            _ => (string?)null,
        };

        members.Sort((left, right) => ReviewQueue.CompareRecords(anchor, left, right));

        var leftOut = batch.Kind == "module-type"
            ? members.Where(record => ReviewQueue.TierOf(model, record) == 1).ToList()
            : new List<Record>();
        var approvals = members.Except(leftOut).ToList();
        var ordered = approvals.Select(record => Describe(model, record, findings)).ToList();
        var changed = ordered
            .Where(action => action.Action is "promoted" or "re-stamped")
            .ToList();
        var batchFailures = BatchGuards(model, changed, by, secondReviewer, reviewReport);
        var recordFailures = RecordGuards(model, changed, approvals, today);
        IReadOnlyList<(string Id, string Guard)> failures;

        if (batch.Kind == "module-type" && batchFailures.Count == 0)
        {
            ordered = HoldFailed(ordered, recordFailures);
            changed = ordered
                .Where(action => action.Action is "promoted" or "re-stamped")
                .ToList();
            failures = changed.Count == 0 && recordFailures.Count > 0
                ? recordFailures
                : [];
        }
        else
        {
            failures = batchFailures.Concat(recordFailures).ToList();
        }

        var sample = Sample(
            model,
            changed.Where(action => action.Tier == 3).Select(action => action.Record).ToList(),
            model.Config.SampleShare);

        return new BatchPlan(batch, ordered, leftOut, failures, sample);
    }

    /// <summary>
    ///     Holds each changed record the record guards fail: it keeps its status, gets no
    ///     stamp, and carries its guards, so one blocked record holds back only itself.
    /// </summary>
    static List<BatchAction> HoldFailed(
        IReadOnlyList<BatchAction> ordered, IReadOnlyList<(string Id, string Guard)> recordFailures)
    {
        var heldGuards = recordFailures
            .GroupBy(failure => failure.Id, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(failure => failure.Guard).ToList(),
                StringComparer.Ordinal);

        return ordered
            .Select(action => action.Action is "promoted" or "re-stamped"
                && heldGuards.TryGetValue(action.Record.Id, out var guards)
                    ? action with
                    {
                        Action = "held",
                        TargetStatus = action.Record.Status ?? RecordStatuses.Approved,
                        HeldGuards = guards,
                    }
                    : action)
            .ToList();
    }

    /// <summary>Every record the batch approval covers, held records included.</summary>
    static List<Record> MembersOf(ProjectModel model, ReviewBatch batch)
    {
        if (batch.Kind == RecordTypes.UseCase
            && model.ById.TryGetValue(batch.Selector, out var useCase)
            && useCase.Type == RecordTypes.UseCase)
        {
            var members = new List<Record> { useCase };

            if (Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
            {
                members.AddRange(model.Records.Where(record => !record.Derived
                    && record.Type is RecordTypes.Result or RecordTypes.Scenario
                    && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                    && home == name
                    && ModelBuilder.UseCaseOf(model, home) is { } holder
                    && holder.Id == useCase.Id
                    && !OnReactionOrJobPage(model, record)));
            }

            return members;
        }

        if (batch.Kind is RecordTypes.Reaction or RecordTypes.Job
            && model.ById.TryGetValue(batch.Selector, out var holder)
            && holder.Type == batch.Kind)
        {
            var members = new List<Record> { holder };

            members.AddRange(model.Records.Where(record => !record.Derived
                && record.Type == RecordTypes.Scenario
                && record.PageId == holder.Id));

            return members;
        }

        if (batch.Kind == RecordTypes.Decision
            && model.ById.TryGetValue(batch.Selector["decision/".Length..], out var decision)
            && decision.Type == RecordTypes.Decision)
        {
            var members = new List<Record> { decision };
        var added = new HashSet<string>(StringComparer.Ordinal) { decision.Id };

        foreach (var id in decision.Ids(RecordFields.Decides).OrderBy(id => id, StringComparer.Ordinal))
        {
            // Use cases anchor their own batches with their results and scenarios, reactions
            // and jobs anchor theirs with the scenarios on their page, and decisions anchor
            // their own, so a decision batch holds none of those.
            if (!added.Add(id)
                || !model.ById.TryGetValue(id, out var decided)
                || decided.Derived
                || decided.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job or RecordTypes.Decision
                || ClaimedByUseCase(model, decided))
            {
                continue;
            }

            members.Add(decided);
        }

        return members;
        }

        return batch.Records.Select(entry => entry.Record).ToList();
    }

    /// <summary>Whether a reaction or job page carries the scenario, so its own batch holds it.</summary>
    static bool OnReactionOrJobPage(ProjectModel model, Record record) =>
        record.Type == RecordTypes.Scenario
        && record.PageId is not null
        && model.ById.TryGetValue(record.PageId, out var page)
        && page.Type is RecordTypes.Reaction or RecordTypes.Job;

    /// <summary>Whether a use case, reaction or job batch claims the record: a use case result or scenario, or a scenario on a reaction or job page.</summary>
    static bool ClaimedByUseCase(ProjectModel model, Record record)
    {
        if (OnReactionOrJobPage(model, record))
        {
            return true;
        }

        return record.Type is RecordTypes.Result or RecordTypes.Scenario
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && ModelBuilder.UseCaseOf(model, home) is not null;
    }

    /// <summary>Describes one member's approval: its action, target status, and evidence.</summary>
    static BatchAction Describe(ProjectModel model, Record record, IReadOnlyDictionary<string, int> findings)
    {
        var action = record.Status == RecordStatuses.Draft
            ? "promoted"
            : ReviewQueue.IsReopened(record) ? "re-stamped" : "held";
        var target = action == "promoted"
            ? ReviewQueue.LayerOf(record) == "behavior" ? RecordStatuses.Reviewed : RecordStatuses.Approved
            : record.Status ?? RecordStatuses.Approved;

        return new BatchAction(
            record,
            action,
            target,
            Checker.SemanticHash(record),
            ReviewQueue.TierOf(model, record),
            ReviewQueue.ScenariosOf(model, record),
            findings.TryGetValue(record.Id, out var count) ? count : 0,
            []);
    }

    /// <summary>
    ///     Checks the batch guards before anything is written: every changed record
    ///     carries a named owner approver, and every tier 1 record a second reading.
    ///     A batch guard fails the whole batch, whatever its kind.
    /// </summary>
    static IReadOnlyList<(string Id, string Guard)> BatchGuards(
        ProjectModel model,
        IReadOnlyList<BatchAction> changed,
        string? by,
        string? secondReviewer,
        string? reviewReport)
    {
        var failures = new List<(string, string)>();
        var tier1 = changed.Where(action => action.Tier == 1).ToList();

        // Every record a batch changes carries a person's name, so the approver guard covers every tier.
        foreach (var action in changed.OrderBy(entry => entry.Record.Id, StringComparer.Ordinal))
        {
            if (ApproverGuard(model, by) is { } approver)
            {
                failures.Add((action.Record.Id, approver));
            }
        }

        foreach (var action in tier1.OrderBy(entry => entry.Record.Id, StringComparer.Ordinal))
        {
            if (ReviewerGuard(model, by, secondReviewer, reviewReport) is { } reviewer)
            {
                failures.Add((action.Record.Id, reviewer));
            }
        }

        return failures;
    }

    /// <summary>
    ///     Checks the record guards before anything is written: each changed record
    ///     carries no open question, each decision names its chosen option, and no
    ///     record stays draft behind what it rests on. Guards gate what the batch
    ///     changes, so records held from an earlier act carry none.
    /// </summary>
    static IReadOnlyList<(string Id, string Guard)> RecordGuards(
        ProjectModel model,
        IReadOnlyList<BatchAction> changed,
        IReadOnlyList<Record> approvals,
        DateOnly today)
    {
        var failures = new List<(string, string)>();

        foreach (var action in changed.OrderBy(entry => entry.Record.Id, StringComparer.Ordinal))
        {
            if (ReviewQueue.OpenQuestionCount(action.Record, model.Config, today) > 0)
            {
                failures.Add((action.Record.Id, "has open questions"));
            }

            // An approved decision states its choice; a decision with none is not decided.
            if (action.Record.Type == RecordTypes.Decision && action.Record.Field(RecordFields.Choice) is not { Length: > 0 })
            {
                failures.Add((action.Record.Id, "names no choice"));
            }
        }

        var simulated = SimulatedStatus(model, approvals);

        foreach (var action in changed.OrderBy(entry => entry.Record.Id, StringComparer.Ordinal))
        {
            if (simulated.TryGetValue(action.Record.Id, out var status) && status == RecordStatuses.Draft
                && RestedOn(model, simulated, action.Record) is { } parent)
            {
                failures.Add((action.Record.Id, $"stays draft behind draft {parent}"));
            }
            else if (TranscribedHold(model, simulated, action.Record) is { } journey)
            {
                failures.Add((action.Record.Id, $"stays transcribed behind draft {journey}"));
            }
        }

        return failures;
    }

    /// <summary>The named-approver guard: every batch needs a person from the owners list.</summary>
    static string? ApproverGuard(ProjectModel model, string? by)
    {
        if (by is not { Length: > 0 })
        {
            return "needs --by naming a project owner (missing)";
        }

        if (by == "auto (veto window)")
        {
            return "needs --by naming a project owner (auto (veto window) is not a person)";
        }

        return model.Config.Owners.Contains(by, StringComparer.Ordinal)
            ? null
            : $"needs --by naming a project owner ('{by}' is not a project owner)";
    }

    /// <summary>
    ///     The second-reviewer guard: tier 1 needs another reading beside the approver,
    ///     except on a project whose owners list holds one name.
    /// </summary>
    static string? ReviewerGuard(ProjectModel model, string? by, string? secondReviewer, string? reviewReport)
    {
        if (model.Config.Owners.Count == 1)
        {
            return null;
        }

        if (secondReviewer is not { Length: > 0 })
        {
            return "tier 1 needs --second-reviewer other than the approver (missing; a project whose owners list holds one name needs none)";
        }

        if (secondReviewer == by)
        {
            return "tier 1 needs --second-reviewer other than the approver (names the approver)";
        }

        if (secondReviewer == "agent"
            && (reviewReport is not { Length: > 0 } || !File.Exists(reviewReport)))
        {
            return "tier 1 needs --second-reviewer other than the approver (agent needs an existing --review-report file)";
        }

        return null;
    }

    /// <summary>
    ///     Each record's effective status after the batch: the approvals hold their target
    ///     status, and every other record keeps its own.
    /// </summary>
    static IReadOnlyDictionary<string, string> SimulatedStatus(
        ProjectModel model, IReadOnlyList<Record> approvals)
    {
        var targets = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var record in approvals)
        {
            targets[record.Id] = record.Status == RecordStatuses.Draft
                ? ReviewQueue.LayerOf(record) == "behavior" ? RecordStatuses.Reviewed : RecordStatuses.Approved
                : record.Status ?? RecordStatuses.Approved;
        }

        var overlaid = model.Records
            .Select(record => (IRecordFacts)new StatusOverlay(
                record, targets.TryGetValue(record.Id, out var target) ? target : record.Status))
            .ToList();
        var byId = new Dictionary<string, IRecordFacts>(StringComparer.Ordinal);

        foreach (var record in overlaid)
        {
            byId.TryAdd(record.Id, record);
        }

        return Authority.EffectiveStatus(overlaid, record => UseCaseHolder(model, byId, record));
    }

    /// <summary>The use case holding a result or scenario in the overlaid records.</summary>
    static IRecordFacts? UseCaseHolder(
        ProjectModel model, IReadOnlyDictionary<string, IRecordFacts> byId, IRecordFacts record)
    {
        if (!model.ById.TryGetValue(record.Id, out var original))
        {
            return null;
        }

        if (original.Derived
            && original.PageId is not null
            && byId.TryGetValue(original.PageId, out var page)
            && page.Type == RecordTypes.UseCase)
        {
            return page;
        }

        return Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && ModelBuilder.UseCaseOf(model, home) is { } holder
            && byId.TryGetValue(holder.Id, out var overlaid)
                ? overlaid
                : null;
    }

    /// <summary>One record with its status replaced, for the simulated effective status.</summary>
    sealed class StatusOverlay(IRecordFacts inner, string? status) : IRecordFacts
    {
        public string Type => inner.Type;
        public string Id => inner.Id;
        public string Title => inner.Title;
        public string? Status => status;
        public string File => inner.File;
        public int Line => inner.Line;
        public string? Field(string name) => inner.Field(name);
        public IReadOnlyList<string> Ids(string name) => inner.Ids(name);
    }

    /// <summary>
    ///     One draft record the record rests on: a deciding decision, a fulfilled
    ///     requirement, a served journey for a transcribed record, or the holding use case.
    /// </summary>
    /// <summary>
    ///     The draft journey a transcribed record serves, which holds it in draft whatever its
    ///     own status: a transcribed record is reviewed only once its Intent parent stands.
    /// </summary>
    /// <follows>litespec/core/model#hold-transcribed-records-in-draft</follows>
    static string? TranscribedHold(ProjectModel model, IReadOnlyDictionary<string, string> simulated, Record record) =>
        record.Field(RecordFields.Provenance) != "transcribed"
            ? null
            : record.Ids(RecordFields.Serves)
                .Select(Identifiers.BaseId)
                .Where(id => model.ById.TryGetValue(id, out var target) && target.Type == RecordTypes.Journey
                    && simulated.TryGetValue(id, out var status) && status == RecordStatuses.Draft)
                .OrderBy(id => id, StringComparer.Ordinal)
                .FirstOrDefault();

    static string? RestedOn(ProjectModel model, IReadOnlyDictionary<string, string> simulated, Record record)
    {
        string? DraftParent(IEnumerable<string> ids) => ids
            .Where(id => simulated.TryGetValue(id, out var status) && status == RecordStatuses.Draft)
            .OrderBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault();

        var decisions = model.Records
            .Where(decision => decision.Type == RecordTypes.Decision
                && decision.Ids(RecordFields.Decides).Contains(record.Id, StringComparer.Ordinal))
            .Select(decision => decision.Id);

        if (DraftParent(decisions) is { } decision)
        {
            return decision;
        }

        if (DraftParent(record.Ids(RecordFields.Fulfills)) is { } requirement)
        {
            return requirement;
        }

        if (TranscribedHold(model, simulated, record) is { } journey)
        {
            return journey;
        }

        if (record.Type is RecordTypes.Result or RecordTypes.Scenario
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && ModelBuilder.UseCaseOf(model, home) is { } holder
            && simulated.TryGetValue(holder.Id, out var status) && status == RecordStatuses.Draft)
        {
            return holder.Id;
        }

        return null;
    }

    /// <summary>
    ///     Draws the tier 3 sample: the records with the lowest semantic hashes read as
    ///     hexadecimal numbers, the project's share rounded up, at least one.
    /// </summary>
    public static IReadOnlyList<string> Sample(
        ProjectModel model, IReadOnlyList<Record> tier3, double share) =>
        tier3
            .Select(record => (record.Id, Hash: Checker.SemanticHash(record)))
            .OrderBy(pair => HashValue(pair.Hash))
            .ThenBy(pair => pair.Id, StringComparer.Ordinal)
            .Take(Math.Max(1, (int)Math.Ceiling(tier3.Count * share)))
            .Select(pair => pair.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

    /// <summary>Reads a semantic hash as a hexadecimal number.</summary>
    static ulong HashValue(string hash) =>
        ulong.TryParse(hash, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)
            ? value
            : ulong.MaxValue;

    /// <summary>
    ///     Writes the batch manifest under the docs proof folder, appending -2, -3 and so
    ///     on while a manifest of that name exists. Returns the manifest path.
    /// </summary>
    public static string WriteManifest(
        ProjectModel model, BatchPlan plan, string by, string? secondReviewer, string? reviewReport, string on)
    {
        var docs = model.Config.RecordRoots.FirstOrDefault() ?? Path.Combine(model.Config.Root, "docs");
        var folder = Path.Combine(docs, "proof", "approvals");

        Directory.CreateDirectory(folder);

        var stem = $"{on}-{plan.Batch.Selector.Replace("/", "-", StringComparison.Ordinal)
            .Replace(".", "-", StringComparison.Ordinal)
            .Replace(":", "-", StringComparison.Ordinal)}";
        var path = Path.Combine(folder, stem + ".json");
        var attempt = 2;

        while (File.Exists(path))
        {
            path = Path.Combine(folder, $"{stem}-{attempt}.json");
            attempt++;
        }

        var manifest = CliJson.Serialize(new
        {
            selector = plan.Batch.Selector,
            approvedBy = by,
            secondReviewer,
            reviewReport,
            approvedOn = on,
            sampleShare = model.Config.SampleShare,
            sample = plan.Sample,
            records = plan.Ordered
                .OrderBy(action => action.Record.Id, StringComparer.Ordinal)
                .Select(action => new
                {
                    id = action.Record.Id,
                    type = action.Record.Type,
                    tier = action.Tier,
                    action = action.Action,
                    hash = action.Hash,
                    scenarios = new
                    {
                        total = action.Scenarios.Total,
                        confirmed = action.Scenarios.Confirmed,
                        passing = action.Scenarios.Passing,
                        stale = action.Scenarios.Stale,
                    },
                    findings = action.Findings,
                    heldBecause = action.HeldGuards.Count > 0 ? action.HeldGuards : null,
                }).ToList(),
        });

        File.WriteAllText(path, manifest + "\n");

        return path;
    }

    /// <summary>
    ///     Updates the in-memory model after a batch is applied: each promoted or
    ///     re-stamped record holds its target status and approval data, and the
    ///     effective status is recomputed, so a later batch in the same run sees
    ///     the promotion without a rebuild from disk.
    /// </summary>
    public static void Apply(ProjectModel model, BatchPlan plan, string? by, string on)
    {
        foreach (var action in plan.Ordered.Where(entry => entry.Action is "promoted" or "re-stamped"))
        {
            var index = model.Records.IndexOf(action.Record);

            if (index < 0)
            {
                index = model.Records.FindIndex(record => record.Id == action.Record.Id);
            }

            if (index < 0)
            {
                continue;
            }

            var record = model.Records[index];
            var updated = new Record
            {
                Type = record.Type,
                Id = record.Id,
                Title = record.Title,
                Owner = record.Owner,
                Status = action.TargetStatus,
                Decider = record.Decider,
                DecideBy = record.DecideBy,
                ApprovedBy = by ?? record.Decider ?? record.Owner ?? "unknown",
                ApprovedOn = on,
                ApprovedHash = Checker.SemanticHash(record),
                Derived = record.Derived,
                File = record.File,
                Line = record.Line,
                IsEntry = record.IsEntry,
                PageId = record.PageId,
                Raw = record.Raw,
            };

            model.Records[index] = updated;

            if (model.ById is Dictionary<string, Record> byId
                && byId.TryGetValue(updated.Id, out var current)
                && ReferenceEquals(current, record))
            {
                byId[updated.Id] = updated;
            }
        }

        var holders = new Dictionary<string, IRecordFacts>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Id.Length > 0)
            {
                holders.TryAdd(record.Id, record);
            }
        }

        var effective = Authority.EffectiveStatus(
            model.Records, record => UseCaseHolder(model, holders, record));

        model.EffectiveStatus.Clear();

        foreach (var entry in effective)
        {
            model.EffectiveStatus[entry.Key] = entry.Value;
        }
    }

    /// <summary>Prints the dry run: the plan, the left-out records, the failures, and the sample.</summary>
    public static string DryRun(BatchPlan plan, double share)
    {
        var lines = new List<string>();
        var changed = plan.Ordered.Count(action => action.Action is "promoted" or "re-stamped");
        var held = plan.Ordered.Count - changed;

        lines.Add($"Batch {plan.Batch.Selector} ({plan.Batch.Kind}, tier {plan.Batch.Tier}): "
            + $"{changed} to approve, {held} held"
            + (plan.LeftOut.Count > 0 ? $", {plan.LeftOut.Count} left out" : "")
            + " (dry run)");

        foreach (var action in plan.Ordered)
        {
            lines.Add(action.Action switch
            {
                "promoted" => $"  {action.Record.Id} would be promoted to {action.TargetStatus}",
                "re-stamped" => $"  {action.Record.Id} would be re-stamped",
                _ when action.HeldGuards.Count > 0 =>
                    $"  {action.Record.Id} would be held: {string.Join("; ", action.HeldGuards)}",
                _ => $"  {action.Record.Id} would be held",
            });
        }

        foreach (var record in plan.LeftOut.OrderBy(entry => entry.Id, StringComparer.Ordinal))
        {
            lines.Add($"  {record.Id}: left out (tier 1)");
        }

        foreach (var (id, guard) in plan.Failures)
        {
            lines.Add($"  {id}: {guard}");
        }

        lines.Add(SampleLine(plan, share));

        if (plan.Failures.Count > 0)
        {
            lines.Add($"Refused: {plan.Failures.Count} guard failures.");
        }

        return string.Join('\n', lines);
    }

    /// <summary>Prints the sample: its size against the tier 3 approvals, and its ids.</summary>
    static string SampleLine(BatchPlan plan, double share)
    {
        var tier3 = plan.Ordered.Count(action =>
            action.Action is "promoted" or "re-stamped" && action.Tier == 3);

        return plan.Sample.Count == 0
            ? $"Sample (0 of {tier3} at {share}): none."
            : $"Sample ({plan.Sample.Count} of {tier3} at {share}): {string.Join(", ", plan.Sample)}.";
    }
}
