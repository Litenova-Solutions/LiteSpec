using System.Text.Json;

namespace LiteSpec.Model.Diff;

/// <summary>
///     Compares two project models record by record, matched by identifier.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
/// <follows>litespec/core/release#review-behavior-changes-through-the-behavior-diff</follows>
static class Differ
{
    /// <summary>The breaking reason for a narrowed requirement.</summary>
    /// <returns>The breaking reason for a narrowed requirement.</returns>
    internal static BreakingReason RequirementNarrowed => Reason("requirement-narrowed");

    /// <summary>The breaking reason for a changed quality measure.</summary>
    /// <returns>The breaking reason for a changed quality measure.</returns>
    internal static BreakingReason QualityMeasureChanged => Reason("quality-measure-changed");

    /// <summary>The breaking reason for a weakened rule.</summary>
    /// <returns>The breaking reason for a weakened rule.</returns>
    internal static BreakingReason RuleWeakened => Reason("rule-weakened");

    /// <summary>The breaking reason for a changed result outcome.</summary>
    /// <returns>The breaking reason for a changed result outcome.</returns>
    internal static BreakingReason ResultOutcomeChanged => Reason("result-outcome-changed");

    /// <summary>The breaking reason for a changed refusal meaning.</summary>
    /// <returns>The breaking reason for a changed refusal meaning.</returns>
    internal static BreakingReason RefusalMeaningChanged => Reason("refusal-meaning-changed");

    /// <summary>The breaking reason for a removed state.</summary>
    /// <returns>The breaking reason for a removed state.</returns>
    internal static BreakingReason StateRemoved => Reason("state-removed");

    /// <summary>The breaking reason for a removed transition.</summary>
    /// <returns>The breaking reason for a removed transition.</returns>
    internal static BreakingReason TransitionRemoved => Reason("transition-removed");

    /// <summary>Diffs the base model against the head model.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="baseLabel">The base name, as the caller passed it.</param>
    /// <param name="headLabel">The head name, as the caller passed it.</param>
    /// <returns>The intent diff between the base model and the head model.</returns>
    public static IntentDiff Diff(
        ProjectModel baseModel, ProjectModel headModel, string baseLabel, string headLabel)
    {
        var baseById = baseModel.ById;
        var headById = headModel.ById;

        // A derived generic result follows its use case, so the use case's own
        // entry carries the change and the derived record stays out of the diff.
        var removedIds = baseById.Keys.Where(id => !headById.ContainsKey(id) && !baseById[id].Derived)
            .ToHashSet(StringComparer.Ordinal);
        var addedIds = headById.Keys.Where(id => !baseById.ContainsKey(id) && !headById[id].Derived)
            .ToHashSet(StringComparer.Ordinal);
        var changes = new List<IntentChange>();
        var byId = new Dictionary<string, IntentChange>(StringComparer.Ordinal);

        foreach (var id in removedIds)
        {
            var entry = RemovedEntry(baseModel, baseById[id]);

            changes.Add(entry);
            byId[id] = entry;
        }

        foreach (var id in addedIds)
        {
            var entry = AddedEntry(baseModel, headModel, headById[id], addedIds);

            changes.Add(entry);
            byId[id] = entry;
        }

        foreach (var id in baseById.Keys.Where(id => headById.ContainsKey(id) && !baseById[id].Derived))
        {
            var entry = ModifiedEntry(
                baseModel, headModel, baseById[id], headById[id], addedIds, removedIds, byId);

            if (entry is not null)
            {
                changes.Add(entry);
                byId[id] = entry;
            }
        }

        changes.Sort((left, right) =>
        {
            var section = left.Change.CompareTo(right.Change);

            if (section != 0)
            {
                return section;
            }

            var family = IntentDiff.FamilyOrder(left.Family).CompareTo(IntentDiff.FamilyOrder(right.Family));

            if (family != 0)
            {
                return family;
            }

            var type = string.Compare(left.Type, right.Type, StringComparison.Ordinal);

            return type != 0
                ? type
                : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
        });

        for (var index = 0; index < changes.Count; index++)
        {
            var entry = changes[index];

            changes[index] = entry with
            {
                Fingerprint = IntentDiff.FingerprintOf(
                    entry.Change, entry.Id,
                    entry.Reasons.Select(reason => reason.Code), entry.Details),
            };
        }

        return new IntentDiff(
            baseLabel, headLabel, changes, SecurityQueueBuilder.Build(baseModel, headModel, addedIds, removedIds));
    }

