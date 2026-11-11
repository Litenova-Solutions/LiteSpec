namespace LiteSpec.Model;

/// <summary>
///     Computes the completion stage of every requirement, use case, journey,
///     feature and goal from proof only: the code found, the tests read, and
///     the scenarios with passing, current proof. A parent is only as far as
///     its weakest child.
/// </summary>
/// <follows>litespec/core/model#compute-completion-in-the-builder</follows>
static class CompletionCalculator
{
    /// <summary>The stage names from least to most complete.</summary>
    static readonly string[] Stages = ["unfulfilled", "specified", "coded", "verified"];

    /// <summary>Computes completion for every requirement, use case, journey, feature and goal in the model.</summary>
    /// <param name="model">The built project model, with its links, tests and ledger read.</param>
    public static void Compute(ProjectModel model)
    {
        model.Completion.Clear();

        var facts = new CompletionLookups(model);
        var coded = CodedUseCases(model);
        var stale = StaleScenarios(model);

        foreach (var requirement in facts.OfType(RecordTypes.Requirement))
        {
            model.Completion[requirement.Id] = RequirementCompletion(facts, requirement, coded, stale);
        }

        foreach (var useCase in facts.OfType(RecordTypes.UseCase))
        {
            model.Completion[useCase.Id] = UseCaseCompletion(facts, useCase, coded, stale);
        }

        foreach (var journey in facts.OfType(RecordTypes.Journey))
        {
            model.Completion[journey.Id] = JourneyCompletion(facts, journey, coded, stale);
        }

        foreach (var feature in facts.OfType(RecordTypes.Feature))
        {
            model.Completion[feature.Id] = FeatureCompletion(facts, feature, coded, stale);
        }

        foreach (var goal in facts.OfType(RecordTypes.Goal))
        {
            model.Completion[goal.Id] = GoalCompletion(facts, goal);
        }
    }

    /// <summary>Whether the model holds a test run, so proof is known.</summary>
    /// <param name="model">The built project model.</param>
    /// <returns>Known when a test run was read, else unknown.</returns>
    static string ProofOf(ProjectModel model) =>
        model.TestRun is null ? "unknown" : "known";

    /// <summary>The rank of a stage name, for weakest-child comparisons.</summary>
    /// <param name="stage">The stage name to rank.</param>
    /// <returns>0 for unfulfilled, 1 for specified, 2 for coded and 3 for verified.</returns>
    static int Rank(string stage) => Array.IndexOf(Stages, stage);

    /// <summary>
    ///     One requirement's completion. Its children are the Behavior records
    ///     fulfilling it and, for a quality requirement, a technical decision
    ///     naming it. No child is unfulfilled, a child with no code is specified,
    ///     every child with code and not every child verified is coded, and
    ///     every child verified with its measure passing is verified.
    /// </summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="requirement">The requirement whose completion is wanted.</param>
    /// <param name="coded">The use cases with code behind them.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>The requirement completion.</returns>
    static Completion RequirementCompletion(
        CompletionLookups facts, Record requirement,
        IReadOnlySet<string> coded, IReadOnlySet<string> stale)
    {
        var model = facts.Model;
        var fulfillers = facts.FulfillersOf(requirement);
        var done = fulfillers.Count(child => FulfillerCoded(facts, child, coded, stale));
        var verified = fulfillers.Count(child => FulfillerVerified(facts, child, stale));
        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["fulfillers coded"] = new CompletionCount(done, fulfillers.Count),
            ["fulfillers verified"] = new CompletionCount(verified, fulfillers.Count),
        };

        if (fulfillers.Count == 0)
        {
            return new Completion("unfulfilled", ProofOf(model), [requirement.Id], counts);
        }

        if (verified == fulfillers.Count && MeasurePassed(model, requirement))
        {
            return new Completion("verified", ProofOf(model), [], counts);
        }

        if (done == fulfillers.Count)
        {
            return new Completion(
                "coded", ProofOf(model),
                Cap(fulfillers.Where(child => !FulfillerVerified(facts, child, stale)).Select(child => child.Id), counts),
                counts);
        }

