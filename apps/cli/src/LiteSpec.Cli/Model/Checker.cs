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

        var bytes = Sha256.Hash(Encoding.UTF8.GetBytes(built.ToString()));

        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    /// <summary>Runs every check, partitioning findings from known gaps.</summary>
    /// <param name="model">The built project model to check.</param>
    /// <returns>The findings that stand and the findings known gaps suppress, each in sorted order.</returns>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Known) Check(ProjectModel model)
    {
        var findings = new List<Finding>(model.Problems);

        SchemaChecks.Run(model, findings);
        IdentifierChecks.Run(model, findings);
        RecordNameChecks.RunNames(model, findings);
        RecordNameChecks.Run(model, findings);
        CitationChecks.Run(model, findings);
        GateChecks.Run(model, findings);
        ProofChecks.Run(model, findings);
        ProofChecks.RunUnverified(model, findings);
        ProofChecks.RunProofInvalid(model, findings);
        ResultScenarioChecks.Run(model, findings);
        FulfilmentChecks.Run(model, findings);
        FulfilmentChecks.RunRequirementCited(model, findings);
        FulfilmentChecks.RunConstraintCited(model, findings);
        LinkChecks.Run(model, findings);
        ExplanationChecks.Run(model, findings);
        ResearchSourceChecks.Run(model, findings);
        FormatChecks.Run(model, findings);
        SizeTypeChecks.Run(model, findings);
        ReachChecks.RunReach(model, findings);
        ReachChecks.RunBehavior(model, findings);
        ReachChecks.RunIntentUnreached(model, findings);
        ReachChecks.RunRoleOrphan(model, findings);
        ReachChecks.RunPermissionOrphan(model, findings);
        ReachChecks.RunDecisionConflict(model, findings);
        FolderChecks.Run(model, findings);
        LocalityChecks.Run(model, findings);
        PlacementChecks.Run(model, findings);
        MarkChecks.Run(model, findings);
        ScreenChecks.Run(model, findings);
        CodeChecks.Run(model, findings);
        ConstituentChecks.Run(model, findings);
        OperationsChecks.Run(model, findings);
        SettingChecks.Run(model, findings);
        DependencyChecks.Run(model, findings);
        AnalyzerSuppressionChecks.Run(model, findings);
        LifecycleChecks.Run(model, findings);
        GraphChecks.RunErasure(model, findings);
        GraphChecks.RunRetention(model, findings);
        CalculationChecks.Run(model, findings);
        GraphChecks.RunReactionTrigger(model, findings);
        MeasuredChecks.Run(model, findings);
        GraphChecks.RunOrphaned(model, findings);
        ParentChecks.Run(model, findings);
        IntentChecks.Run(model, findings);
        GraphChecks.RunStepUnserved(model, findings);
        GraphChecks.RunEffectLoop(model, findings);
        GraphChecks.RunConsistency(model, findings);
        GraphChecks.RunQueryOutput(model, findings);
        GraphChecks.RunPaging(model, findings);
        GraphChecks.RunWidening(model, findings);
        GraphChecks.RunSettingDefault(model, findings);
        GraphChecks.RunReservation(model, findings);
        GraphChecks.RunNotificationSender(model, findings);
        GraphChecks.RunExceptFallback(model, findings);
        GraphChecks.RunJobGuard(model, findings);
        GraphChecks.RunSchedule(model, findings);
        GraphChecks.RunProse(model, findings);
        GraphChecks.RunManyAggregates(model, findings);
        GraphChecks.RunAtomicGroupDrift(model, findings);
        GraphChecks.RunAtomicGroupInvalid(model, findings);
        GraphChecks.RunTypedInputs(model, findings);
        GraphChecks.RunExercises(model, findings);
        GraphChecks.RunDoors(model, findings);
        ScopeChecks.Run(model, findings);

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

                if (FindingCodes.IsKnown(code) && !codes.Contains(code))
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
