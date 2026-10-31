using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>
///     Every model check: schema, identifiers, citations, gates, proof, reach, graph and tiers.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static partial class Checker
{
    /// <summary>Every finding code a known gap may name.</summary>
    public static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "record-invalid", "field-invalid", "identifier-invalid", "identifier-duplicate",
        "use-case-name-clash", "use-case-verb-unknown", "term-clash", "statement-word-invalid", "value-invalid", "use-case-noun-unknown", "refusal-code-invalid", "template-key-invalid", "setting-unit-missing", "integration-event-name-invalid", "use-case-verb-invalid", "reaction-name-invalid", "job-name-invalid", "reference-unknown", "approval-invalid",
        "owner-unknown", "decider-missing", "known-gap-expired", "gap-finding-unknown",
        "rule-name-invalid", "use-case-success-missing", "scenario-result-unknown",
        "refusal-unimplemented", "use-case-unimplemented", "proves-unreached",
        "size-type-missing", "product-count-invalid",
        "use-case-journey-missing",
        "use-case-scenario-missing", "requirement-fulfillment-missing",
        "requirement-cites-missing", "constraint-unreached",
        "aggregate-name-clash", "decision-decides-missing", "decision-unimplemented", "decision-basis-missing", "link-unknown", "operation-use-case-drift", "constituent-parent-missing", "alert-link-missing", "alert-threshold-drift", "quality-alert-missing", "setting-key-missing", "setting-unimplemented", "setting-key-drift", "template-unimplemented", "template-unrecorded", "template-param-drift", "dependency-undeclared", "dependency-version-drift", "dependency-license-unknown", "admission-invalid", "screen-unimplemented", "screen-offers-drift", "screen-serves-drift", "step-screen-missing", "refusal-sentence-missing", "reader-missing", "proves-duplicate",
        "behavior-unrecorded", "mark-missing", "mark-unknown", "mark-count-invalid", "mark-style-invalid",
        "runtime-value-drift", "partial-missing", "act-event-undeclared", "act-event-unimplemented", "use-case-act-missing",
        "aggregate-unimplemented", "reaction-unimplemented", "job-unimplemented",
        "external-system-unimplemented", "use-case-endpoint-missing", "effect-unimplemented", "use-case-unreached",
        "state-unreached", "transition-unknown",
        "erasure-undeclared", "calculation-table-missing",
        "reaction-trigger-unknown", "requirement-measure-missing",
        "record-link-missing", "record-parent-missing", "feature-goal-missing", "result-scenario-missing", "step-use-case-missing", "effect-loop-invalid", "consistency-cites-missing",
        "query-output-missing", "paging-undeclared", "widening-cites-missing",
        "retention-enforcement-missing", "risk-acceptance-missing", "exemption-expired",
        "setting-requirement-missing", "reservation-release-missing",
        "notification-sender-missing", "except-fallback-missing", "approval-reopened",
        "parent-reopened",
        "transcription-status-invalid", "transcription-expired",
        "job-guard-missing", "schedule-invalid",
        "write-undeclared", "result-undeclared", "effect-undeclared", "repository-unimplemented",
        "use-case-method-drift", "event-tense-invalid", "name-drift", "operation-id-duplicate", "route-invalid",
        "name-reason-missing", "collaborator-repository-invalid", "repository-read-invalid",
        "query-filter-invalid", "dependency-count-invalid", "effect-timing-invalid",
        "prose-missing", "atomic-group-duplicate", "question-expired",
    };

    /// <summary>Requirement kinds that code makes true, so some rule, use case, reaction, job, notification or setting fulfills them.</summary>
    private static readonly HashSet<string> FulfilledKinds = new(StringComparer.Ordinal)
    {
        "functional", "quality", "policy",
    };

    /// <summary>Feature lifecycles under which a requirement is not yet, or never, made true by code.</summary>
    private static readonly HashSet<string> UndeliveredStates = new(StringComparer.Ordinal)
    {
        "planned", "later", "refused",
    };

    /// <summary>Rule-name words that keep a restriction visible, so a negating statement needs no echo.</summary>
    private static readonly HashSet<string> RuleNameRestrictions = new(StringComparer.Ordinal)
    {
        "not", "no", "never", "cannot", "none", "nothing", "without", "unless", "only",
        "refused", "refuses", "refusal", "forbidden", "blocked", "rejected", "non",
    };

    /// <summary>Runs every check, partitioning findings from known gaps.</summary>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Known) Check(ProjectModel model)
    {
        var findings = new List<Finding>(model.Problems);
        var owners = Codeowners.Load(model.Config.Root);

        Time("schema", () => CheckSchema(model, findings, owners));
        Time("identifiers", () => CheckIdentifiers(model, findings));
        Time("verbs", () => CheckUseCaseVerb(model, findings));
        Time("names", () => CheckReactionAndJobNames(model, findings));
        Time("lookalikes", () => CheckTermLookalikes(model, findings));
        Time("wording", () => CheckStatementWording(model, findings));
        Time("language", () => CheckLanguage(model, findings));
        Time("record-names", () => CheckRecordNames(model, findings));
        Time("citations", () => CheckCitations(model, findings));
        Time("gates", () => CheckGates(model, findings));
        Time("proof", () => CheckProof(model, findings));
        Time("result-scenarios", () => CheckResultWithoutScenario(model, findings));
        Time("fulfillment", () => CheckFulfilment(model, findings));
        Time("requirement-cited", () => CheckRequirementCited(model, findings));
        Time("constraint-cited", () => CheckConstraintCited(model, findings));
        Time("parent-reopened", () => CheckParentChanged(model, findings));
        Time("links", () => CheckLinks(model, findings));
        Time("profile", () => CheckSizeTypes(model, findings));
        Time("reach", () => CheckJourneyReach(model, findings));
        Time("behavior", () => CheckBehaviorUnrecorded(model, findings));
        Time("marks", () => CheckMarks(model, findings));
        Time("screens", () => CheckScreens(model, findings));
        Time("code", () => CheckCode(model, findings));
        Time("constituents", () => CheckConstituents(model, findings));
        Time("operations", () => CheckOperations(model, findings));
        Time("setting-keys", () => CheckSettings(model, findings));
        Time("dependencies", () => CheckDependencies(model, findings));
        Time("lifecycle", () => CheckLifecycle(model, findings));
        Time("erasure", () => CheckErasure(model, findings));
        Time("retention", () => CheckRetention(model, findings));
        Time("acceptance", () => CheckRiskAcceptance(model, findings));
        Time("exemption", () => CheckExemption(model, findings));
        Time("calculations", () => CheckCalculations(model, findings));
        Time("reactions", () => CheckReactionTrigger(model, findings));
        Time("measured", () => CheckMeasured(model, findings));
        Time("questions", () => CheckQuestions(model, findings));
        Time("aged", () => CheckQuestionAged(model, findings));
        Time("orphaned", () => CheckOrphaned(model, findings));
        Time("parents", () => CheckMissingParent(model, findings));
        Time("feature-goal", () => CheckFeatureGoal(model, findings));
        Time("steps", () => CheckStepUnserved(model, findings));
        Time("loops", () => CheckEffectLoop(model, findings));
        Time("consistency", () => CheckConsistency(model, findings));
        Time("output", () => CheckQueryOutput(model, findings));
        Time("paging", () => CheckPaging(model, findings));
        Time("widening", () => CheckWidening(model, findings));
        Time("settings", () => CheckSettingDefault(model, findings));
        Time("reservations", () => CheckReservation(model, findings));
        Time("senders", () => CheckNotificationSender(model, findings));
        Time("except", () => CheckExceptFallback(model, findings));
        Time("approval", () => CheckApproval(model, findings));
        Time("transcribed", () => CheckTranscribed(model, findings));
        Time("operator", () => CheckOperatorJob(model, findings));
        Time("schedule", () => CheckSchedule(model, findings));
        Time("prose", () => CheckProse(model, findings));
        Time("groups", () => CheckManyAggregates(model, findings));

        findings.Sort((left, right) =>
        {
            // Several checks run in parallel, so findings on one line are tied
            // by code and message to print in the same order on every run.
            var path = string.Compare(left.Path, right.Path, StringComparison.Ordinal);

            if (path != 0)
            {
                return path;
            }

            var line = left.Line.CompareTo(right.Line);

            if (line != 0)
            {
                return line;
            }

            var code = string.Compare(left.Code, right.Code, StringComparison.Ordinal);
            return code != 0 ? code : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        });

        return Suppress(model, findings);
    }

    private static void Time(string name, Action run)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        run();

        Timing.Report($"[time] check-{name} {watch.Elapsed.TotalSeconds:0.0}s");
    }

    private static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Known) Suppress(
        ProjectModel model, List<Finding> findings) =>
        Suppression.Partition(
            findings,
            model.Records,
            model.Config.Root,
            DateTime.UtcNow.Date,
            home => ModelBuilder.UseCaseOf(model, home)?.Id);

    /// <summary>Whether the record lives in the behavior layer, from the one layer table.</summary>
    private static bool IsBehavior(Record record) =>
        RecordTypes.LayerByType.TryGetValue(record.Type, out var layer) && layer == "behavior";

    private static void CheckSchema(ProjectModel model, List<Finding> findings, Codeowners owners)
    {
        foreach (var record in model.Records)
        {
            // A derived generic result inherits its shape from its use case.
            if (record.Derived)
            {
                continue;
            }

            if (record.Type.Length == 0)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.File}' has no type.", record.File, record.Line, null));
                continue;
            }

            if (!RecordTypes.All.Contains(record.Type))
            {
                continue;
            }

            if (record.Id.Length == 0)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.File}' of type '{record.Type}' has no id.", record.File, record.Line, null));
            }

            if (record.Title.Length == 0)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no title.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Owner is null)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no owner.", record.File, record.Line, SubjectOf(record)));
            }
            else if (model.Config.HasOwners && !model.Config.Owners.Contains(record.Owner, StringComparer.Ordinal))
            {
                findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' names owner '{record.Owner}', which the owner list does not hold.", record.File, record.Line, SubjectOf(record)));
            }
            else if (record.Owner is { } owner && !owners.Covers(model.Config.Root, record.File, model.Config.CandidatesFor(owner)))
            {
                findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' names owner '{owner}', which belongs to no team owning its path.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Status is null)
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no status.", record.File, record.Line, SubjectOf(record)));
            }
            else if (!ValidStatus(record))
            {
                findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has status '{record.Status}' outside {StatusVocabulary(record)}.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Status == "draft")
            {
                if (record.Decider is null)
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' is a draft with no decider.", record.File, record.Line, SubjectOf(record)));
                }
                else if (model.Config.HasOwners && !model.Config.Owners.Contains(record.Decider, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' is a draft naming decider '{record.Decider}' outside the owner list.", record.File, record.Line, SubjectOf(record)));
                }

                if (record.DecideBy is null)
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' is a draft with no decide-by date.", record.File, record.Line, SubjectOf(record)));
                }
                else if (!ValidDate(record.DecideBy))
                {
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' is a draft with decide-by '{record.DecideBy}' which is no date.", record.File, record.Line, SubjectOf(record)));
                }
            }

            CheckQuestionDeciders(record, model, findings);
            CheckTypeFields(model, record, findings);
            CheckReplaces(model, record, findings);
        }
    }

    /// <summary>Whether the status fits the record layer vocabulary.</summary>
    private static bool ValidStatus(Record record)
    {
        if (record.Status is "draft" or "retired")
        {
            return true;
        }

        // Terms span both layers, so either live status fits.
        if (record.Type == "term")
        {
            return record.Status is "approved" or "reviewed";
        }

        return IsBehavior(record)
            ? record.Status == "reviewed"
            : record.Status == "approved";
    }

    /// <summary>The status words the record layer vocabulary allows.</summary>
    private static string StatusVocabulary(Record record)
    {
        if (record.Type == "term")
        {
            return "draft, approved, reviewed or retired";
        }

        return IsBehavior(record)
            ? "draft, reviewed or retired"
            : "draft, approved or retired";
    }

    /// <summary>Reports a question entry naming no decider or no decide-by date.</summary>
    private static void CheckQuestionDeciders(Record record, ProjectModel model, List<Finding> findings)
    {
        if (record.Status == "retired")
        {
            return;
        }

        var decider = false;
        var date = false;

        foreach (var entry in QuestionEntries(record))
        {
            if (!decider)
            {
                var named = Str(entry, "decider");

                if (named is not { Length: > 0 })
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' holds a question that names no decider.", record.File, record.Line, SubjectOf(record)));
                    decider = true;
                }
                else if (model.Config.HasOwners && !model.Config.Owners.Contains(named, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("owner-unknown", $"Record '{record.Id}' holds a question naming decider '{named}' outside the owner list.", record.File, record.Line, SubjectOf(record)));
                    decider = true;
                }
            }

            if (!date)
            {
                var decideBy = Str(entry, "decideBy");

                if (decideBy is not { Length: > 0 })
                {
                    findings.Add(new Finding("decider-missing", $"Record '{record.Id}' holds a question that names no decide-by date.", record.File, record.Line, SubjectOf(record)));
                    date = true;
                }
                else if (!ValidDate(decideBy))
                {
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds a question with decide-by '{decideBy}' which is no date.", record.File, record.Line, SubjectOf(record)));
                    date = true;
                }
            }

            if (decider && date)
            {
                break;
            }
        }
    }

    /// <summary>The record's question entries: objects in its questions array.</summary>
    private static IReadOnlyList<JsonElement> QuestionEntries(Record record)
    {
        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty("questions", out var questions)
            && questions.ValueKind == JsonValueKind.Array)
        {
            return questions.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    /// <summary>Whether the question entry is answered: it carries an answer or an answerer.</summary>
    private static bool Answered(JsonElement entry) =>
        Str(entry, "answer") is { Length: > 0 } || Str(entry, "answeredBy") is { Length: > 0 };

    private static void CheckTypeFields(ProjectModel model, Record record, List<Finding> findings)
    {
        switch (record.Type)
        {
            case "product":
                RequireString(record, "boundary", findings);
                RequireArray(record, "exclusions", 0, findings);
                RequireIds(record, "addresses", 1, findings);
                break;

            case "goal":
                RequireString(record, "statement", findings);
                RequireString(record, "measure", findings);
                RequireString(record, "target", findings);
                RequireString(record, "dueBy", findings);
                break;

            case "constraint":
                RequireString(record, "authority", findings);
                RequireEnum(record, "kind", ["technical", "organizational", "convention", "legal"], findings);
                RequireBoolean(record, "applies", findings);
                RequireString(record, "appliesBecause", findings);
                break;

            case "research":
                RequireArray(record, "sources", 1, findings);
                RequireString(record, "question", findings);
                RequireString(record, "conclusion", findings);
                break;

            case "decision":
                RequireArray(record, "options", 1, findings);
                RequireString(record, "reopenedWhen", findings);

                if (record.Status == "approved")
                {
                    RequireString(record, "choice", findings);
                }

                break;

            case "topic":
                RequireDate(record, "reviewBy", findings);
                break;

            case "requirement":
                RequireEnum(record, "kind", ["functional", "quality", "policy"], findings);
                RequireString(record, "statement", findings);
                RequireString(record, "rationale", findings);
                CheckSetBy(record, findings);

                if (record.Field("kind") == "policy")
                {
                    RequireArray(record, "values", 1, findings);
                }

                if (record.Field("kind") == "quality")
                {
                    RequireString(record, "stimulus", findings);
                    RequireString(record, "environment", findings);
                    RequireString(record, "response", findings);
                    RequireString(record, "measure", findings);
                    RequireString(record, "dueBy", findings);
                }

                if (record.Has("measure"))
                {
                    RequireString(record, "dueBy", findings);
                }

                if (record.Has("dueBy"))
                {
                    RequireString(record, "measure", findings);
                }

                break;

            case "problem":
                RequireIds(record, "affects", 1, findings);
                RequireString(record, "statement", findings);
                break;

            case "feature":
                RequireEnum(record, "lifecycle", ["planned", "shipped", "later", "refused"], findings);
                RequireIds(record, "solves", 1, findings);

                if (record.Field("lifecycle") == "refused")
                {
                    RequireString(record, "refusedBecause", findings);
                }

                if (record.Field("lifecycle") == "planned")
                {
                    RequireString(record, "plannedFor", findings);
                }

                if (record.Has("serves"))
                {
                    findings.Add(new Finding("field-invalid", $"Feature '{record.Id}' names serves and advances holds its goal.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case "actor":
                RequireEnum(record, "kind", ["person", "organization", "system"], findings);
                break;

            case "role":
                RequirePattern(record, "scope", @"^[a-z0-9]+(-[a-z0-9]+)*$", "kebab-case", findings);
                RequireIds(record, "allows", 1, findings);
                break;

            case "permission":
                RequireString(record, "meaning", findings);
                break;

            case "journey":
                RequireIds(record, "actor", 1, findings);
                RequireIds(record, "serves", 1, findings);
                RequireArray(record, "steps", 1, findings);
                break;

            case "module":
                RequireArray(record, "dependsOn", 0, findings);
                break;

            case "aggregate":
                // A store carries no lifecycle, so it carries no states.
                if (record.Field("kind") is not "store")
                {
                    RequireArray(record, "states", 1, findings);
                    RequireStateEntries(record, findings);
                }

                break;

            case "rule":
                RequireEnum(record, "kind", ["invariant", "precondition", "calculation", "cross-aggregate", "atomicity", "reservation", "commercial-data", "default"], findings);
                RequireString(record, "statement", findings);
                RequireIds(record, "governs", 1, findings);
                break;

            case "use-case":
                RequireEnum(record, "kind", ["command", "query"], findings);
                // A retired use case names no entry point, so only a live one must be reached.
                if (record.Status != "retired")
                {
                    RequireIds(record, "reachedThrough", 1, findings);
                }
                CheckIdempotency(record, findings);
                break;

            case "reaction":
                CheckLegacyEdges(record, ["issues", "sends"], findings);
                RequireArray(record, "effects", 1, findings);
                CheckIdempotency(record, findings);
                CheckFailurePolicy(record, findings);
                break;

            case "job":
                CheckLegacyEdges(record, ["issues"], findings);
                CheckIdempotency(record, findings);
                CheckFailurePolicy(record, findings);
                break;

            case "setting":
                CheckSetBy(record, findings);
                break;

            case "external-system":
                RequireIds(record, "actor", 1, findings);
                break;

            case "result":
                RequireEnum(record, "outcome", ["success", "refusal"], findings);
                RequireString(record, "meaning", findings);

                if (record.Field("outcome") == "refusal")
                {
                    RequireString(record, "code", findings);

                    if (record.Field("code") is { } code
                        && (code.Length == 0
                            || !Identifiers.RefusalCode().IsMatch(code)
                            || Identifiers.RefusalCode().Match(code).Value != code))
                    {
                        findings.Add(new Finding("field-invalid", $"Result '{record.Id}' has code '{code}', which is not a refusal code.", record.File, record.Line, SubjectOf(record)));
                    }
                }

                break;

            case "scenario":
                RequireString(record, "given", findings);
                RequireString(record, "when", findings);
                RequireString(record, "then", findings);
                RequireString(record, "result", findings);
                break;

            case "event":
                RequireString(record, "meaning", findings);
                RequireArray(record, "carries", 0, findings);
                break;

            case "entry-point":
                RequireEnum(record, "kind", ["browser", "api", "webhook", "device", "cli", "mcp", "served-content", "reaction", "job"], findings);
                break;

            case "screen":
                RequireIds(record, "entryPoint", 1, findings);

                if (record.Has("address") && record.Has("host"))
                {
                    findings.Add(new Finding("record-invalid", $"Screen '{record.Id}' carries address and host.", record.File, record.Line, SubjectOf(record)));
                }
                else if (!record.Has("address") && !record.Has("host"))
                {
                    findings.Add(new Finding("record-invalid", $"Screen '{record.Id}' carries neither address nor host.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case "runbook":
                RequireString(record, "trigger", findings);
                break;

            case "deviation":
                RequireString(record, "provision", findings);
                RequireString(record, "reason", findings);
                RequireDate(record, "reviewBy", findings);
                RequireString(record, "removeWhen", findings);

                if (record.Field("provision") is { } provision
                    && !IsProvisionCitation(provision))
                {
                    findings.Add(new Finding("field-invalid", $"Deviation '{record.Id}' names '{provision}', which is not a shared provision.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case "known-gap":
                RequireString(record, "check", findings);
                RequireIds(record, "subject", 1, findings);
                RequireDate(record, "endsBy", findings);
                RequireString(record, "reason", findings);

                if (record.Field("check") is { } check && !Codes.Contains(check))
                {
                    findings.Add(new Finding("gap-finding-unknown", $"Known gap '{record.Id}' names check '{check}', which no check uses.", record.File, record.Line, SubjectOf(record)));
                }

                break;

            case "term":
                if (record.Field("meaning") is not { Length: > 0 })
                {
                    findings.Add(new Finding("record-invalid", $"Term '{record.Id}' has no meaning.", record.File, record.Line, SubjectOf(record)));
                }
                else if (record.Field("meaning")!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > 25)
                {
                    findings.Add(new Finding("field-invalid", $"Term '{record.Id}' has a meaning over 25 words.", record.File, record.Line, SubjectOf(record)));
                }

                break;
        }

        CheckLinkTypes(model, record, findings);
    }

    private static void CheckLinkTypes(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in record.Raw.EnumerateObject())
        {
            if (!RecordTypes.LinkTargets.TryGetValue(property.Name, out var allowed))
            {
                continue;
            }

            foreach (var target in ModelBuilder.TargetsOf(property.Value))
            {
                var baseId = Identifiers.BaseId(target);

                if (!model.ById.TryGetValue(baseId, out var resolved))
                {
                    continue;
                }

                // A technical decision serves the requirements it names in basedOn; every
                // other basedOn names only constraints and research.
                var servesRequirement = property.Name == "basedOn"
                    && resolved.Type == "requirement"
                    && record.Type == "decision"
                    && UnderImplementationDecisions(model.Config, record.File);

                if (allowed.Count > 0 && !allowed.Contains(resolved.Type) && !servesRequirement)
                {
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' links {property.Name} to '{target}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                // A journey cites the requirements it shows; every other cites names
                // a goal, problem, feature, constraint, research or decision.
                if (property.Name == "cites" && resolved.Type == "requirement" && record.Type != "journey")
                {
                    findings.Add(new Finding("field-invalid", $"Record '{record.Id}' links cites to '{target}', which is a requirement.", record.File, record.Line, SubjectOf(record)));
                }

                CheckFragment(model, record, target, baseId, resolved, findings);
            }
        }

        if (record.Type == "journey"
            && record.Raw.TryGetProperty("steps", out var steps)
            && steps.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in steps.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (step.TryGetProperty("useCase", out var useCase)
                    && useCase.ValueKind == JsonValueKind.String
                    && useCase.GetString() is { } id
                    && model.ById.TryGetValue(id, out var resolved)
                    && resolved.Type != "use-case")
                {
                    findings.Add(new Finding("field-invalid", $"Journey '{record.Id}' steps to '{id}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
                }

                if (step.TryGetProperty("actor", out var actor)
                    && actor.ValueKind == JsonValueKind.String
                    && actor.GetString() is { } actorId
                    && model.ById.TryGetValue(actorId, out var actorRecord)
                    && actorRecord.Type != "actor")
                {
                    findings.Add(new Finding("field-invalid", $"Journey '{record.Id}' names actor '{actorId}', which is a {actorRecord.Type}.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>
    ///     Whether the link fragment names a journey step or an external-system
    ///     operation its record holds. The citation check reads only the base id,
    ///     so a missing step or operation is reported here.
    /// </summary>
    private static void CheckFragment(
        ProjectModel model, Record record, string target, string baseId, Record resolved, List<Finding> findings)
    {
        if (target == baseId)
        {
            return;
        }

        if (Identifiers.TrySplitStep(target, out _, out _))
        {
            if (resolved.Type == "journey" && !resolved.StepIds().Contains(target, StringComparer.Ordinal))
            {
                findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' serves '{target}', which names no step of the journey.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (resolved.Type == "external-system")
        {
            var operation = target[(target.IndexOf('#', StringComparison.Ordinal) + 1)..];

            if (!OperationNames(resolved).Contains(operation, StringComparer.Ordinal))
            {
                findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' calls '{target}', which names no operation of the system.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    private static IReadOnlyList<string> OperationNames(Record system)
    {
        if (system.Raw.ValueKind != JsonValueKind.Object
            || !system.Raw.TryGetProperty("operations", out var operations)
            || operations.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. operations.EnumerateArray()
                          .Where(entry => entry.ValueKind == JsonValueKind.Object
                              && entry.TryGetProperty("name", out var name)
                              && name.ValueKind == JsonValueKind.String)
                          .Select(entry => entry.GetProperty("name").GetString()!),
        ];
    }

    private static void CheckReplaces(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Type != "decision" && record.Ids("replaces").Any())
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' carries replaces and is no decision.", record.File, record.Line, SubjectOf(record)));
        }

        foreach (var target in record.Ids("replaces"))
        {
            if (model.ById.TryGetValue(target, out var resolved) && resolved.Type != record.Type)
            {
                findings.Add(new Finding("field-invalid", $"Record '{record.Id}' replaces '{target}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    private static void CheckIdentifiers(ProjectModel model, List<Finding> findings)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var recordNames = new HashSet<string>(StringComparer.Ordinal);
        var useCaseNames = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            // A derived generic result carries its use case home and name, not the grammar.
            if (record.Id.Length == 0 || record.Derived)
            {
                continue;
            }

            if (!seen.Add(record.Id))
            {
                findings.Add(new Finding("identifier-duplicate", $"Identifier '{record.Id}' is used twice.", record.File, record.Line, record.Id));
            }

            if (!Identifiers.TrySplit(record.Id, out var prefix, out var home, out var name))
            {
                // A home-parented record reports its missing home through
                // record-parent-missing, so identifier-invalid stays silent here.
                if (ReportsMissingHome(record))
                {
                    continue;
                }

                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' is not type/name or type/home.name.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (prefix != record.Type)
            {
                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' names type {prefix} on a {record.Type} record.", record.File, record.Line, SubjectOf(record)));
            }

            var wantsHome = RecordTypes.WithHome.Contains(record.Type);

            if (wantsHome && home is null)
            {
                if (!ReportsMissingHome(record))
                {
                    findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' names no home.", record.File, record.Line, SubjectOf(record)));
                }
            }
            else if (!wantsHome && home is not null)
            {
                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' holds a home and the {record.Type} type holds no home.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type == "requirement" && home is not null)
            {
                CheckRequirementTopic(record, home, findings);
            }

            if (!Identifiers.ValidName(name, record.Type == "decision") || Identifiers.AllDigitsName(name))
            {
                findings.Add(new Finding("identifier-invalid", $"Identifier '{record.Id}' breaks the name rules.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type == "aggregate" && !recordNames.Add(name))
            {
                findings.Add(new Finding("aggregate-name-clash", $"Aggregate '{record.Id}' shares its name '{name}' with another aggregate.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type == "use-case")
            {
                if (!useCaseNames.TryGetValue(name, out var grouped))
                {
                    grouped = [];
                    useCaseNames[name] = grouped;
                }

                grouped.Add(record);
            }
        }

        foreach (var clash in useCaseNames.Where(entry => entry.Value.Count > 1))
        {
            foreach (var record in clash.Value)
            {
                findings.Add(new Finding("use-case-name-clash", $"Use case '{record.Id}' shares its name '{clash.Key}' with another use case.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a requirement whose topic segment differs from its page's name.</summary>
    private static void CheckRequirementTopic(Record record, string home, List<Finding> findings)
    {
        var page = Path.GetFileNameWithoutExtension(record.File);

        if (!home.Equals(page, StringComparison.Ordinal))
        {
            findings.Add(new Finding("identifier-invalid", $"Requirement '{record.Id}' names topic '{home}', which differs from its page '{page}'.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void CheckUseCaseVerb(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == "use-case"))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(name, model.Config.PhrasalVerbs);

            if (!Declares(model.Config, verb))
            {
                findings.Add(new Finding("use-case-verb-unknown", UnknownVerbMessage(model.Config, $"Use case '{useCase.Id}'", verb), useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            // One verb, one kind: a reading verb opens only a query, and a changing verb only a command.
            var kind = KindOf(model.Config, verb);
            var declared = useCase.Field("kind");

            if ((kind == UseCaseVerbs.VerbKind.Query && declared == "command")
                || (kind == UseCaseVerbs.VerbKind.Command && declared == "query"))
            {
                findings.Add(new Finding("use-case-verb-invalid", $"Use case '{useCase.Id}' is a {declared} but '{verb}' names a {(kind == UseCaseVerbs.VerbKind.Query ? "query" : "command")}.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    private static void CheckReactionAndJobNames(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type is "reaction" or "job"))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var verb = UseCaseVerbs.VerbOf(name, model.Config.PhrasalVerbs);

            if (record.Type == "reaction")
            {
                // <verb>-<what>-on-<fact>: what it does, then the fact it answers.
                if (!Declares(model.Config, verb) || !name.Contains("-on-", StringComparison.Ordinal))
                {
                    findings.Add(new Finding("reaction-name-invalid", $"Reaction '{record.Id}' is not named <verb>-<what>-on-<fact> with a declared verb.", record.File, record.Line, SubjectOf(record)));
                }

                continue;
            }

            // A job carries the verb of the command it issues.
            var issued = record.EdgeIds("issues")
                               .Where(id => Identifiers.TrySplit(id, out _, out _, out var target) && target is not null)
                               .Select(id => { Identifiers.TrySplit(id, out _, out _, out var target); return UseCaseVerbs.VerbOf(target!, model.Config.PhrasalVerbs); })
                               .ToList();

            if (issued.Count > 0 && !issued.Contains(verb, StringComparer.Ordinal))
            {
                findings.Add(new Finding("job-name-invalid", $"Job '{record.Id}' starts with '{verb}' but issues '{string.Join("', '", issued.Distinct())}'.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Whether the baseline or the project declares the verb.</summary>
    private static bool Declares(ConsumerConfig config, string verb) =>
        UseCaseVerbs.IsDeclared(verb, config.Verbs, config.QueryVerbs, config.PhrasalVerbs);

    /// <summary>The kind a declared verb names: the baseline kind, a project query verb, or either.</summary>
    private static UseCaseVerbs.VerbKind KindOf(ConsumerConfig config, string verb) =>
        UseCaseVerbs.KindOf(verb, config.QueryVerbs);

    /// <summary>The message for an undeclared verb, naming its replacement when the verb is a retired synonym.</summary>
    private static string UnknownVerbMessage(ConsumerConfig config, string subject, string verb) =>
        config.RetiredVerbs.TryGetValue(verb, out var replacement) || UseCaseVerbs.Retired.TryGetValue(verb, out replacement)
            ? $"{subject} starts with '{verb}' and '{replacement}' replaces it."
            : $"{subject} starts with '{verb}', which the vocabulary does not declare.";

    private static void CheckCitations(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var cited = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in record.Raw.EnumerateObject())
            {
                if (property.Name is "id" or "type")
                {
                    continue;
                }

                foreach (var text in StringsOf(property.Value))
                {
                    foreach (Match match in Identifiers.Citation.Matches(text))
                    {
                        cited.Add(Identifiers.TrimCitation(match.Value));
                    }

                    foreach (Match match in Identifiers.SharedCitation().Matches(text))
                    {
                        cited.Add(match.Value);
                    }
                }
            }

            foreach (var id in cited.Order(StringComparer.Ordinal))
            {
                if (!Resolves(model, id))
                {
                    findings.Add(new Finding("reference-unknown", $"Record '{record.Id}' cites '{id}', which no record holds.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }

        var texts = new List<(string File, string Text, int StartLine, string? Subject)>(model.Bodies.Count);

        // A batch review report records what its reader saw when the review ran, as an approval
        // manifest does, so a record renamed or removed since then is history, not a broken link.
        var docs = model.Config.RecordRoots.FirstOrDefault() ?? Path.Combine(model.Config.Root, "docs");
        var reviews = Path.GetFullPath(Path.Combine(docs, "proof", "reviews")) + Path.DirectorySeparatorChar;

        foreach (var body in model.Bodies)
        {
            if (Path.GetFullPath(body.File).StartsWith(reviews, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            texts.Add((body.File, body.Body, body.BodyStartLine, body.PageId));
        }

        foreach (var comment in model.Code.Comments)
        {
            texts.Add((comment.File, comment.Text, comment.Line, null));
        }

        foreach (var comment in model.ScreenComments)
        {
            texts.Add((comment.File, comment.Text, comment.Line, null));
        }

        foreach (var comment in model.TestComments)
        {
            texts.Add((comment.File, comment.Text, comment.Line, null));
        }

        var found = new List<Finding>();

        System.Threading.Tasks.Parallel.ForEach(
            texts,
            () => new List<Finding>(),
            (text, _, local) =>
            {
                CheckTextCitations(model, text.File, text.Text, text.StartLine, text.Subject, local);

                return local;
            },
            local =>
            {
                lock (found)
                {
                    found.AddRange(local);
                }
            });

        findings.AddRange(found);
    }

    private static void CheckTextCitations(
        ProjectModel model,
        string file,
        string text,
        int startLine,
        string? subject,
        List<Finding> findings)
    {
        var cited = new Dictionary<string, int>(StringComparer.Ordinal);
        var starts = LineStarts(text);

        void Collect(MatchCollection matches, Func<string, string> clean)
        {
            foreach (Match match in matches)
            {
                cited.TryAdd(clean(match.Value), startLine + LineOf(starts, match.Index) - 1);
            }
        }

        // A link target is a file path, so a path that starts with a record type's name, such as
        // product/brief.md, is not a citation. Blanking the targets keeps every offset in place.
        var prose = LinkTarget().Replace(text, match => "](" + new string(' ', match.Length - 3) + ")");

        Collect(Identifiers.Citation.Matches(prose), Identifiers.TrimCitation);
        Collect(Identifiers.SharedCitation().Matches(prose), value => value);

        foreach (var entry in cited.OrderBy(left => left.Value).ThenBy(left => left.Key, StringComparer.Ordinal))
        {
            if (!Resolves(model, entry.Key))
            {
                findings.Add(new Finding("reference-unknown", $"Citation '{entry.Key}' resolves to no record.", file, entry.Value, subject));
            }
        }
    }

    private static bool Resolves(ProjectModel model, string id)
    {
        if (model.ById.ContainsKey(id))
        {
            return true;
        }

        if (id.StartsWith("litespec/", StringComparison.Ordinal))
        {
            return model.Provisions.Count == 0 || model.Provisions.Contains(id);
        }

        return false;
    }

    private static void CheckGates(ProjectModel model, List<Finding> findings)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // No record auto-approves: only a question does, after its decide-by date
        // plus the veto window. Every draft decision holds what it decides.
        var drafts = model.Records
            .Where(record => record.Type == "decision" && record.Status == "draft")
            .SelectMany(decision => decision.Ids("decides"))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if ((record.Status == "approved" || record.Status == "reviewed") && drafts.Contains(record.Id))
            {
                findings.Add(new Finding("approval-invalid", $"Record '{record.Id}' is {record.Status} while its decision is a draft.", record.File, record.Line, SubjectOf(record)));
            }
        }

        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == "decision" && candidate.Status != "retired" && !UnderImplementationDecisions(model.Config, candidate.File)))
        {
            if (!decision.Ids("decides").Any())
            {
                findings.Add(new Finding("decision-decides-missing", $"Decision '{decision.Id}' names nothing it decides.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }

        foreach (var decision in model.Records.Where(candidate =>
            candidate.Type == "decision" && candidate.Status != "retired" && UnderImplementationDecisions(model.Config, candidate.File)))
        {
            if (!decision.Ids("basedOn").Any())
            {
                findings.Add(new Finding("decision-basis-missing", $"Decision '{decision.Id}' names no constraint or requirement in basedOn.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }

        foreach (var gap in model.Records.Where(record => record.Type == "known-gap"))
        {
            if (gap.Field("endsBy") is { } endsBy
                && IsoDate(endsBy, out var ends)
                && today > ends)
            {
                findings.Add(new Finding("known-gap-expired", $"Known gap '{gap.Id}' stands past its end {endsBy}.", gap.File, gap.Line, SubjectOf(gap)));
            }
        }

        foreach (var rule in model.Records.Where(record => record.Type == "rule"))
        {
            if (rule.Field("statement") is not { } statement)
            {
                continue;
            }

            if (!Identifiers.TrySplit(rule.Id, out _, out _, out var name))
            {
                continue;
            }

            // Any word that restricts keeps the negation: "no", "never" and "refused" say it
            // directly, and "only" or a uniqueness "one ... per" states a limit a reader cannot miss.
            var words = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (words.Any(RuleNameRestrictions.Contains) || (words.Contains("one") && words.Contains("per")))
            {
                continue;
            }

            var match = Regex.Match(statement, @"\b(not|no|never|cannot)\b", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                findings.Add(new Finding("rule-name-invalid", $"Rule '{rule.Id}' says '{match.Value.ToLowerInvariant()}' but its name keeps no restricting word.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }
    }

    private static void CheckFulfilment(ProjectModel model, List<Finding> findings)
    {
        var fulfilled = model.Records
            .Where(record => record.Type is "rule" or "use-case" or "reaction" or "job" or "notification" or "setting")
            .SelectMany(record => record.Ids("fulfills"))
            .ToHashSet(StringComparer.Ordinal);

        var served = model.Records
            .Where(record => record.Type == "decision" && UnderImplementationDecisions(model.Config, record.File))
            .SelectMany(record => record.Ids("basedOn"))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var requirement in model.Records.Where(record => record.Type == "requirement" && record.Status != "retired"))
        {
            if (!FulfilledKinds.Contains(requirement.Field("kind") ?? ""))
            {
                continue;
            }

            var undelivered = requirement.Ids("cites").Any(id =>
                model.ById.TryGetValue(id, out var cited)
                && cited.Type == "feature"
                && UndeliveredStates.Contains(cited.Field("lifecycle") ?? ""));

            // A quality requirement is also fulfilled when a technical decision serves
            // it: the decision names it in basedOn instead of a rule or use case.
            var decided = requirement.Field("kind") == "quality" && served.Contains(requirement.Id);

            if (!fulfilled.Contains(requirement.Id) && !undelivered && !decided)
            {
                findings.Add(new Finding("requirement-fulfillment-missing", $"Requirement '{requirement.Id}' is fulfilled by no rule, use case, reaction, job, notification or setting.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
        }
    }

    /// <summary>Reports a requirement naming no reason in cites: tier A, tier G when it carries risk.</summary>
    private static void CheckRequirementCited(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == "requirement" && record.Status != "retired"))
        {
            if (requirement.Ids("cites").Count == 0)
            {
                findings.Add(Tiered("requirement-cites-missing", $"Requirement '{requirement.Id}' cites no goal, problem, feature, constraint, research or decision.", requirement, model));
            }
        }
    }

    /// <summary>Reports an applying constraint no requirement cites.</summary>
    private static void CheckConstraintCited(ProjectModel model, List<Finding> findings)
    {
        var cited = model.Records
            .Where(record => record.Type == "requirement" && record.Status != "retired")
            .SelectMany(record => record.Ids("cites"))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var constraint in model.Records.Where(record => record.Type == "constraint" && record.Status != "retired"))
        {
            if (constraint.Raw.ValueKind == JsonValueKind.Object
                && constraint.Raw.TryGetProperty("applies", out var applies)
                && applies.ValueKind == JsonValueKind.True
                && !cited.Contains(constraint.Id))
            {
                findings.Add(new Finding("constraint-unreached", $"Constraint '{constraint.Id}' applies and is cited by no requirement.", constraint.File, constraint.Line, SubjectOf(constraint)));
            }
        }
    }

    /// <summary>
    ///     Whether a refusal code matches a template the project composes at run time and its key is a literal in
    ///     the code, the way a commit answers a lost uniqueness key from the key's scope.
    /// </summary>
    private static bool IsComposedFromLiteral(ProjectModel model, string code) =>
        model.Config.RefusalComposed.Any(template => RefusalTemplate.TryMatch(template, code, out var key) && model.Code.KeyLiterals.Contains(key));

    private static void CheckProof(ProjectModel model, List<Finding> findings)
    {
        var results = model.Records.Where(record => record.Type == "result").ToList();
        var thrown = model.Code.Literals.Select(literal => literal.Value).ToHashSet(StringComparer.Ordinal);

        foreach (var useCase in model.Records.Where(record => record.Type == "use-case"))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
            {
                continue;
            }

            var own = results.Where(result =>
                Identifiers.TrySplit(result.Id, out _, out var home, out _) && home == name).ToList();

            if (!own.Any(result => result.Field("outcome") == "success"))
            {
                findings.Add(new Finding("use-case-success-missing", $"Use case '{useCase.Id}' has no success result.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (model.Config.Size == "large"
                && !model.Records.Any(record => record.Type == "scenario"
                    && Identifiers.TrySplit(record.Id, out _, out var scenarioHome, out _) && scenarioHome == name))
            {
                findings.Add(new Finding("use-case-scenario-missing", $"Use case '{useCase.Id}' has no scenario.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (model.Status.TryGetValue(useCase.Id, out var status) && !status.Implemented)
            {
                findings.Add(new Finding("use-case-unimplemented", $"Use case '{useCase.Id}' has no handler, reaction or job.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }

        foreach (var scenario in model.Records.Where(record => record.Type == "scenario"))
        {
            var resultId = scenario.Ids("result").FirstOrDefault();

            if (resultId is null)
            {
                continue;
            }

            if (!model.ById.TryGetValue(resultId, out var result) || result.Type != "result")
            {
                continue;
            }

            if (!ResultBelongsToScenario(model, scenario, result))
            {
                findings.Add(new Finding("scenario-result-unknown", $"Scenario '{scenario.Id}' ends in '{resultId}' outside its use case.", scenario.File, scenario.Line, SubjectOf(scenario)));
            }
        }

        // A derived generic refusal is enforced by the access pipeline and input
        // validation, never thrown by handler code, so only written refusals count.
        foreach (var result in results.Where(left => !left.Derived && left.Field("outcome") == "refusal"))
        {
            if (result.Field("code") is { } code && !thrown.Contains(code) && !IsComposedFromLiteral(model, code))
            {
                findings.Add(new Finding("refusal-unimplemented", $"Result '{result.Id}' names code '{code}', which no code throws.", result.File, result.Line, SubjectOf(result)));
            }
        }

        var provesByClaim = model.Proves
            .GroupBy(link => (link.Scenario, link.Claim))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var test in model.Tests)
        {
            foreach (var claim in test.Claims)
            {
                // Only scenario and rule proofs confirm through the test's steps,
                // messages and effects; a quality requirement, a technical decision,
                // an external system, an integration event, or a provision stands
                // proven by its passing run instead.
                if (!IsBehaviorClaim(claim))
                {
                    continue;
                }

                if (provesByClaim.TryGetValue((test.Name, claim), out var link) && !link.Confirmed)
                {
                    findings.Add(new Finding("proves-unreached", $"Scenario '{test.Name}' claims '{claim}' but carries no step, message or effect that proves it.", test.File, test.Line, claim));
                }
            }
        }
    }

    /// <summary>Whether the claim confirms through the test body: a scenario or a rule.</summary>
    private static bool IsBehaviorClaim(string claim) =>
        claim.StartsWith("scenario/", StringComparison.Ordinal)
        || claim.StartsWith("rule/", StringComparison.Ordinal);

    /// <summary>
    ///     Whether the scenario ends where it may: a reaction scenario in a result of
    ///     the use case its reaction issues, any other scenario in a result of its own
    ///     use case, written or derived, both named result/<use-case>.<name>.
    /// </summary>
    private static bool ResultBelongsToScenario(ProjectModel model, Record scenario, Record result)
    {
        if (scenario.PageId is not null
            && model.ById.TryGetValue(scenario.PageId, out var page)
            && page.Type == "reaction"
            && page.EdgeIds("issues").FirstOrDefault() is { } issued
            && model.ById.TryGetValue(issued, out var target)
            && target.Type == "use-case"
            && Identifiers.TrySplit(issued, out _, out _, out var operation))
        {
            return Identifiers.TrySplit(result.Id, out _, out var home, out _)
                && home == operation;
        }

        var scenarioHome = Identifiers.TrySplit(scenario.Id, out _, out var left, out _) ? left : null;

        var resultHome = Identifiers.TrySplit(result.Id, out _, out var right, out _) ? right : null;

        return scenarioHome is not null && scenarioHome == resultHome;
    }

    private static void CheckLinks(ProjectModel model, List<Finding> findings)
    {
        var found = new System.Collections.Concurrent.ConcurrentBag<Finding>();

        Parallel.ForEach(model.Bodies, body =>
        {
            var text = WithoutCode(body.Body);
            var starts = LineStarts(text);
            var folder = Path.GetDirectoryName(body.File) ?? ".";

            foreach (Match match in MarkdownLink().Matches(text))
            {
                var target = match.Groups["target"].Value.Trim('<', '>');

                if (target.Length == 0 || target.StartsWith('#') || UriScheme().IsMatch(target))
                {
                    continue;
                }

                var path = Uri.UnescapeDataString(target.Split('#')[0].Split('?')[0]);
                var resolved = Path.GetFullPath(Path.Combine(folder, path.Replace('/', Path.DirectorySeparatorChar)));

                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    found.Add(new Finding("link-unknown", $"Link '{target}' resolves to no file.", body.File, body.BodyStartLine + LineOf(starts, match.Index) - 1, body.PageId));
                }
            }
        });

        findings.AddRange(found);
    }

    /// <summary>Blanks fenced and inline code, keeping offsets and line breaks, so links in examples are not read.</summary>
    private static string WithoutCode(string text)
    {
        static string Blank(Match match) => Regex.Replace(match.Value, @"[^\r\n]", " ");

        var fenced = FencedCode().Replace(text, Blank);
        return InlineCode().Replace(fenced, Blank);
    }

    [GeneratedRegex(@"!?\[[^\]\r\n]*\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9+.-]*:")]
    private static partial Regex UriScheme();

    [GeneratedRegex(@"\]\([^)\s]*\)")]
    private static partial Regex LinkTarget();

    [GeneratedRegex(@"(?ms)^[ \t]*(```|~~~).*?^[ \t]*\1[^\r\n]*$")]
    private static partial Regex FencedCode();

    [GeneratedRegex(@"`[^`\r\n]*`")]
    private static partial Regex InlineCode();

    private static void CheckSizeTypes(ProjectModel model, List<Finding> findings)
    {
        var declaration = Path.Combine(model.Config.Root, "litespec.project.json");
        var required = model.Config.Size == "small"
            ? RecordTypes.SmallRequired
            : RecordTypes.SmallRequired.Concat(RecordTypes.LargeAdded).ToList();

        foreach (var type in required.Concat(model.Config.OptionalTypes).Distinct(StringComparer.Ordinal))
        {
            if (type is "runbook" && !model.Config.OptionalTypes.Contains(type, StringComparer.Ordinal))
            {
                continue;
            }

            if (RecordTypes.All.Contains(type) && !model.Records.Any(record => record.Type == type))
            {
                findings.Add(new Finding("size-type-missing", $"Record type '{type}' is required by the {model.Config.Size} size and is held by no record.", declaration, 1, null));
            }
        }

        if (model.Records.Count(record => record.Type == "product") != 1)
        {
            findings.Add(new Finding("product-count-invalid", "Product 'product' has no single record.", declaration, 1, null));
        }
    }

    private static void CheckJourneyReach(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.Size == "small")
        {
            return;
        }

        foreach (var useCase in model.Records.Where(record => record.Type == "use-case"))
        {
            if (ModelBuilder.BackgroundOnly(model, useCase))
            {
                continue;
            }

            var servesStep = useCase.Ids("serves")
                .Any(id => Identifiers.TrySplitStep(id, out _, out _));

            if (!servesStep)
            {
                findings.Add(new Finding("use-case-journey-missing", $"Use case '{useCase.Id}' sits in no journey.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    private static void CheckBehaviorUnrecorded(ProjectModel model, List<Finding> findings)
    {
        // A refusal result declares the code a caller sees. A rule whose refusal is a bare code
        // declares a domain guard that no caller reaches, such as one a validator pre-empts.
        var declared = model.Records
            .Where(record => record.Type == "result" && record.Field("outcome") == "refusal")
            .Select(record => record.Field("code"))
            .Concat(model.Records
                .Where(record => record.Type == "rule")
                .Select(record => record.Field("refusal"))
                .Where(refusal => refusal is not null && !refusal.StartsWith("result/", StringComparison.Ordinal)))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var literal in model.Code.Literals.Where(literal => literal.Declares))
        {
            if (!declared.Contains(literal.Value))
            {
                findings.Add(new Finding("behavior-unrecorded", $"Code '{literal.Value}' is declared by no refusal result.", literal.File, literal.Line, null));
            }
        }

        CheckUnmatchedCalls(model, findings);
    }

    /// <summary>Whether the id names a use case record.</summary>
    private static bool IsUseCase(ProjectModel model, string id) =>
        model.ById.TryGetValue(id, out var record) && record.Type == "use-case";

    private static void CheckUnmatchedCalls(ProjectModel model, List<Finding> findings)
    {
        if (model.Screens.Count == 0 || model.Config.Shape == "client-only")
        {
            return;
        }

        var byRoute = new Dictionary<string, string>(StringComparer.Ordinal);
        var byOperation = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var endpoint in model.Code.Endpoints)
        {
            if (endpoint.Operation.Length > 0)
            {
                byOperation.TryAdd(Naming.OperationKey(endpoint.Operation), endpoint.UseCaseId);
            }

            if (endpoint.Method.Length > 0 && endpoint.Route.Length > 0)
            {
                byRoute.TryAdd($"{endpoint.Method} {Template(endpoint.Route)}", endpoint.UseCaseId);
            }
        }

        foreach (var screen in model.Screens.OrderBy(left => left.File, StringComparer.Ordinal))
        {
            foreach (var operation in screen.Operations.OrderBy(left => left.Name, StringComparer.Ordinal))
            {
                if (byRoute.ContainsKey($"{operation.Method} {Template(operation.Path)}")
                    || byOperation.ContainsKey(Naming.OperationKey(operation.Name)))
                {
                    continue;
                }

                findings.Add(new Finding(
                    "behavior-unrecorded",
                    $"Screen '{screen.Frontend} {screen.Route}' calls '{operation.Name}', which matches no endpoint.",
                    ScreenFile(model, screen.File),
                    1,
                    screen.ScreenId)
                {
                    Reader = screen.Reader,
                    ReaderIsQualified = screen.Reader == Finding.TypeScriptReader ? model.TypeScriptQualified : true,
                });
            }
        }
    }

    /// <summary>The route template with parameter constraints removed, so `{id:guid}` reads `{id}`.</summary>
    private static string Template(string route) => TemplateConstraint().Replace(route, "{$1}");

    [GeneratedRegex(@"\{(?<name>[^}:]+):[^}]+\}")]
    private static partial Regex TemplateConstraint();

    /// <summary>The screen's absolute file: screens keep the path relative to the consumer root.</summary>
    private static string ScreenFile(ProjectModel model, string file) =>
        Path.IsPathRooted(file)
            ? file
            : Path.GetFullPath(Path.Combine(model.Config.Root, file.Replace('/', Path.DirectorySeparatorChar)));

    private static void CheckLifecycle(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == "aggregate"))
        {
            var states = StateNames(aggregate);
            var transitions = TransitionsOf(aggregate);

            if (transitions.Count == 0)
            {
                continue;
            }

            foreach (var transition in transitions)
            {
                if (transition.From != RecordReader.Creation && !states.Contains(transition.From))
                {
                    findings.Add(new Finding("transition-unknown", $"Aggregate '{aggregate.Id}' holds a transition from '{transition.From}' which is no state of the aggregate.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (!states.Contains(transition.To))
                {
                    findings.Add(new Finding("transition-unknown", $"Aggregate '{aggregate.Id}' holds a transition to '{transition.To}' which is no state of the aggregate.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (transition.By is not null && !IsUseCase(model, transition.By))
                {
                    findings.Add(new Finding("transition-unknown", $"Aggregate '{aggregate.Id}' holds a transition by '{transition.By}' which no use case record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            var targets = transitions.Select(transition => transition.To).ToHashSet(StringComparer.Ordinal);
            var sources = transitions.Select(transition => transition.From).ToHashSet(StringComparer.Ordinal);

            foreach (var state in states.Order(StringComparer.Ordinal))
            {
                if (!targets.Contains(state) && !sources.Contains(state))
                {
                    findings.Add(new Finding("state-unreached", $"Aggregate '{aggregate.Id}' holds state '{state}' that no transition reaches.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }
        }
    }

    /// <summary>One aggregate transition: the states it leaves and enters, and the use case behind it.</summary>
    sealed record AggregateTransition(string From, string To, string? By);

    /// <summary>The aggregate's transitions; entries without both states are left to the shape check.</summary>
    private static IReadOnlyList<AggregateTransition> TransitionsOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty("transitions", out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<AggregateTransition>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("from", out var from)
                && from.ValueKind == JsonValueKind.String
                && from.GetString() is { Length: > 0 } leave
                && entry.TryGetProperty("to", out var to)
                && to.ValueKind == JsonValueKind.String
                && to.GetString() is { Length: > 0 } enter)
            {
                found.Add(new AggregateTransition(
                    leave,
                    enter,
                    entry.TryGetProperty("by", out var by) && by.ValueKind == JsonValueKind.String ? by.GetString() : null));
            }
        }

        return found;
    }

    /// <summary>The aggregate's state names.</summary>
    private static IReadOnlySet<string> StateNames(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty("states", out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return value.EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() is { Length: > 0 })
            .Select(entry => entry.GetProperty("name").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>One aggregate data field: its name and classification.</summary>
    sealed record AggregateField(string Name, string Classification);

    /// <summary>The aggregate's data fields; entries without a name and classification are left to the shape check.</summary>
    private static IReadOnlyList<AggregateField> DataOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty("data", out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<AggregateField>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() is { Length: > 0 } field
                && entry.TryGetProperty("classification", out var classification)
                && classification.ValueKind == JsonValueKind.String
                && classification.GetString() is { Length: > 0 } level)
            {
                found.Add(new AggregateField(field, level));
            }
        }

        return found;
    }

    private static void CheckCalculations(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == "rule" && record.Field("kind") == "calculation"))
        {
            var covered = GovernedUseCases(model, rule)
                .SelectMany(useCase => ScenariosOf(model, useCase))
                .Any(scenario => scenario.Status != "retired" && HasExamples(scenario));

            if (!covered)
            {
                findings.Add(new Finding("calculation-table-missing", $"Rule '{rule.Id}' is a calculation with no scenario examples table.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }
    }

    /// <summary>The use cases a rule governs: directly, or through the aggregate that holds them.</summary>
    private static IReadOnlyList<Record> GovernedUseCases(ProjectModel model, Record rule)
    {
        var found = new List<Record>();

        foreach (var target in rule.Ids("governs"))
        {
            if (!model.ById.TryGetValue(target, out var governed))
            {
                continue;
            }

            if (governed.Type == "use-case")
            {
                found.Add(governed);
            }
            else if (governed.Type == "aggregate")
            {
                found.AddRange(model.Records.Where(record =>
                    record.Type == "use-case" && ModelBuilder.AggregateOf(model, record)?.Id == governed.Id));
            }
        }

        return found;
    }

    /// <summary>The scenarios on a use case page.</summary>
    private static IReadOnlyList<Record> ScenariosOf(ProjectModel model, Record useCase)
    {
        if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            return [];
        }

        return model.Records
            .Where(record => record.Type == "scenario"
                && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                && home == name)
            .ToList();
    }

    /// <summary>Whether the scenario carries a non-empty examples table.</summary>
    private static bool HasExamples(Record scenario) =>
        scenario.Raw.ValueKind == JsonValueKind.Object
        && scenario.Raw.TryGetProperty("examples", out var value)
        && value.ValueKind == JsonValueKind.Array
        && value.GetArrayLength() > 0;

    private static void CheckMeasured(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == "requirement" && record.Field("measure") is { Length: > 0 }))
        {
            var check = requirement.Field("check");

            if (check is not { Length: > 0 })
            {
                findings.Add(new Finding("requirement-measure-missing", $"Requirement '{requirement.Id}' is measured but names no check.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
            else if (!model.Measures.Any(measure => measure.Passed && MeasureMatches(measure, check)))
            {
                findings.Add(new Finding("requirement-measure-missing", $"Requirement '{requirement.Id}' names check '{check}' with no passing run.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
        }

        CheckNumberedStatement(model, findings);
    }

    /// <summary>Reports a statement naming a number with no measure, at tier A.</summary>
    private static void CheckNumberedStatement(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record =>
            record.Type == "requirement"
            && record.Status != "retired"
            && record.Field("measure") is not { Length: > 0 }
            && record.Field("statement") is { Length: > 0 } statement
            && NamesNumber(statement)))
        {
            findings.Add(new Finding("requirement-measure-missing", $"Requirement '{requirement.Id}' names a number with no measure.", requirement.File, requirement.Line, SubjectOf(requirement))
            {
                TierOverride = "A",
            });
        }
    }

    /// <summary>Whether the statement names a number: a digit outside any record citation.</summary>
    private static bool NamesNumber(string statement)
    {
        var prose = Identifiers.Citation.Replace(statement, " ");
        prose = Identifiers.SharedCitation().Replace(prose, " ");
        prose = StepFragment().Replace(prose, " ");

        return prose.Any(char.IsDigit);
    }

    [GeneratedRegex(@"#[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*")]
    private static partial Regex StepFragment();

    /// <summary>Whether the tool result is the requirement's check: the check alone, or the tool and the check.</summary>
    private static bool MeasureMatches(MeasureResult measure, string check) =>
        measure.Check.Equals(check, StringComparison.Ordinal)
        || $"{measure.Tool} {measure.Check}".Equals(check, StringComparison.Ordinal);

    private static void CheckQuestions(ProjectModel model, List<Finding> findings)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var record in model.Records)
        {
            if ((record.Status == "approved" || record.Status == "reviewed") && HasOpenQuestions(record, model.Config, today))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' is {record.Status} with open questions.", record.File, record.Line, SubjectOf(record)));
            }

            CheckQuestionRecommendation(record, findings);
        }
    }

    /// <summary>
    ///     Whether the record carries open questions: entries that are neither
    ///     answered nor auto-approved. An answered or auto-approved question is
    ///     closed, not open.
    /// </summary>
    private static bool HasOpenQuestions(Record record, ConsumerConfig config, DateOnly today) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty("questions", out var value)
        && value.ValueKind == JsonValueKind.Array
        && value.EnumerateArray().Any(entry => !Answered(entry) && !AutoApproved(entry, record, config, today));

    /// <summary>Reports an open question naming no recommendation on a record carrying no risk.</summary>
    private static void CheckQuestionRecommendation(Record record, List<Finding> findings)
    {
        if (record.Status == "retired" || record.Derived || HasRisk(record))
        {
            return;
        }

        foreach (var entry in QuestionEntries(record))
        {
            if (!Answered(entry) && Str(entry, "recommendation") is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds a question that names no recommendation.", record.File, record.Line, SubjectOf(record)));
                break;
            }
        }
    }

    /// <summary>One CODEOWNERS line: the paths it owns and the owners it names.</summary>
    sealed record CodeownerRule(Regex Matcher, IReadOnlyList<string> Owners);

    /// <summary>
    ///     The consumer CODEOWNERS file, when one exists: a record's owner, or a CODEOWNERS
    ///     handle its project owners entry lists, matches an owner of the last pattern its
    ///     path matches. Basename patterns match in any folder, directory patterns own
    ///     everything beneath, and `*`, `**` and `?` span as usual.
    /// </summary>
    sealed class Codeowners
    {
        private readonly IReadOnlyList<CodeownerRule> _rules;

        private Codeowners(IReadOnlyList<CodeownerRule> rules) => _rules = rules;

        /// <summary>Reads the CODEOWNERS file from its usual homes, or no rules when none exists.</summary>
        public static Codeowners Load(string root)
        {
            foreach (var candidate in new[] { ".github/CODEOWNERS", "CODEOWNERS", "docs/CODEOWNERS" })
            {
                var file = Path.Combine(root, candidate.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(file))
                {
                    return new Codeowners(Parse(SourceTree.Read(file)));
                }
            }

            return new Codeowners([]);
        }

        /// <summary>Whether a candidate matches an owner of the file's path, or no rule constrains the path.</summary>
        public bool Covers(string root, string file, IReadOnlyList<string> candidates)
        {
            if (_rules.Count == 0)
            {
                return true;
            }

            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            IReadOnlyList<string>? hit = null;

            foreach (var rule in _rules)
            {
                if (rule.Matcher.IsMatch(relative))
                {
                    hit = rule.Owners;
                }
            }

            return hit is null || candidates.Any(candidate => hit.Any(entry => OwnerMatches(entry, candidate)));
        }

        private static IReadOnlyList<CodeownerRule> Parse(string text)
        {
            var rules = new List<CodeownerRule>();

            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    continue;
                }

                rules.Add(new CodeownerRule(RuleMatcher(parts[0]), parts[1..]));
            }

            return rules;
        }

        private static bool OwnerMatches(string entry, string owner)
        {
            var named = entry.StartsWith('@') ? entry[1..] : entry;
            var claimed = owner.StartsWith('@') ? owner[1..] : owner;

            return named.Equals(claimed, StringComparison.OrdinalIgnoreCase)
                || named.EndsWith("/" + claimed, StringComparison.OrdinalIgnoreCase);
        }

        private static Regex RuleMatcher(string pattern)
        {
            var mine = pattern.StartsWith('/') ? pattern[1..] : pattern;
            string full;

            if (mine.EndsWith('/'))
            {
                full = $"^(?:{GlobBody(mine.TrimEnd('/'))})/";
            }
            else if (!mine.Contains('/'))
            {
                full = $"^(?:.*/)?(?:{GlobBody(mine)})$";
            }
            else if (mine.IndexOfAny(['*', '?']) < 0)
            {
                full = $"^(?:{GlobBody(mine)})(?:/.*)?$";
            }
            else
            {
                full = $"^(?:{GlobBody(mine)})$";
            }

            return new Regex(full, RegexOptions.Compiled);
        }

        /// <summary>Turns a CODEOWNERS pattern into its regex body: `**` spans folders, `*` and `?` stay inside one.</summary>
        private static string GlobBody(string pattern)
        {
            var built = new StringBuilder();

            for (var index = 0; index < pattern.Length; index++)
            {
                if (pattern[index] == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
                {
                    if (index + 2 < pattern.Length && pattern[index + 2] == '/')
                    {
                        built.Append("(?:.*/)?");
                        index += 2;
                    }
                    else
                    {
                        built.Append(".*");
                        index += 1;
                    }
                }
                else if (pattern[index] == '*')
                {
                    built.Append("[^/]*");
                }
                else if (pattern[index] == '?')
                {
                    built.Append("[^/]");
                }
                else
                {
                    built.Append(Regex.Escape(pattern[index].ToString()));
                }
            }

            return built.ToString();
        }
    }

    private static string? SubjectOf(Record record) => record.Id.Length > 0 ? record.Id : null;

    private static bool ValidDate(string text) =>
        IsoDate(text, out _)
        || DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);

    // Record dates are ISO 8601, so they parse the same under every culture.
    private static bool IsoDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool IsProvisionCitation(string text) =>
        Identifiers.SharedCitation().IsMatch(text)
        && Identifiers.SharedCitation().Match(text).Value == text;

    private static void RequireString(Record record, string field, List<Finding> findings)
    {
        if (record.Field(field) is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequireStateEntries(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty("states", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !entry.TryGetProperty("name", out var name)
                || name.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(name.GetString())
                || !entry.TryGetProperty("meaning", out var meaning)
                || meaning.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(meaning.GetString()))
            {
                findings.Add(new Finding("field-invalid", $"Aggregate '{record.Id}' holds a state without a name and a meaning.", record.File, record.Line, SubjectOf(record)));
                return;
            }
        }
    }

    private static void RequireArray(Record record, string field, int minimum, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (record.Raw.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds {field} as no array.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (value.GetArrayLength() < minimum)
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds an empty {field}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequireIds(Record record, string field, int minimum, List<Finding> findings)
    {
        var ids = record.Ids(field);

        if (!record.Has(field) || ids.Count == 0)
        {
            if (minimum > 0 || !record.Has(field))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }
    }

    private static void RequireEnum(Record record, string field, string[] values, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!values.Contains(value, StringComparer.Ordinal))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has {field} '{value}' outside {string.Join(", ", values)}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a setBy outside the decider, operator and tenant who sets the value.</summary>
    private static void CheckSetBy(Record record, List<Finding> findings)
    {
        if (record.Field("setBy") is { } setBy && setBy is not ("decider" or "operator" or "tenant"))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has setBy '{setBy}' outside decider, operator, tenant.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequireBoolean(Record record, string field, List<Finding> findings)
    {
        if (!record.Has(field))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (record.Raw.TryGetProperty(field, out var value)
            && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' holds {field} as no boolean.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequireDate(Record record, string field, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!ValidDate(value))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has {field} '{value}', which is not a date.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static void RequirePattern(Record record, string field, string pattern, string shape, List<Finding> findings)
    {
        var value = record.Field(field);

        if (value is null)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has no {field}.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!Regex.IsMatch(value, pattern))
        {
            findings.Add(new Finding("field-invalid", $"Record '{record.Id}' has {field} '{value}', which is not {shape}.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>
    ///     Flags the retired top-level edge fields on a reaction or job: edges live in
    ///     the effects array, under one grammar for every record that has edges.
    /// </summary>
    private static void CheckLegacyEdges(Record record, string[] fields, List<Finding> findings)
    {
        foreach (var field in fields)
        {
            if (record.Has(field))
            {
                findings.Add(new Finding("record-invalid", $"{Label(record.Type)} '{record.Id}' holds top-level {field} outside effects.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The idempotency scopes a key may name.</summary>
    private static readonly string[] IdempotencyScopes = ["tenant", "account", "login", "global"];

    /// <summary>A {field} placeholder inside an idempotency key template.</summary>
    static readonly Regex IdempotencyPlaceholder = new(
        @"\{(?<name>[^{}]+)\}",
        RegexOptions.CultureInvariant);

    /// <summary>
    ///     The idempotency key and scope: the key templates {field} placeholders over
    ///     the record's input field names, and the scope names one of the four tenancies.
    /// </summary>
    private static void CheckIdempotency(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty("idempotency", out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (idempotency.TryGetProperty("scope", out var scope)
            && scope.ValueKind == JsonValueKind.String
            && scope.GetString() is { } scopeName
            && !IdempotencyScopes.Contains(scopeName, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency scope '{scopeName}' outside {string.Join(", ", IdempotencyScopes)}.", record.File, record.Line, SubjectOf(record)));
        }

        if (!idempotency.TryGetProperty("key", out var key)
            || key.ValueKind != JsonValueKind.String
            || key.GetString() is not { } template)
        {
            return;
        }

        var fields = InputNames(record);
        var inScope = fields.Count > 0 ? string.Join(", ", fields) : "none";

        var placeholders = IdempotencyPlaceholder.Matches(template)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (placeholders.Count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency key '{template}' with no {{field}} placeholder and input fields in scope: {inScope}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        // A reaction or a job takes no input: its key names fields of the triggering
        // event or the run window, which the record does not list.
        if (record.Type is "reaction" or "job")
        {
            return;
        }

        foreach (var name in placeholders)
        {
            if (!fields.Contains(name, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency key '{template}' where placeholder '{name}' names no input field with input fields in scope: {inScope}.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The record's input field names, in written order.</summary>
    private static IReadOnlyList<string> InputNames(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty("input", out var input)
            || input.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. input.EnumerateArray()
                      .Where(entry => entry.ValueKind == JsonValueKind.Object
                          && entry.TryGetProperty("name", out var name)
                          && name.ValueKind == JsonValueKind.String
                          && name.GetString() is { Length: > 0 })
                      .Select(entry => entry.GetProperty("name").GetString()!),
        ];
    }

    /// <summary>An ISO 8601 duration, as the failure policy delays and retention use.</summary>
    static readonly Regex IsoDuration = new(
        @"^P(?=.*\d)(?:\d+Y)?(?:\d+M)?(?:\d+W)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    ///     The reaction or job failure policy: a retry object with attempts, delays and
    ///     backoff, or none, with an optional dead letter. Prose and malformed durations report.
    /// </summary>
    private static void CheckFailurePolicy(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty("onFailure", out var policy))
        {
            return;
        }

        if (policy.ValueKind == JsonValueKind.String)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure '{policy.GetString()}' as prose with no retry object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (policy.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure as no object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!policy.TryGetProperty("retry", out var retry))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure with no retry.", record.File, record.Line, SubjectOf(record)));
        }
        else if (retry.ValueKind == JsonValueKind.String)
        {
            if (retry.GetString() is not ("none" or "next-run"))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure retry '{retry.GetString()}' outside none, next-run or a retry object.", record.File, record.Line, SubjectOf(record)));
            }
        }
        else if (retry.ValueKind == JsonValueKind.Object)
        {
            CheckRetryPolicy(record, retry, findings);
        }
        else
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure retry as neither none nor a retry object.", record.File, record.Line, SubjectOf(record)));
        }

        if (policy.TryGetProperty("deadLetter", out var deadLetter))
        {
            CheckDeadLetter(record, deadLetter, findings);
        }
    }

    /// <summary>The retry object: attempts, ISO 8601 delays and an exponential or fixed backoff.</summary>
    private static void CheckRetryPolicy(Record record, JsonElement retry, List<Finding> findings)
    {
        if (!retry.TryGetProperty("maxAttempts", out var attempts)
            || attempts.ValueKind != JsonValueKind.Number
            || !attempts.TryGetInt32(out var count)
            || count < 1)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure retry with no positive maxAttempts.", record.File, record.Line, SubjectOf(record)));
        }

        CheckPolicyDuration(record, retry, "initialDelay", "onFailure retry initialDelay", findings);
        CheckPolicyDuration(record, retry, "maxDelay", "onFailure retry maxDelay", findings);

        if (!retry.TryGetProperty("backoff", out var backoff)
            || backoff.ValueKind != JsonValueKind.String
            || backoff.GetString() is not { } shape)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure retry with no backoff.", record.File, record.Line, SubjectOf(record)));
        }
        else if (shape is not ("exponential" or "fixed"))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure retry backoff '{shape}' outside exponential, fixed.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>One ISO 8601 duration inside the failure policy: missing or malformed reports.</summary>
    private static void CheckPolicyDuration(Record record, JsonElement holder, string field, string where, List<Finding> findings)
    {
        if (!holder.TryGetProperty(field, out var value)
            || value.ValueKind != JsonValueKind.String
            || value.GetString() is not { } text)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} with no duration.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!IsoDuration.IsMatch(text))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} '{text}', which is not an ISO 8601 duration.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>The dead letter: an ISO 8601 retention and what a person does.</summary>
    private static void CheckDeadLetter(Record record, JsonElement deadLetter, List<Finding> findings)
    {
        if (deadLetter.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure deadLetter as no object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        CheckPolicyDuration(record, deadLetter, "after", "onFailure deadLetter after", findings);

        if (!deadLetter.TryGetProperty("action", out var action)
            || action.ValueKind != JsonValueKind.String
            || action.GetString() is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds onFailure deadLetter with no action.", record.File, record.Line, SubjectOf(record)));
        }
    }

    private static IEnumerable<string> StringsOf(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String && value.GetString() is { } text)
        {
            yield return text;
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in value.EnumerateArray())
            {
                foreach (var nested in StringsOf(entry))
                {
                    yield return nested;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                foreach (var nested in StringsOf(property.Value))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>Every line's start offset, for offset-to-line reads.</summary>
    private static int[] LineStarts(string text)
    {
        var starts = new List<int> { 0 };

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                starts.Add(index + 1);
            }
        }

        return [.. starts];
    }

    /// <summary>The 1-based line holding the offset.</summary>
    private static int LineOf(int[] starts, int offset)
    {
        var found = Array.BinarySearch(starts, offset);

        return found >= 0 ? found + 1 : ~found;
    }
}
