using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using LiteSpec.Model.Checks;
using LiteSpec.Standards;

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

    /// <summary>Every check in reporting order. Each reads the built model and adds its findings to the list it is given.</summary>
    private static readonly Action<ProjectModel, List<Finding>>[] Checks =
    [
        SchemaChecks.Run,
        IdentifierChecks.Run,
        RecordNameChecks.RunNames,
        RecordNameChecks.Run,
        CitationChecks.Run,
        GateChecks.Run,
        DepartureChecks.Run,
        ProofChecks.Run,
        ProofChecks.RunUnverified,
        ProofChecks.RunProofInvalid,
        ResultScenarioChecks.Run,
        FulfilmentChecks.Run,
        FulfilmentChecks.RunRequirementCited,
        FulfilmentChecks.RunConstraintCited,
        LinkChecks.Run,
        ExplanationChecks.Run,
        ResearchSourceChecks.Run,
        FormatChecks.Run,
        SizeTypeChecks.Run,
        ReachChecks.RunReach,
        ReachChecks.RunBehavior,
        ReachChecks.RunIntentUnreached,
        ReachChecks.RunRoleOrphan,
        ReachChecks.RunPermissionOrphan,
        ReachChecks.RunDecisionConflict,
        FolderChecks.Run,
        LocalityChecks.Run,
        PlacementChecks.Run,
        MarkChecks.Run,
        ScreenChecks.Run,
        CodeChecks.Run,
        ConstituentChecks.Run,
        OperationsChecks.Run,
        SettingChecks.Run,
        DependencyChecks.Run,
        AnalyzerSuppressionChecks.Run,
        LifecycleChecks.Run,
        GraphChecks.RunErasure,
        GraphChecks.RunRetention,
        CalculationChecks.Run,
        GraphChecks.RunReactionTrigger,
        MeasuredChecks.Run,
        GraphChecks.RunOrphaned,
        ParentChecks.Run,
        IntentChecks.Run,
        GraphChecks.RunStepUnserved,
        GraphChecks.RunEffectLoop,
        GraphChecks.RunConsistency,
        GraphChecks.RunQueryOutput,
        GraphChecks.RunPaging,
        GraphChecks.RunWidening,
        GraphChecks.RunSettingDefault,
        GraphChecks.RunReservation,
        GraphChecks.RunNotificationSender,
        GraphChecks.RunExceptFallback,
        GraphChecks.RunJobGuard,
        GraphChecks.RunSchedule,
        GraphChecks.RunProse,
        GraphChecks.RunManyAggregates,
        GraphChecks.RunAtomicGroupDrift,
        GraphChecks.RunAtomicGroupInvalid,
        GraphChecks.RunTypedInputs,
        GraphChecks.RunExercises,
        GraphChecks.RunDoors,
        ScopeChecks.Run
    ];

    /// <summary>Runs every check, partitioning findings from departures.</summary>
    /// <param name="model">The built project model to check.</param>
    /// <returns>The findings that stand and the findings departures cover, each in sorted order.</returns>
    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Covered) Check(ProjectModel model)
    {
        // The checks only read the built model, so they run in parallel, each into its
        // own list, taking the next check as a thread frees; the lists join in the order above.
        var raised = new List<Finding>[Checks.Length];

        try
        {
            Parallel.ForEach(
                Partitioner.Create(Enumerable.Range(0, Checks.Length), EnumerablePartitionerOptions.NoBuffering),
                index =>
                {
                    var own = new List<Finding>();
                    Checks[index](model, own);
                    raised[index] = own;
                });
        }
        catch (AggregateException failure) when (failure.InnerExceptions.Count > 0)
        {
            ExceptionDispatchInfo.Capture(failure.InnerExceptions[0]).Throw();
            throw;
        }

        var findings = new List<Finding>(model.Problems);

        foreach (var own in raised)
        {
            findings.AddRange(own);
        }

        // A project reports only the findings its layer claims cover.
        findings.RemoveAll(finding => !model.Config.Layers.Covers(finding.Code, model.Config.Extensions));

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

    /// <summary>Partitions findings into the ones that stand and the ones a departure covers.</summary>
    /// <param name="model">The built project model the findings belong to.</param>
    /// <param name="findings">The findings from every check, in sorted order.</param>
    /// <returns>The standing findings and the suppressed findings.</returns>
    private static (IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Covered) Suppress(
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
    ///     line names, and the codes each page governs, the union of its provisions' codes.
    ///     Built once per check, so a departure covers its provision or its page.
    /// </summary>
    /// <returns>The finding codes governed by each provision or page identifier, or an empty map when no LiteSpec home is found.</returns>
    private static Dictionary<string, IReadOnlyList<string>> CodesOfProvision()
    {
        var empty = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var home = LiteSpecHome.Root;

        if (home is null || !Directory.Exists(Path.Combine(home, "docs")))
        {
            return empty;
        }

        var governed = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var pages = new Dictionary<string, List<string>>(StringComparer.Ordinal);

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

            if (codes.Count == 0)
            {
                continue;
            }

            governed[row.Id] = codes;

            var page = row.Id.Split('#')[0];

            if (!pages.TryGetValue(page, out var pageCodes))
            {
                pageCodes = [];
                pages[page] = pageCodes;
            }

            foreach (var code in codes.Where(candidate => !pageCodes.Contains(candidate)))
            {
                pageCodes.Add(code);
            }
        }

        foreach (var (page, pageCodes) in pages)
        {
            governed[page] = pageCodes;
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