    /// <summary>The change entry of a removed record.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="record">The removed record.</param>
    /// <returns>The change entry of the removed record.</returns>
    static IntentChange RemovedEntry(ProjectModel baseModel, Record record)
    {
        var details = new List<string> { RemovedLine(record) };

        if (record.Type == RecordTypes.Scenario)
        {
            details.Add(ScenarioDiffer.Line(record));
        }

        if (record.Type == RecordTypes.Aggregate)
        {
            details.Add(AggregateDiffer.StatesLine(record));
        }

        if (record.Type == RecordTypes.Journey)
        {
            details.AddRange(JourneyDiffer.StepsLines(record));
        }

        var reasons = new List<BreakingReason>();

        if (Approved(baseModel, record))
        {
            var reason = record.Type switch
            {
                RecordTypes.Requirement => "requirement-removed",
                RecordTypes.Result => "result-removed",
                RecordTypes.Scenario => "scenario-removed",
                RecordTypes.Aggregate => "aggregate-removed",
                _ => null,
            };

            if (reason is not null)
            {
                reasons.Add(Reason(reason));
            }
        }

        return Entry(record, ChangeKind.Removed, reasons, details);
    }

    /// <summary>The change entry of an added record.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="record">The added record.</param>
    /// <param name="addedIds">The ids the head adds.</param>
    /// <returns>The change entry of the added record.</returns>
    static IntentChange AddedEntry(
        ProjectModel baseModel, ProjectModel headModel, Record record, HashSet<string> addedIds)
    {
        var details = new List<string> { AddedLine(baseModel, headModel, record) };

        if (record.Type == RecordTypes.Scenario)
        {
            details.Add(ScenarioDiffer.Line(record));
        }

        if (record.Type == RecordTypes.Journey)
        {
            details.AddRange(JourneyDiffer.StepsLines(record));
        }

        var reasons = new List<BreakingReason>();

        if (record.Type == RecordTypes.Result
            && record.Field(RecordFields.Outcome) == "refusal"
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && ModelBuilder.UseCaseOf(baseModel, home) is { } useCase
            && Approved(baseModel, useCase))
        {
            reasons.Add(Reason("refusal-added"));
        }

        if (record.Type == RecordTypes.Rule
            && record.Field(RecordFields.Kind) == "precondition"
            && record.Ids(RecordFields.Governs).Any(target =>
                target.StartsWith("use-case/", StringComparison.Ordinal)
                && baseModel.ById.TryGetValue(target, out var governed)
                && Approved(baseModel, governed)))
        {
            reasons.Add(Reason("precondition-added"));
        }

        if (record.Type == RecordTypes.Scenario
            && record.Ids(RecordFields.Result).FirstOrDefault() is { } resultId
            && addedIds.Contains(resultId)
            && headModel.ById.TryGetValue(resultId, out var result)
            && result.Field(RecordFields.Outcome) == "refusal")
        {
            reasons.Add(Reason("refusal-path-added"));
        }

        return Entry(record, ChangeKind.Added, reasons, details);
    }

    /// <summary>The change entry of a modified record, or null when nothing changed.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="before">The base record.</param>
    /// <param name="after">The head record.</param>
    /// <param name="addedIds">The ids the head adds.</param>
    /// <param name="removedIds">The ids the head removes.</param>
    /// <param name="byId">The change entries by record id.</param>
    /// <returns>The change entry of the modified record, or null when nothing changed.</returns>
    static IntentChange? ModifiedEntry(
        ProjectModel baseModel,
        ProjectModel headModel,
        Record before,
        Record after,
        HashSet<string> addedIds,
        HashSet<string> removedIds,
        Dictionary<string, IntentChange> byId)
    {
        var details = new List<string>();
        var reasons = new List<BreakingReason>();
        var approved = Approved(baseModel, before);

        if (before.Type != after.Type)
        {
            details.Add($"Type changed from {before.Type} to {after.Type}.");
            return Entry(after, ChangeKind.Modified, reasons, details);
        }

        CompareStatus(before, after, details);
        CompareQuestions(before, after, details);

        switch (before.Type)
        {
            case RecordTypes.Requirement:
                RequirementDiffer.CompareRequirement(before, after, approved, details, reasons);
                break;

            case RecordTypes.Journey:
                JourneyDiffer.CompareJourney(before, after, details);
                break;

            case RecordTypes.Decision:
                DecisionDiffer.CompareDecision(before, after, details);
                break;

            case RecordTypes.Rule:
                RuleDiffer.CompareRule(before, after, approved, details, reasons);
                break;

            case RecordTypes.Result:
                ResultDiffer.CompareResult(before, after, approved, details, reasons);
                break;

            case RecordTypes.Scenario:
                ScenarioDiffer.CompareScenario(before, after, details);
                break;

            case RecordTypes.Aggregate:
                AggregateDiffer.CompareAggregate(before, after, baseModel, headModel, approved, details, reasons);
                break;

            case RecordTypes.UseCase:
                UseCaseDiffer.CompareUseCase(before, after, details);
                UseCaseDiffer.EnrichUseCase(baseModel, headModel, before, addedIds, removedIds, byId, details, reasons);
                break;

            default:
                DiffValues.CompareFields(before, after, details);
                break;
        }

        return details.Count == 0 ? null : Entry(after, ChangeKind.Modified, reasons, details);
    }

