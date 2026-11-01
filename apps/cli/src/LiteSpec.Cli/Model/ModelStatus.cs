using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     The statuses the builder computes: each use case's implemented and verified
///     state, the stale proofs, each proof target's tested state, each
///     requirement's status, each aggregate's reachable states, and each record's
///     effective status.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class ModelStatus
{
    /// <summary>Computes each use case's status, the stale proofs, the tested states, the requirement statuses, and the reachable states.</summary>
    /// <param name="model">The project model.</param>
    public static void Compute(ProjectModel model)
    {
        var implemented = new HashSet<string>(StringComparer.Ordinal);

        foreach (var link in model.Implements)
        {
            if (link.Kind is "handler" or "reaction" or "job")
            {
                implemented.Add(link.To);
            }
        }

        // An endpoint that holds its behavior itself, with no handler behind it, implements
        // its use case too: a small development or session route needs no command.
        foreach (var endpoint in model.Code.Endpoints)
        {
            implemented.Add(endpoint.UseCaseId);
        }

        var scenarios = model.Records.Where(record => record.Type == RecordTypes.Scenario).ToList();

        ComputeStale(model, scenarios);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
            {
                continue;
            }

            var own = scenarios.Where(scenario =>
                Identifiers.TrySplit(scenario.Id, out _, out var home, out _) && home == name).ToList();

            var verified = own.Count > 0;

            foreach (var scenario in own)
            {
                var links = model.Proves.Where(link => link.Claim == scenario.Id).ToList();

                if (links.Count == 0
                    || links.Any(link => !link.Confirmed || link.Passing != true)
                    || model.Stale.Contains(scenario.Id))
                {
                    verified = false;
                    break;
                }
            }

            model.Status[useCase.Id] = new UseCaseStatus(
                implemented.Contains(useCase.Id), verified);
        }

        ComputeRequirementStatus(model);
        ComputeTested(model);
        ComputeReachableStates(model);
    }

    /// <summary>
    ///     Computes each record's effective status: draft when the record is a draft, when a draft
    ///     decision decides it, or when a requirement it fulfills is effectively a draft. Reviewed
    ///     counts as live like approved. A result or a scenario takes its use case's effective status.
    /// </summary>
    /// <param name="model">The project model.</param>
    public static void ComputeAuthority(ProjectModel model)
    {
        foreach (var entry in Authority.EffectiveStatus(model.Records, record => UseCaseHolder(model, (Record)record)))
        {
            model.EffectiveStatus[entry.Key] = entry.Value;
        }
    }

    /// <summary>
    ///     The use case holding a result or scenario: the use case its home names, else
    ///     the use case page that carries it.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="record">The result or scenario record.</param>
    /// <returns>The use case holding the record, or null when none holds it.</returns>
    private static Record? UseCaseHolder(ProjectModel model, Record record)
    {
        if (Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home is not null
            && ModelBuilder.UseCaseOf(model, home) is { } named)
        {
            return named;
        }

        return record.PageId is not null
            && model.ById.TryGetValue(record.PageId, out var holder)
            && holder.Type == RecordTypes.UseCase
                ? holder
                : null;
    }

    /// <summary>
    ///     Computes the stale proofs: a scenario whose confirmed proof passes in the read run is
    ///     stale when its semantic hash or its tests' hash differs from the hashes the ledger
    ///     recorded for that run. A run passing the proof for the first time records the
    ///     current hashes instead.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="scenarios">The scenario records.</param>
    private static void ComputeStale(ProjectModel model, IReadOnlyList<Record> scenarios)
    {
        if (model.TestRun is null)
        {
            return;
        }

        var run = ProofLedger.RunOf(model.Config, model.TestRun);
        var passing = model.Proves
            .Where(link => link.Confirmed && link.Passing == true)
            .Select(link => link.Claim)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var scenario in scenarios.Where(scenario => passing.Contains(scenario.Id)))
        {
            if (model.Ledger.Stale(
                scenario.Id,
                Checker.SemanticHash(scenario),
                ProofLedger.TestHashOf(model, scenario.Id),
                run))
            {
                model.Stale.Add(scenario.Id);
            }
        }
    }

    /// <summary>
    ///     Computes each proof target's tested state: a quality requirement, a
    ///     technical decision, an external system, or an integration event is tested
    ///     when a passing test that asserts something proves it. Tested never
    ///     verifies a use case.
    /// </summary>
    /// <param name="model">The project model.</param>
    private static void ComputeTested(ProjectModel model)
    {
        foreach (var record in model.Records)
        {
            if (!IsTestedTarget(model, record))
            {
                continue;
            }

            model.Tested[record.Id] = model.Proves.Any(link =>
                link.Claim == record.Id
                && link.Confirmed
                && link.Passing == true
                && TestAsserts(model, link.Scenario));
        }
    }

    /// <summary>
    ///     Whether the proving test asserts something: a backend test with an assertion
    ///     call, or a steps test with a then step asserting through its steps class, and
    ///     a browser test with an asserted text or code.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="scenario">The proving test's name or title.</param>
    /// <returns>True when the proving test asserts something.</returns>
    private static bool TestAsserts(ProjectModel model, string scenario)
    {
        foreach (var test in model.Tests)
        {
            if (test.Name == scenario)
            {
                return test.HasAssertion || test.Steps.Any(step => step.Phase == "then");
            }
        }

        foreach (var browser in model.BrowserTests)
        {
            if (browser.Title == scenario)
            {
                return browser.Texts.Count > 0 || browser.Codes.Count > 0;
            }
        }

        return false;
    }

    /// <summary>Whether the record carries a tested state: a proof target outside verified status.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="record">The record.</param>
    /// <returns>True when the record carries a tested state.</returns>
    private static bool IsTestedTarget(ProjectModel model, Record record) => record.Type switch
    {
        RecordTypes.Requirement => record.Field(RecordFields.Kind) == "quality",
        RecordTypes.Decision => Checker.UnderImplementationDecisions(model.Config, record.File),
        RecordTypes.ExternalSystem => true,
        RecordTypes.IntegrationEvent => true,
        _ => false,
    };

    /// <summary>
    ///     Computes each requirement's status: verified when everything fulfilling it is
    ///     verified and, for a measured requirement, when its check has a passing result.
    /// </summary>
    /// <param name="model">The project model.</param>
    private static void ComputeRequirementStatus(ProjectModel model)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement))
        {
            var fulfillers = model.Records
                .Where(record => record.Type is RecordTypes.Rule or RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job or RecordTypes.Notification or RecordTypes.Setting
                    && record.Ids(RecordFields.Fulfills).Contains(requirement.Id, StringComparer.Ordinal))
                .ToList();

            var satisfied = fulfillers.Count > 0 && fulfillers.All(record => FulfillerVerified(model, record));
            bool? passed = null;

            if (requirement.Has(RecordFields.Measure))
            {
                passed = requirement.Field(RecordFields.Check) is { Length: > 0 } check
                    && model.Measures.Any(measure => measure.Passed && CheckMatches(check, measure));
            }

            model.RequirementStatus[requirement.Id] = new RequirementStatus(
                satisfied && (passed is null || passed.Value),
                passed);
        }
    }

    /// <summary>Whether a requirement's fulfiller is verified.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="fulfiller">The fulfilling record.</param>
    /// <returns>True when the fulfiller is verified.</returns>
    private static bool FulfillerVerified(ProjectModel model, Record fulfiller) => fulfiller.Type switch
    {
        RecordTypes.UseCase => model.Status.TryGetValue(fulfiller.Id, out var status) && status.Verified,
        RecordTypes.Rule or RecordTypes.Reaction or RecordTypes.Job or RecordTypes.Notification or RecordTypes.Setting
            => model.Proves.Any(link => link.Claim == fulfiller.Id && link.Confirmed && link.Passing == true),
        _ => false,
    };

    /// <summary>Whether a measure result answers the requirement's check.</summary>
    /// <param name="check">The requirement's check.</param>
    /// <param name="measure">The measure result.</param>
    /// <returns>True when a measure result answers the requirement's check.</returns>
    private static bool CheckMatches(string check, MeasureResult measure) =>
        string.Equals(check, measure.Check, StringComparison.Ordinal)
        || string.Equals(check, $"{measure.Tool} {measure.Check}", StringComparison.Ordinal)
        || check.Contains(measure.Check, StringComparison.Ordinal);

    /// <summary>
    ///     Computes each aggregate's reachable states: every state a transition leads
    ///     into, plus every state a transition leaves that nothing leads into, which
    ///     starts its lifecycle.
    /// </summary>
    /// <param name="model">The project model.</param>
    private static void ComputeReachableStates(ProjectModel model)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            var entered = new HashSet<string>(StringComparer.Ordinal);
            var left = new HashSet<string>(StringComparer.Ordinal);

            if (aggregate.Raw.ValueKind == JsonValueKind.Object
                && aggregate.Raw.TryGetProperty(RecordFields.Transitions, out var transitions)
                && transitions.ValueKind == JsonValueKind.Array)
            {
                foreach (var transition in transitions.EnumerateArray())
                {
                    if (transition.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (transition.TryGetProperty("from", out var from)
                        && from.ValueKind == JsonValueKind.String
                        && from.GetString() is { Length: > 0 } start
                        && start != RecordReader.Creation)
                    {
                        // A creation leaves no state: "-" marks that the instance did not exist yet.
                        left.Add(start);
                    }

                    if (transition.TryGetProperty("to", out var to)
                        && to.ValueKind == JsonValueKind.String
                        && to.GetString() is { Length: > 0 } end)
                    {
                        entered.Add(end);
                    }
                }
            }

            left.ExceptWith(entered);
            entered.UnionWith(left);
            model.ReachableStates[aggregate.Id] = entered;
        }
    }
}
