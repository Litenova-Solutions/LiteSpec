using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using LiteSpec.Model.Checks;

namespace LiteSpec.Model;

/// <summary>
///     Every model check: schema, identifiers, citations, gates, proof, reach, graph and levels.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class Checker
{
    /// <summary>Every finding code an exception may name.</summary>
    public static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "record-invalid", "folder-invalid", "module-undeclared", "record-placement-invalid", "identifier-folder-drift", "name-duplicate", "decision-number-duplicate", "link-locality-invalid", "module-dependency-invalid", "event-publisher-invalid", "intent-unreached", "role-orphaned", "permission-orphaned", "decision-decides-duplicate", "code-placement-drift", "field-invalid", "identifier-invalid", "identifier-duplicate",
        "template-key-invalid", "setting-unit-missing", "integration-event-name-invalid", "job-name-invalid", "reference-unknown",
        "exception-expired", "exception-finding-unknown",
        "rule-name-invalid", "use-case-success-missing", "scenario-result-unknown",
        "refusal-unimplemented", "use-case-unimplemented", "proves-unreached", "use-case-unverified", "proof-invalid",
        "size-type-missing", "product-count-invalid",
        "use-case-journey-missing",
        "use-case-scenario-missing", "requirement-fulfillment-missing",
        "requirement-cites-missing", "constraint-unreached",
        "decision-decides-missing", "decision-unimplemented", "decision-basis-missing", "link-unknown", "operation-use-case-drift", "constituent-parent-missing", "alert-link-missing", "alert-threshold-drift", "quality-alert-missing", "setting-key-missing", "setting-unimplemented", "setting-key-drift", "template-unimplemented", "template-unrecorded", "template-param-drift", "dependency-undeclared", "dependency-version-drift", "dependency-license-unknown", "admission-invalid", "screen-unimplemented", "screen-offers-drift", "screen-serves-drift", "step-screen-missing", "refusal-sentence-missing", "reader-missing", "proves-duplicate",
        "behavior-unrecorded", "mark-missing", "mark-unknown", "mark-count-invalid", "mark-style-invalid", "mark-folder-drift",
        "runtime-value-drift", "partial-missing", "act-event-undeclared", "act-event-unimplemented", "use-case-act-missing",
        "aggregate-unimplemented", "reaction-unimplemented", "job-unimplemented",
        "external-system-unimplemented", "use-case-endpoint-missing", "effect-unimplemented", "use-case-unreached",
        "state-unreached", "transition-unknown",
        "erasure-undeclared", "calculation-table-missing",
        "reaction-trigger-unknown", "requirement-measure-missing",
        "record-link-missing", "record-parent-missing", "result-scenario-missing", "step-use-case-missing", "effect-loop-invalid", "consistency-cites-missing",
        "query-output-missing", "paging-undeclared", "widening-cites-missing",
        "retention-enforcement-missing",
        "setting-requirement-missing", "reservation-release-missing",
        "notification-sender-missing", "except-fallback-missing",
        "job-guard-missing", "schedule-invalid", "atomic-group-drift", "atomic-group-invalid",
        "write-undeclared", "result-undeclared", "effect-undeclared", "repository-unimplemented",
        "operation-id-duplicate",
        "name-reason-missing", "collaborator-repository-invalid", "repository-read-invalid",
        "query-filter-invalid", "effect-timing-invalid",
        "prose-missing", "atomic-group-duplicate",
        "explanation-orphaned", "explanation-missing", "format-drift",
        "source-link-bare", "source-key-unknown", "source-unused",
        "problem-sufferer-invalid", "problem-evidence-missing", "actor-unused", "goal-mover-missing", "term-missing",
        "runbook-unused", "analyzer-lowering-unrecorded",
    };

    /// <summary>Fields the semantic hash skips: identity and nested entries, which carry their own hash.</summary>
    internal static readonly HashSet<string> HashExcluded = new(StringComparer.Ordinal)
    {
        "type", "id", "title", "records",
    };

    /// <summary>
    ///     The semantic hash over the record: every written field except identity
    ///     and nested entries, canonicalized and hashed to eight hex characters.
    ///     The stale-proof ledger compares it across runs.
    /// </summary>
    /// <param name="record">The record whose written fields are hashed.</param>
    /// <returns>The eight-hex-character semantic hash of the record.</returns>
    public static string SemanticHash(Record record)
    {
        var built = new StringBuilder();

        CanonicalJson.WriteTopLevel(record.Raw, built, HashExcluded);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(built.ToString()));

        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    /// <summary>Runs every check, partitioning findings from known gaps.</summary>
    /// <param name="model">The built project model to check.</param>
    /// <returns>The findings that stand and the findings known gaps suppress, each in sorted order.</returns>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Known) Check(ProjectModel model)
    {
        var findings = new List<Finding>(model.Problems);

        Time("schema", () => SchemaChecks.Run(model, findings));
        Time("identifiers", () => IdentifierChecks.Run(model, findings));
        Time("names", () => RecordNameChecks.RunNames(model, findings));
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
        Time("links", () => LinkChecks.Run(model, findings));
        Time("explanations", () => ExplanationChecks.Run(model, findings));
        Time("sources", () => ResearchSourceChecks.Run(model, findings));
        Time("format", () => FormatChecks.Run(model, findings));
        Time("profile", () => SizeTypeChecks.Run(model, findings));
        Time("reach", () => ReachChecks.RunReach(model, findings));
        Time("behavior", () => ReachChecks.RunBehavior(model, findings));
        Time("intent-unreached", () => ReachChecks.RunIntentUnreached(model, findings));
        Time("role-orphaned", () => ReachChecks.RunRoleOrphan(model, findings));
        Time("permission-orphaned", () => ReachChecks.RunPermissionOrphan(model, findings));
        Time("decision-decides-duplicate", () => ReachChecks.RunDecisionConflict(model, findings));
        Time("folders", () => FolderChecks.Run(model, findings));
        Time("locality", () => LocalityChecks.Run(model, findings));
        Time("placement", () => PlacementChecks.Run(model, findings));
        Time("marks", () => MarkChecks.Run(model, findings));
        Time("screens", () => ScreenChecks.Run(model, findings));
        Time("code", () => CodeChecks.Run(model, findings));
        Time("constituents", () => ConstituentChecks.Run(model, findings));
        Time("operations", () => OperationsChecks.Run(model, findings));
        Time("setting-keys", () => SettingChecks.Run(model, findings));
        Time("dependencies", () => DependencyChecks.Run(model, findings));
        Time("analyzer-suppressions", () => AnalyzerSuppressionChecks.Run(model, findings));
        Time("lifecycle", () => LifecycleChecks.Run(model, findings));
        Time("erasure", () => GraphChecks.RunErasure(model, findings));
        Time("retention", () => GraphChecks.RunRetention(model, findings));
        Time("calculations", () => CalculationChecks.Run(model, findings));
        Time("reactions", () => GraphChecks.RunReactionTrigger(model, findings));
        Time("measured", () => MeasuredChecks.Run(model, findings));
        Time("orphaned", () => GraphChecks.RunOrphaned(model, findings));
        Time("parents", () => ParentChecks.Run(model, findings));
        Time("intent", () => IntentChecks.Run(model, findings));
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
        Time("job-guard", () => GraphChecks.RunJobGuard(model, findings));
        Time("schedule", () => GraphChecks.RunSchedule(model, findings));
        Time("prose", () => GraphChecks.RunProse(model, findings));
        Time("groups", () => GraphChecks.RunManyAggregates(model, findings));
        Time("atomic-drift", () => GraphChecks.RunAtomicGroupDrift(model, findings));
        Time("atomic-invalid", () => GraphChecks.RunAtomicGroupInvalid(model, findings));
        Time("typed-inputs", () => GraphChecks.RunTypedInputs(model, findings));
        Time("exercises", () => GraphChecks.RunExercises(model, findings));
        Time("doors", () => GraphChecks.RunDoors(model, findings));
        Time("scope", () => ScopeChecks.Run(model, findings));

        // A project reports only the findings of the conformance level it claims.
        var claimed = model.Config.Conformance;
        findings.RemoveAll(finding => !ConformanceLevels.Runs(finding.Code, claimed));

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

    /// <summary>Runs one check phase and reports its elapsed time to the timing log.</summary>
    /// <param name="name">The phase name used in the timing line.</param>
    /// <param name="run">The check phase to run.</param>
    private static void Time(string name, Action run)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        run();

        Timing.Report($"[time] check-{name} {watch.Elapsed.TotalSeconds:0.0}s");
    }

    /// <summary>Partitions findings into the ones that stand and the ones a known gap covers.</summary>
    /// <param name="model">The built project model the findings belong to.</param>
    /// <param name="findings">The findings from every check, in sorted order.</param>
    /// <returns>The standing findings and the suppressed findings.</returns>
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
    /// <returns>The finding codes governed by each provision id, or an empty map when no LiteSpec home is found.</returns>
    private static Dictionary<string, IReadOnlyList<string>> CodesOfProvision()
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

    /// <summary>The pattern matching one backtick-quoted code span in a Checked by line.</summary>
    private static readonly Regex CodeSpan = new("`(?<code>[^`]+)`", RegexOptions.Compiled);

    /// <summary>Whether the file sits under the project's implementation decisions folder.</summary>
    /// <param name="config">The consumer configuration holding the project root.</param>
    /// <param name="file">The file path to test.</param>
    /// <returns>True when the file sits under the implementation decisions folder.</returns>
    internal static bool UnderImplementationDecisions(ConsumerConfig config, string file) =>
        MarkChecks.UnderImplementationDecisions(config, file);
}