    /// <summary>One change entry from its record, change kind, reasons, and details.</summary>
    /// <param name="record">The changed record.</param>
    /// <param name="change">The change kind.</param>
    /// <param name="reasons">The breaking reasons.</param>
    /// <param name="details">The detail lines.</param>
    /// <returns>One change entry.</returns>
    static IntentChange Entry(
        Record record, ChangeKind change, List<BreakingReason> reasons, List<string> details) =>
        new(
            record.Id,
            record.Title,
            record.Type,
            IntentDiff.FamilyOf(record.Type),
            Qualifier(record),
            change,
            reasons.Count > 0,
            reasons,
            details,
            IntentDiff.ViewOf(record));

    /// <summary>The parenthetical after a change entry's title, such as a kind or an outcome.</summary>
    /// <param name="record">The changed record.</param>
    /// <returns>The parenthetical after the change entry's title.</returns>
    static string Qualifier(Record record) => record.Type switch
    {
        RecordTypes.Requirement => record.Field(RecordFields.Kind) is { } kind ? $" ({kind})" : "",
        RecordTypes.Rule => record.Field(RecordFields.Kind) is { } kind ? $" ({kind})" : "",
        RecordTypes.UseCase => record.Field(RecordFields.Kind) is { } kind ? $" ({kind})" : "",
        RecordTypes.Result => record.Field(RecordFields.Outcome) is { } outcome ? $" ({outcome})" : "",
        _ => "",
    };

    /// <summary>The removal sentence of a record.</summary>
    /// <param name="record">The removed record.</param>
    /// <returns>The removal sentence of the record.</returns>
    static string RemovedLine(Record record)
    {
        var title = record.Title;

        return record.Type switch
        {
            RecordTypes.Result when record.Field(RecordFields.Outcome) == "refusal"
                => $"Removed refusal: {title} ({record.Field(RecordFields.Code) ?? "no code"}).",
            RecordTypes.Result => $"Removed success result: {title}.",
            RecordTypes.Scenario => $"Removed scenario: {title}.",
            RecordTypes.Requirement => $"Removed {record.Field(RecordFields.Kind) ?? "requirement"} requirement: {title}.",
            RecordTypes.Rule => $"Removed {record.Field(RecordFields.Kind) ?? "rule"} rule: {title}.",
            RecordTypes.UseCase => $"Removed {record.Field(RecordFields.Kind) ?? "use case"}: {title}.",
            RecordTypes.Aggregate => $"Removed aggregate: {title}.",
            _ => $"Removed {record.Type.Replace('-', ' ')}: {title}.",
        };
    }

    /// <summary>The addition sentence of a record.</summary>
    /// <param name="baseModel">The base model.</param>
    /// <param name="headModel">The head model.</param>
    /// <param name="record">The added record.</param>
    /// <returns>The addition sentence of the record.</returns>
    static string AddedLine(ProjectModel baseModel, ProjectModel headModel, Record record)
    {
        var title = record.Title;

        if (record.Type == RecordTypes.Result && record.Field(RecordFields.Outcome) == "refusal")
        {
            var holder = Identifiers.TrySplit(record.Id, out _, out var home, out _) && home is not null
                ? ModelBuilder.UseCaseOf(headModel, home) ?? ModelBuilder.UseCaseOf(baseModel, home)
                : null;

            return holder is null
                ? $"New refusal: {title} ({record.Field(RecordFields.Code) ?? "no code"})."
                : $"New refusal: {title} ({record.Field(RecordFields.Code) ?? "no code"}) on use case \"{holder.Title}\".";
        }

        return record.Type switch
        {
            RecordTypes.Result => $"New success result: {title}.",
            RecordTypes.Scenario => $"New scenario: {title}.",
            RecordTypes.Requirement => $"New {record.Field(RecordFields.Kind) ?? "requirement"} requirement: {title}.",
            RecordTypes.Rule => $"New {record.Field(RecordFields.Kind) ?? "rule"} rule: {title}.",
            RecordTypes.UseCase => $"New {record.Field(RecordFields.Kind) ?? "use case"}: {title}.",
            _ => $"New {record.Type.Replace('-', ' ')}: {title}.",
        };
    }

    /// <summary>Compares the status of two records of one type.</summary>
    /// <param name="before">The base record.</param>
    /// <param name="after">The head record.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    static void CompareStatus(Record before, Record after, List<string> details)
    {
        var oldStatus = before.Status ?? RecordStatuses.Approved;
        var newStatus = after.Status ?? RecordStatuses.Approved;

        if (!string.Equals(oldStatus, newStatus, StringComparison.Ordinal))
        {
            details.Add($"Status changed from {oldStatus} to {newStatus}.");
        }
    }

