using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using LiteSpec.Model.Checks;

namespace LiteSpec.Model;

/// <summary>
///     Every model check: schema, identifiers, citations, gates, proof, reach, graph and tiers.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class Checker
{
    /// <summary>Every finding code an exception may name.</summary>
    public static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "record-invalid", "field-invalid", "identifier-invalid", "identifier-duplicate",
        "use-case-name-clash", "use-case-verb-unknown", "term-clash", "statement-word-invalid", "value-invalid", "use-case-noun-unknown", "refusal-code-invalid", "template-key-invalid", "setting-unit-missing", "integration-event-name-invalid", "use-case-verb-invalid", "reaction-name-invalid", "job-name-invalid", "reference-unknown", "approval-invalid",
        "owner-unknown", "decider-missing", "exception-expired", "exception-finding-unknown",
        "rule-name-invalid", "use-case-success-missing", "scenario-result-unknown",
        "refusal-unimplemented", "use-case-unimplemented", "proves-unreached", "use-case-unverified", "proof-invalid",
        "size-type-missing", "product-count-invalid",
        "use-case-journey-missing",
        "use-case-scenario-missing", "requirement-fulfillment-missing",
        "requirement-cites-missing", "constraint-unreached",
        "aggregate-name-clash", "decision-decides-missing", "decision-unimplemented", "decision-basis-missing", "link-unknown", "operation-use-case-drift", "constituent-parent-missing", "alert-link-missing", "alert-threshold-drift", "quality-alert-missing", "setting-key-missing", "setting-unimplemented", "setting-key-drift", "template-unimplemented", "template-unrecorded", "template-param-drift", "dependency-undeclared", "dependency-version-drift", "dependency-license-unknown", "admission-invalid", "screen-unimplemented", "screen-offers-drift", "screen-serves-drift", "step-screen-missing", "refusal-sentence-missing", "reader-missing", "proves-duplicate",
        "behavior-unrecorded", "mark-missing", "mark-unknown", "mark-count-invalid", "mark-style-invalid", "mark-folder-drift",
        "runtime-value-drift", "partial-missing", "act-event-undeclared", "act-event-unimplemented", "use-case-act-missing",
        "aggregate-unimplemented", "reaction-unimplemented", "job-unimplemented",
        "external-system-unimplemented", "use-case-endpoint-missing", "effect-unimplemented", "use-case-unreached",
        "state-unreached", "transition-unknown",
        "erasure-undeclared", "calculation-table-missing", "table-drift",
        "reaction-trigger-unknown", "requirement-measure-missing",
        "record-link-missing", "record-parent-missing", "feature-goal-missing", "result-scenario-missing", "step-use-case-missing", "effect-loop-invalid", "consistency-cites-missing",
        "query-output-missing", "paging-undeclared", "widening-cites-missing",
        "retention-enforcement-missing", "risk-acceptance-missing", "exemption-expired",
        "setting-requirement-missing", "reservation-release-missing",
        "notification-sender-missing", "except-fallback-missing", "approval-reopened",
        "parent-reopened",
        "transcription-status-invalid", "transcription-expired",
        "job-guard-missing", "schedule-invalid", "atomic-group-drift", "atomic-group-invalid",
        "write-undeclared", "result-undeclared", "effect-undeclared", "repository-unimplemented",
        "use-case-method-drift", "event-tense-invalid", "name-drift", "operation-id-duplicate", "route-invalid",
        "name-reason-missing", "collaborator-repository-invalid", "repository-read-invalid",
        "query-filter-invalid", "dependency-count-invalid", "effect-timing-invalid",
        "prose-missing", "atomic-group-duplicate", "question-expired",
    };

    /// <summary>Fields the semantic hash skips: identity, workflow and approval data.</summary>
    internal static readonly HashSet<string> ApprovalExcluded = new(StringComparer.Ordinal)
    {
        "type", "id", "title", "owner", "status", "decider", "decideBy",
        "approvedBy", "approvedOn", "approvedHash", "provenance",

        // A nested entry carries its own hash, so its workflow and approval data never reopen its page.
        "records",

        // A term's labels translate its name; adding a language leaves the approved meaning alone.
        "labels",
    };

    /// <summary>
    ///     The semantic hash over the record fields: every written field except identity,
    ///     workflow and approval data, canonicalized and hashed to eight hex characters.
    ///     Prose bodies sit outside the hash, so rewording prose reopens no approval.
    /// </summary>
    public static string SemanticHash(Record record) => SemanticHash(record.Raw);

    /// <summary>The semantic hash over written record fields, as <see cref="SemanticHash(Record)" /> computes it.</summary>
    public static string SemanticHash(JsonElement raw)
    {
        var built = new StringBuilder();

        CanonicalJson.WriteTopLevel(raw, built, ApprovalExcluded);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(built.ToString()));

        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    /// <summary>Runs every check, partitioning findings from known gaps.</summary>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Known) Check(ProjectModel model)
    {
        var findings = new List<Finding>(model.Problems);
        var owners = Codeowners.Load(model.Config.Root);

        Time("schema", () => SchemaChecks.Run(model, findings, owners));
        Time("identifiers", () => IdentifierChecks.Run(model, findings));
        Time("verbs", () => RecordNameChecks.RunVerbs(model, findings));
        Time("names", () => RecordNameChecks.RunNames(model, findings));
        Time("lookalikes", () => TermLookalikeChecks.Run(model, findings));
        Time("wording", () => WordingChecks.Run(model, findings));
        Time("language", () => LanguageChecks.Run(model, findings));
        Time("record-names", () => RecordNameChecks.Run(model, findings));
        Time("citations", () => CitationChecks.Run(model, findings));
        Time("gates", () => GateChecks.Run(model, findings));
        Time("proof", () => ProofChecks.Run(model, findings));
        Time("use-case-unverified", () => ProofChecks.RunUnverified(model, findings));
        Time("proof-invalid", () => ProofChecks.RunProofInvalid(model, findings));
        Time("result-scenarios", () => ResultScenarioChecks.Run(model, findings));
        Time("fulfillment", () => FulfilmentChecks.Run(model, findings));
        Time("requirement-cited", () => FulfilmentChecks.RunRequirementCited(model, findings));
        Time("constraint-cited", () => FulfilmentChecks.RunConstraintCited(model, findings));
        Time("parent-reopened", () => ParentChecks.RunParentChanged(model, findings));
        Time("links", () => LinkChecks.Run(model, findings));
        Time("profile", () => SizeTypeChecks.Run(model, findings));
        Time("reach", () => ReachChecks.RunReach(model, findings));
        Time("behavior", () => ReachChecks.RunBehavior(model, findings));
        Time("marks", () => MarkChecks.Run(model, findings));
        Time("screens", () => ScreenChecks.Run(model, findings));
        Time("code", () => CodeChecks.Run(model, findings));
        Time("constituents", () => ConstituentChecks.Run(model, findings));
        Time("operations", () => OperationsChecks.Run(model, findings));
        Time("setting-keys", () => SettingChecks.Run(model, findings));
        Time("dependencies", () => DependencyChecks.Run(model, findings));
        Time("lifecycle", () => LifecycleChecks.Run(model, findings));
        Time("erasure", () => GraphChecks.RunErasure(model, findings));
        Time("retention", () => GraphChecks.RunRetention(model, findings));
        Time("acceptance", () => GraphChecks.RunRiskAcceptance(model, findings));
        Time("exemption", () => GraphChecks.RunExemption(model, findings));
        Time("calculations", () => CalculationChecks.Run(model, findings));
        Time("tables", () => GraphChecks.RunTableDrift(model, findings));
        Time("reactions", () => GraphChecks.RunReactionTrigger(model, findings));
        Time("measured", () => MeasuredChecks.Run(model, findings));
        Time("questions", () => QuestionChecks.Run(model, findings));
        Time("aged", () => GraphChecks.RunQuestionAged(model, findings));
        Time("orphaned", () => GraphChecks.RunOrphaned(model, findings));
        Time("parents", () => ParentChecks.Run(model, findings));
        Time("feature-goal", () => FeatureChecks.Run(model, findings));
        Time("steps", () => GraphChecks.RunStepUnserved(model, findings));
        Time("loops", () => GraphChecks.RunEffectLoop(model, findings));
        Time("consistency", () => GraphChecks.RunConsistency(model, findings));
        Time("output", () => GraphChecks.RunQueryOutput(model, findings));
        Time("paging", () => GraphChecks.RunPaging(model, findings));
        Time("widening", () => GraphChecks.RunWidening(model, findings));
        Time("settings", () => GraphChecks.RunSettingDefault(model, findings));
        Time("reservations", () => GraphChecks.RunReservation(model, findings));
        Time("senders", () => GraphChecks.RunNotificationSender(model, findings));
        Time("except", () => GraphChecks.RunExceptFallback(model, findings));
        Time("approval", () => GraphChecks.RunApproval(model, findings));
        Time("transcribed", () => GraphChecks.RunTranscribed(model, findings));
        Time("job-guard", () => GraphChecks.RunJobGuard(model, findings));
        Time("schedule", () => GraphChecks.RunSchedule(model, findings));
        Time("prose", () => GraphChecks.RunProse(model, findings));
        Time("groups", () => GraphChecks.RunManyAggregates(model, findings));
        Time("atomic-drift", () => GraphChecks.RunAtomicGroupDrift(model, findings));
        Time("atomic-invalid", () => GraphChecks.RunAtomicGroupInvalid(model, findings));
        Time("saga", () => GraphChecks.RunSaga(model, findings));
        Time("typed-inputs", () => GraphChecks.RunTypedInputs(model, findings));
        Time("exercises", () => GraphChecks.RunExercises(model, findings));
        Time("doors", () => GraphChecks.RunDoors(model, findings));

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
        ProjectModel model, List<Finding> findings)
    {
        var codes = CodesOfProvision();

        return Suppression.Partition(
            findings,
            model.Records,
            model.Config.Root,
            DateTime.UtcNow.Date,
            home => ModelBuilder.UseCaseOf(model, home)?.Id,
            provision => codes.TryGetValue(provision, out var governed) ? governed : []);
    }

    /// <summary>
    ///     The finding codes each provision governs, from the codes its Checked by
    ///     line names. Built once per check, so departures cover their provision.
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CodesOfProvision()
    {
        var empty = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var home = LiteSpecHome.Root;

        if (home is null || !Directory.Exists(Path.Combine(home, "docs")))
        {
            return empty;
        }

        var governed = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var row in Provisions.Collect(home))
        {
            var codes = new List<string>();

            foreach (Match match in CodeSpan.Matches(row.Evidence))
            {
                var code = match.Groups["code"].Value;

                if (Codes.Contains(code) && !codes.Contains(code))
                {
                    codes.Add(code);
                }
            }

            if (codes.Count > 0)
            {
                governed[row.Id] = codes;
            }
        }

        return governed;
    }

    private static readonly Regex CodeSpan = new("`(?<code>[^`]+)`", RegexOptions.Compiled);

    /// <summary>Whether the file sits under the project's implementation decisions folder.</summary>
    internal static bool UnderImplementationDecisions(ConsumerConfig config, string file) =>
        MarkChecks.UnderImplementationDecisions(config, file);
}