        return new Completion(
            "specified", ProofOf(model),
            Cap(fulfillers.Where(child => !FulfillerCoded(facts, child, coded, stale)).Select(child => child.Id), counts),
            counts);
    }

    /// <summary>
    ///     One use case's completion. Its children are its scenarios. No scenario
    ///     is unfulfilled, a scenario with no code behind the use case is
    ///     specified, code with not every scenario verified is coded, and
    ///     every scenario verified fresh is verified.
    /// </summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="useCase">The use case whose completion is wanted.</param>
    /// <param name="coded">The use cases with code behind them.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>The use case completion.</returns>
    static Completion UseCaseCompletion(
        CompletionLookups facts, Record useCase,
        HashSet<string> coded, IReadOnlySet<string> stale)
    {
        var model = facts.Model;
        var scenarios = facts.ScenariosOf(useCase);
        var hasCode = coded.Contains(useCase.Id);
        var verifiedScenarios = scenarios.Count(scenario => ScenarioVerified(facts, scenario, stale));
        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["code found"] = new CompletionCount(hasCode ? 1 : 0, 1),
            ["scenarios verified"] = new CompletionCount(verifiedScenarios, scenarios.Count),
        };

        if (UseCaseVerified(facts, useCase, stale))
        {
            return new Completion("verified", ProofOf(model), [], counts);
        }

        if (hasCode)
        {
            return new Completion(
                "coded", ProofOf(model),
                Cap(scenarios.Where(scenario => !ScenarioVerified(facts, scenario, stale)).Select(scenario => scenario.Id), counts),
                counts);
        }

        if (scenarios.Count > 0)
        {
            return new Completion("specified", ProofOf(model), [useCase.Id], counts);
        }

        return new Completion("unfulfilled", ProofOf(model), [useCase.Id], counts);
    }

    /// <summary>
    ///     One journey's completion. Its children are its steps. An unserved step
    ///     is unfulfilled, a served step with no code is specified, a step whose
    ///     serving code is found and that no passing proof exercises is
    ///     coded, and an exercised step is verified.
    /// </summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="journey">The journey whose completion is wanted.</param>
    /// <param name="coded">The use cases with code behind them.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>The journey completion.</returns>
    static Completion JourneyCompletion(
        CompletionLookups facts, Record journey,
        IReadOnlySet<string> coded, IReadOnlySet<string> stale)
    {
        var model = facts.Model;
        var steps = journey.StepIds();
        var served = 0;
        var done = 0;
        var exercised = 0;
        var stepStages = new List<(string Step, string Stage)>();

        foreach (var step in steps)
        {
            var stage = StepStage(facts, step, coded, stale);

            stepStages.Add((step, stage));

            if (Rank(stage) >= Rank("specified"))
            {
                served++;
            }

            if (Rank(stage) >= Rank("coded"))
            {
                done++;
            }

            if (stage == "verified")
            {
                exercised++;
            }
        }

        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["journey steps served"] = new CompletionCount(served, steps.Count),
            ["steps coded"] = new CompletionCount(done, steps.Count),
            ["steps exercised"] = new CompletionCount(exercised, steps.Count),
        };

        if (steps.Count == 0)
        {
            return new Completion("unfulfilled", ProofOf(model), [journey.Id], counts);
        }

        var weakest = stepStages.Min(entry => Rank(entry.Stage));

        return new Completion(
            Stages[weakest], ProofOf(model),
            Cap(stepStages.Where(entry => Rank(entry.Stage) == weakest).Select(entry => entry.Step), counts),
            counts);
    }

    /// <summary>
    ///     One feature's completion from the requirements citing it and the
    ///     journeys citing those requirements. The feature is only as far as
    ///     its weakest child.
    /// </summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="feature">The feature whose completion is wanted.</param>
    /// <param name="coded">The use cases with code behind them.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>The feature completion.</returns>
    static Completion FeatureCompletion(
        CompletionLookups facts, Record feature,
        IReadOnlySet<string> coded, IReadOnlySet<string> stale)
    {
        var model = facts.Model;
        var citing = facts.OfType(RecordTypes.Requirement)
            .Where(requirement => requirement.Ids(RecordFields.Cites).Contains(feature.Id, StringComparer.Ordinal))
            .ToList();
        var journeys = JourneysCiting(facts, citing);
        var steps = journeys.SelectMany(journey => journey.StepIds()).Distinct(StringComparer.Ordinal).ToList();

        var fulfilled = citing.Count(requirement => facts.FulfillersOf(requirement).Count > 0);
        var verified = citing.Count(requirement =>
            model.Completion.TryGetValue(requirement.Id, out var child) && child.Stage == "verified");
        var servedSteps = steps.Count(step => Rank(StepStage(facts, step, coded, stale)) >= Rank("specified"));
        var doneSteps = steps.Count(step => Rank(StepStage(facts, step, coded, stale)) >= Rank("coded"));
        var exercisedSteps = steps.Count(step => StepStage(facts, step, coded, stale) == "verified");

        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["requirements fulfilled"] = new CompletionCount(fulfilled, citing.Count),
            ["requirements verified"] = new CompletionCount(verified, citing.Count),
            ["journey steps served"] = new CompletionCount(servedSteps, steps.Count),
            ["steps coded"] = new CompletionCount(doneSteps, steps.Count),
            ["steps exercised"] = new CompletionCount(exercisedSteps, steps.Count),
        };

        var childStages = new List<(string Id, string Stage)>();

        foreach (var requirement in citing)
        {
            if (model.Completion.TryGetValue(requirement.Id, out var child))
            {
                childStages.Add((requirement.Id, child.Stage));
            }
        }

        foreach (var journey in journeys)
        {
            if (model.Completion.TryGetValue(journey.Id, out var child))
            {
                childStages.Add((journey.Id, child.Stage));
            }
        }

        if (childStages.Count == 0)
        {
            return new Completion(
                "unfulfilled", ProofOf(model),
                Cap([feature.Id], counts), counts);
        }

        var weakest = childStages.Min(entry => Rank(entry.Stage));
        var held = childStages
            .Where(entry => Rank(entry.Stage) == weakest)
            .Select(entry => entry.Id);

        return new Completion(Stages[weakest], ProofOf(model), Cap(held, counts), counts);
    }

    /// <summary>
    ///     One goal's completion from the features advancing it, the journeys
    ///     serving it and the requirements citing it. The goal is only as far
    ///     as its weakest child.
    /// </summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="goal">The goal whose completion is wanted.</param>
    /// <returns>The goal completion.</returns>
    static Completion GoalCompletion(CompletionLookups facts, Record goal)
    {
        var model = facts.Model;
        var features = facts.OfType(RecordTypes.Feature)
            .Where(feature => feature.Ids(RecordFields.Advances).Contains(goal.Id, StringComparer.Ordinal))
            .ToList();
        var journeys = facts.OfType(RecordTypes.Journey)
            .Where(journey => journey.Ids(RecordFields.Serves).Contains(goal.Id, StringComparer.Ordinal))
            .ToList();
        var requirements = facts.OfType(RecordTypes.Requirement)
            .Where(requirement => requirement.Ids(RecordFields.Cites).Contains(goal.Id, StringComparer.Ordinal))
            .ToList();

        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal);

        void AddCount(string label, IReadOnlyList<Record> children)
        {
            if (children.Count > 0)
            {
                counts[label] = new CompletionCount(
                    children.Count(child =>
                        model.Completion.TryGetValue(child.Id, out var childCompletion)
                        && childCompletion.Stage == "verified"),
                    children.Count);
            }
        }

        AddCount("features verified", features);
        AddCount("journeys verified", journeys);
        AddCount("requirements verified", requirements);

        var childStages = new List<(string Id, string Stage)>();

        foreach (var child in features.Concat(journeys).Concat(requirements))
        {
            if (model.Completion.TryGetValue(child.Id, out var completion))
            {
                childStages.Add((child.Id, completion.Stage));
            }
        }

        if (childStages.Count == 0)
        {
            counts["children verified"] = new CompletionCount(0, 0);

            return new Completion(
                "unfulfilled", ProofOf(model),
                Cap([goal.Id], counts), counts);
        }

        var weakest = childStages.Min(entry => Rank(entry.Stage));
        var held = childStages
            .Where(entry => Rank(entry.Stage) == weakest)
            .Select(entry => entry.Id);

        return new Completion(Stages[weakest], ProofOf(model), Cap(held, counts), counts);
    }

    /// <summary>The journeys citing any of the requirements, in id order.</summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="requirements">The requirements whose citing journeys are wanted.</param>
    /// <returns>The journeys citing one of the requirements.</returns>
    static List<Record> JourneysCiting(CompletionLookups facts, IReadOnlyList<Record> requirements)
    {
        var ids = requirements.Select(requirement => requirement.Id).ToHashSet(StringComparer.Ordinal);

        return facts.OfType(RecordTypes.Journey)
            .Where(journey => journey.Ids(RecordFields.Cites).Any(ids.Contains))
            .ToList();
    }

    /// <summary>Whether a fulfiller has code: a use case with a handler, a record a mark implements, or a verified fulfiller.</summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="fulfiller">The fulfilling record to test.</param>
    /// <param name="coded">The use cases with code behind them.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>True when code for the fulfiller was found or its proof passes.</returns>
    static bool FulfillerCoded(
        CompletionLookups facts, Record fulfiller,
        IReadOnlySet<string> coded, IReadOnlySet<string> stale)
    {
        if (FulfillerVerified(facts, fulfiller, stale))
        {
            return true;
        }

        return fulfiller.Type switch
        {
            RecordTypes.UseCase => coded.Contains(fulfiller.Id),
            RecordTypes.Decision => facts.Followed.Contains(fulfiller.Id),
            _ => facts.Implemented.Contains(fulfiller.Id),
        };
    }

    /// <summary>Whether a fulfiller is verified: a verified use case, a tested implementation decision, or a passing proof naming it.</summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="fulfiller">The fulfilling record to test.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>True when the fulfiller is verified or tested with a passing proof that is not stale.</returns>
    static bool FulfillerVerified(CompletionLookups facts, Record fulfiller, IReadOnlySet<string> stale) => fulfiller.Type switch
    {
        RecordTypes.UseCase => UseCaseVerified(facts, fulfiller, stale),
        RecordTypes.Decision => TestedDecision(facts, fulfiller),
        _ => facts.ProvenPassing.Contains(fulfiller.Id),
    };

    /// <summary>Whether a use case is verified: it holds a scenario, and every scenario has a passing proof that is not stale.</summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="useCase">The use case to test.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>True when every scenario of the use case has a confirmed passing proof that is not stale.</returns>
    static bool UseCaseVerified(CompletionLookups facts, Record useCase, IReadOnlySet<string> stale)
    {
        var scenarios = facts.ScenariosOf(useCase);

        if (scenarios.Count == 0)
        {
            return false;
        }

        foreach (var scenario in scenarios)
        {
            var links = facts.ProvesOf(scenario.Id);

            if (links.Count == 0
                || links.Any(link => !link.Confirmed || link.Passing != true)
                || stale.Contains(scenario.Id))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether an implementation decision is tested: a passing test that asserts something proves it.</summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="decision">The decision to test.</param>
    /// <returns>True when a passing asserting test proves the decision.</returns>
    static bool TestedDecision(CompletionLookups facts, Record decision) =>
        Checker.UnderImplementationDecisions(facts.Model.Config, decision.File)
        && facts.ProvesOf(decision.Id).Any(link =>
            link.Confirmed
            && link.Passing == true
            && facts.TestAsserts(link.Scenario));

    /// <summary>Whether a measure result answers the requirement's check.</summary>
    /// <param name="check">The requirement's check.</param>
    /// <param name="measure">The measure result.</param>
    /// <returns>True when a measure result answers the requirement's check.</returns>
    static bool CheckMatches(string check, MeasureResult measure) =>
        string.Equals(check, measure.Check, StringComparison.Ordinal)
        || string.Equals(check, $"{measure.Tool} {measure.Check}", StringComparison.Ordinal)
        || check.Contains(measure.Check, StringComparison.Ordinal);

    /// <summary>Whether a requirement's measure passes: true with no measure, else a passing result for its check.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="requirement">The requirement to test.</param>
    /// <returns>True when the requirement carries no measure or its check has a passing result.</returns>
    static bool MeasurePassed(ProjectModel model, Record requirement)
    {
        if (!requirement.Has(RecordFields.Measure))
        {
            return true;
        }

        return requirement.Field(RecordFields.Check) is { Length: > 0 } check
            && model.Measures.Any(measure => measure.Passed && CheckMatches(check, measure));
    }

    /// <summary>Whether a scenario is verified: a confirmed passing proof that is not stale.</summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="scenario">The scenario record to test.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>True when a passing proof confirms the scenario and the proof is not stale.</returns>
    static bool ScenarioVerified(CompletionLookups facts, Record scenario, IReadOnlySet<string> stale) =>
        !stale.Contains(scenario.Id)
        && facts.ProvenPassing.Contains(scenario.Id);

    /// <summary>
    ///     The use cases with code behind them: one a handler, reaction or job
    ///     implements, or one an endpoint holds itself with no handler behind it.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <returns>The ids of the use cases with code behind them.</returns>
    static HashSet<string> CodedUseCases(ProjectModel model)
    {
        var coded = new HashSet<string>(StringComparer.Ordinal);

        foreach (var link in model.Implements)
        {
            if (link.Kind is "handler" or "reaction" or "job")
            {
                coded.Add(link.To);
            }
        }

        // An endpoint that holds its behavior itself, with no handler behind it, implements
        // its use case too: a small development or session route needs no command.
        foreach (var endpoint in model.Code.Endpoints)
        {
            coded.Add(endpoint.UseCaseId);
        }

        return coded;
    }

    /// <summary>
    ///     The scenarios with stale proof: a scenario whose confirmed proof passes in the read run is
    ///     stale when its semantic hash or its tests' hash differs from the hashes the ledger
    ///     recorded for that run. A run passing the proof for the first time records the
    ///     current hashes instead. Empty when no test run was read.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <returns>The ids of the scenarios whose passing proof went stale.</returns>
    static HashSet<string> StaleScenarios(ProjectModel model)
    {
        var stale = new HashSet<string>(StringComparer.Ordinal);

        if (model.TestRun is null)
        {
            return stale;
        }

        var run = ProofLedger.RunOf(model.Config, model.TestRun);
        var passing = model.Proves
            .Where(link => link.Confirmed && link.Passing == true)
            .Select(link => link.Claim)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var scenario in model.Records.Where(scenario =>
            scenario.Type == RecordTypes.Scenario && passing.Contains(scenario.Id)))
        {
            if (model.Ledger.Stale(
                scenario.Id,
                Checker.SemanticHash(scenario),
                ProofLedger.TestHashOf(model, scenario.Id),
                ProofLedger.MethodHashOf(model, scenario.Id),
                run))
            {
                stale.Add(scenario.Id);
            }
        }

        return stale;
    }

    /// <summary>One journey step's stage from the records serving it, worked out once per step.</summary>
    /// <param name="facts">The model's records and links, indexed for this computation.</param>
    /// <param name="step">The journey step id, as <c>journey/&lt;name&gt;#&lt;step&gt;</c>.</param>
    /// <param name="coded">The use cases with code behind them.</param>
    /// <param name="stale">The scenarios whose passing proof went stale.</param>
    /// <returns>Unfulfilled when nothing serves the step, specified when its serving record has no code, coded when code is found and no passing proof exercises it, and verified when a passing proof exercises it.</returns>
    static string StepStage(
        CompletionLookups facts, string step,
        IReadOnlySet<string> coded, IReadOnlySet<string> stale)
    {
        if (facts.StepStages.TryGetValue(step, out var known))
        {
            return known;
        }

        string stage;

        if (facts.Model.ExercisedSteps.Contains(step))
        {
            stage = "verified";
        }
        else
        {
            var servers = facts.ServersOf(step);

            stage = servers.Count == 0
                ? "unfulfilled"
                : servers.Any(server => FulfillerCoded(facts, server, coded, stale))
                    ? "coded"
                    : "specified";
        }

        facts.StepStages[step] = stage;

        return stage;
    }

    /// <summary>Caps held-by ids at ten, in the order given, and counts the full set when it is capped.</summary>
    /// <param name="ids">The ids holding a record back.</param>
    /// <param name="counts">The counts behind the stage, which gain a held-by count when the list is capped.</param>
    /// <returns>At most ten ids, in the order given.</returns>
    static List<string> Cap(IEnumerable<string> ids, Dictionary<string, CompletionCount> counts)
    {
        var all = ids.Distinct(StringComparer.Ordinal).ToList();

        if (all.Count > 10)
        {
            counts["held by"] = new CompletionCount(10, all.Count);
        }

        return all.Take(10).ToList();
    }
}