    /// <summary>
    ///     Compares the open questions on any record, keyed by the question text.
    ///     The generic field walk skips questions, so this runs for every type.
    /// </summary>
    /// <param name="before">The base record.</param>
    /// <param name="after">The head record.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    static void CompareQuestions(Record before, Record after, List<string> details)
    {
        var oldQuestions = DiffEntries.QuestionEntries(before);
        var newQuestions = DiffEntries.QuestionEntries(after);

        if (oldQuestions is null || newQuestions is null)
        {
            if (DiffValues.Field(before, RecordFields.Questions) is not null || DiffValues.Field(after, RecordFields.Questions) is not null)
            {
                DiffValues.CompareValue("Questions", DiffValues.Field(before, RecordFields.Questions), DiffValues.Field(after, RecordFields.Questions), details);
            }

            return;
        }

        if (oldQuestions.Count == 0 && newQuestions.Count == 0)
        {
            return;
        }

        foreach (var question in oldQuestions.Keys.Where(question => !newQuestions.ContainsKey(question))
                     .OrderBy(question => question, StringComparer.Ordinal))
        {
            details.Add($"Question removed: \"{question}\".");
        }

        foreach (var question in newQuestions.Keys.Where(question => !oldQuestions.ContainsKey(question))
                     .OrderBy(question => question, StringComparer.Ordinal))
        {
            details.Add($"Question added: \"{question}\".");
        }

        foreach (var question in oldQuestions.Keys.Where(newQuestions.ContainsKey)
                     .OrderBy(question => question, StringComparer.Ordinal))
        {
            var oldEntry = oldQuestions[question];
            var newEntry = newQuestions[question];

            CompareQuestionField(question, "recommendation", oldEntry, newEntry, details);
            CompareQuestionField(question, "decideBy", oldEntry, newEntry, details);
            CompareQuestionField(question, "decider", oldEntry, newEntry, details);
            CompareQuestionField(question, "ballot", oldEntry, newEntry, details);
            CompareQuestionField(question, "answer", oldEntry, newEntry, details);
            CompareQuestionField(question, "answeredBy", oldEntry, newEntry, details);
            CompareQuestionField(question, "answeredOn", oldEntry, newEntry, details);
        }
    }

    /// <summary>Compares one field of one open question.</summary>
    /// <param name="question">The question text.</param>
    /// <param name="field">The field name.</param>
    /// <param name="oldEntry">The base question entry.</param>
    /// <param name="newEntry">The head question entry.</param>
    /// <param name="details">The detail lines a change adds to.</param>
    static void CompareQuestionField(
        string question, string field, JsonElement oldEntry, JsonElement newEntry, List<string> details)
    {
        var oldValue = DiffEntries.StringOf(oldEntry, field);
        var newValue = DiffEntries.StringOf(newEntry, field);

        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            details.Add($"Question \"{question}\" {field} changed: \"{newValue ?? "none"}\" (was: \"{oldValue ?? "none"}\").");
        }
    }

    /// <summary>Whether the record stands approved or reviewed: its effective status, else its own.</summary>
    /// <param name="model">The model holding the record's effective status.</param>
    /// <param name="record">The record.</param>
    /// <returns>True when the record stands approved or reviewed.</returns>
    static bool Approved(ProjectModel model, Record record)
    {
        if (model.EffectiveStatus.TryGetValue(record.Id, out var effective))
        {
            return effective is RecordStatuses.Approved or RecordStatuses.Reviewed;
        }

        return record.Status is null or RecordStatuses.Approved or RecordStatuses.Reviewed;
    }

    static BreakingReason Reason(string code) => new(code, code switch
    {
        "requirement-removed" => "An approved requirement no longer exists.",
        "requirement-narrowed" => "An approved requirement statement changed.",
        "quality-measure-changed" => "A quality requirement measure or target changed.",
        "refusal-added" => "A refusal was added to an approved use case.",
        "precondition-added" => "A precondition was added to an approved use case.",
        "refusal-path-added" => "A scenario proves a new refusal path.",
        "result-removed" => "A result was removed from approved intent.",
        "scenario-removed" => "A scenario was removed from approved intent.",
        "state-removed" => "A state was removed from an approved aggregate.",
        "transition-removed" => "A transition was removed from an approved aggregate.",
        "aggregate-removed" => "An approved aggregate was removed.",
        "rule-weakened" => "A rule was weakened.",
        "refusal-meaning-changed" => "A refusal meaning changed.",
        "result-outcome-changed" => "A result outcome changed.",
        _ => "Approved intent changed.",
    });
}
