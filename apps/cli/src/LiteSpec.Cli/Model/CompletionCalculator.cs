namespace LiteSpec.Model;

/// <summary>
///     Computes the completion stage of every requirement, use case, journey,
///     feature and goal from the facts the builder already holds: fulfilment,
///     code found, verified use cases, met requirements and exercised steps.
///     A parent is only as far as its weakest in-scope child. A requirement an
///     approved gap exception parks stays out of the stage and the counts and
///     names itself in <c>heldBy</c>, so deferred work stays visible.
/// </summary>
/// <follows>litespec/core/model#compute-completion-in-the-builder</follows>
static class CompletionCalculator
{
    /// <summary>The stage names from least to most complete.</summary>
    static readonly string[] Stages = ["unfulfilled", "specified", "implemented", "proven"];

    /// <summary>Computes completion for every requirement, use case, journey, feature and goal in the model.</summary>
    /// <param name="model">The built project model, with use case and requirement status already computed.</param>
    public static void Compute(ProjectModel model)
    {
        model.Completion.Clear();

        foreach (var requirement in Live(model, RecordTypes.Requirement))
        {
            model.Completion[requirement.Id] = RequirementCompletion(model, requirement);
        }

        foreach (var useCase in Live(model, RecordTypes.UseCase))
        {
            model.Completion[useCase.Id] = UseCaseCompletion(model, useCase);
        }

        foreach (var journey in Live(model, RecordTypes.Journey))
        {
            model.Completion[journey.Id] = JourneyCompletion(model, journey);
        }

        foreach (var feature in Live(model, RecordTypes.Feature))
        {
            model.Completion[feature.Id] = FeatureCompletion(model, feature);
        }

        foreach (var goal in Live(model, RecordTypes.Goal))
        {
            model.Completion[goal.Id] = GoalCompletion(model, goal);
        }
    }

    /// <summary>The live records of one type, in id order.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="type">The record type to list.</param>
    /// <returns>The records of the type that are not retired, in id order.</returns>
    static List<Record> Live(ProjectModel model, string type) =>
        model.Records
            .Where(record => record.Type == type && record.Status != RecordStatuses.Retired)
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>Whether the model holds a test run, so proof is known.</summary>
    /// <param name="model">The built project model.</param>
    /// <returns>Known when a test run was read, else unknown.</returns>
    static string ProofOf(ProjectModel model) =>
        model.TestRun is null ? "unknown" : "known";

    /// <summary>The rank of a stage name, for weakest-child comparisons.</summary>
    /// <param name="stage">The stage name to rank.</param>
    /// <returns>0 for unfulfilled, 1 for specified, 2 for implemented and 3 for proven.</returns>
    static int Rank(string stage) => Array.IndexOf(Stages, stage);

    /// <summary>
    ///     One requirement's completion. Its children are the Behavior records
    ///     fulfilling it and, for a quality requirement, a technical decision
    ///     naming it. No child is unfulfilled, a child with no code is specified,
    ///     every child with code and not every child proven is implemented, and
    ///     every child proven with its measure passing is proven.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <param name="requirement">The requirement whose completion is wanted.</param>
    /// <returns>The requirement completion.</returns>
    static Completion RequirementCompletion(ProjectModel model, Record requirement)
    {
        var fulfillers = FulfillersOf(model, requirement);
        var implemented = fulfillers.Count(child => FulfillerImplemented(model, child));
        var proven = fulfillers.Count(child => FulfillerProven(model, child));
        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["fulfillers implemented"] = new CompletionCount(implemented, fulfillers.Count),
            ["fulfillers proven"] = new CompletionCount(proven, fulfillers.Count),
        };

        if (fulfillers.Count == 0)
        {
            return new Completion("unfulfilled", ProofOf(model), [requirement.Id], counts);
        }

        if (model.RequirementStatus.TryGetValue(requirement.Id, out var status) && status.Verified)
        {
            return new Completion("proven", ProofOf(model), [], counts);
        }

        if (implemented == fulfillers.Count)
        {
            return new Completion(
                "implemented", ProofOf(model),
                Cap(fulfillers.Where(child => !FulfillerProven(model, child)).Select(child => child.Id), counts),
                counts);
        }

        return new Completion(
            "specified", ProofOf(model),
            Cap(fulfillers.Where(child => !FulfillerImplemented(model, child)).Select(child => child.Id), counts),
            counts);
    }

    /// <summary>
    ///     One use case's completion. Its children are its scenarios. No scenario
    ///     is unfulfilled, a scenario with no code behind the use case is
    ///     specified, code with not every scenario proven is implemented, and
    ///     every scenario proven fresh is proven.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <param name="useCase">The use case whose completion is wanted.</param>
    /// <returns>The use case completion.</returns>
    static Completion UseCaseCompletion(ProjectModel model, Record useCase)
    {
        var scenarios = ScenariosOf(model, useCase);
        var status = model.Status.TryGetValue(useCase.Id, out var found)
            ? found
            : new UseCaseStatus(false, false);
        var provenScenarios = scenarios.Count(scenario => ScenarioProven(model, scenario));
        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["code found"] = new CompletionCount(status.Implemented ? 1 : 0, 1),
            ["scenarios proven"] = new CompletionCount(provenScenarios, scenarios.Count),
        };

        if (status.Verified && scenarios.Count > 0)
        {
            return new Completion("proven", ProofOf(model), [], counts);
        }

        if (status.Implemented)
        {
            return new Completion(
                "implemented", ProofOf(model),
                Cap(scenarios.Where(scenario => !ScenarioProven(model, scenario)).Select(scenario => scenario.Id), counts),
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
    ///     implemented, and an exercised step is proven.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <param name="journey">The journey whose completion is wanted.</param>
    /// <returns>The journey completion.</returns>
    static Completion JourneyCompletion(ProjectModel model, Record journey)
    {
        var steps = journey.StepIds();
        var served = 0;
        var implemented = 0;
        var exercised = 0;
        var stepStages = new List<(string Step, string Stage)>();

        foreach (var step in steps)
        {
            var stage = StepStage(model, step);

            stepStages.Add((step, stage));

            if (Rank(stage) >= Rank("specified"))
            {
                served++;
            }

            if (Rank(stage) >= Rank("implemented"))
            {
                implemented++;
            }

            if (stage == "proven")
            {
                exercised++;
            }
        }

        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["journey steps served"] = new CompletionCount(served, steps.Count),
            ["steps implemented"] = new CompletionCount(implemented, steps.Count),
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
    ///     One feature's completion. Its in-scope children are the requirements
    ///     citing it that no gap exception parks and the steps of the journeys
    ///     citing those requirements. A parked requirement, and every
    ///     requirement of a later or refused feature, stays out of the stage and
    ///     the counts and names itself in <c>heldBy</c>.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <param name="feature">The feature whose completion is wanted.</param>
    /// <returns>The feature completion.</returns>
    static Completion FeatureCompletion(ProjectModel model, Record feature)
    {
        var citing = Live(model, RecordTypes.Requirement)
            .Where(requirement => requirement.Ids(RecordFields.Cites).Contains(feature.Id, StringComparer.Ordinal))
            .ToList();
        var parked = citing.Where(requirement => IsParked(model, requirement, feature)).ToList();
        var inScope = citing.Except(parked).ToList();
        var journeys = JourneysCiting(model, inScope);
        var steps = journeys.SelectMany(journey => journey.StepIds()).Distinct(StringComparer.Ordinal).ToList();

        var fulfilled = inScope.Count(requirement => FulfillersOf(model, requirement).Count > 0);
        var proven = inScope.Count(requirement =>
            model.Completion.TryGetValue(requirement.Id, out var child) && child.Stage == "proven");
        var servedSteps = steps.Count(step => Rank(StepStage(model, step)) >= Rank("specified"));
        var implementedSteps = steps.Count(step => Rank(StepStage(model, step)) >= Rank("implemented"));
        var exercisedSteps = steps.Count(step => StepStage(model, step) == "proven");

        var counts = new Dictionary<string, CompletionCount>(StringComparer.Ordinal)
        {
            ["requirements fulfilled"] = new CompletionCount(fulfilled, inScope.Count),
            ["requirements proven"] = new CompletionCount(proven, inScope.Count),
            ["journey steps served"] = new CompletionCount(servedSteps, steps.Count),
            ["steps implemented"] = new CompletionCount(implementedSteps, steps.Count),
            ["steps exercised"] = new CompletionCount(exercisedSteps, steps.Count),
        };

        var childStages = new List<(string Id, string Stage)>();

        foreach (var requirement in inScope)
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

        var parkedIds = parked.Select(requirement => requirement.Id).ToList();

        if (childStages.Count == 0)
        {
            return new Completion(
                "unfulfilled", ProofOf(model),
                Cap([feature.Id, .. parkedIds], counts), counts);
        }

        var weakest = childStages.Min(entry => Rank(entry.Stage));
        var held = childStages
            .Where(entry => Rank(entry.Stage) == weakest)
            .Select(entry => entry.Id)
            .Concat(parkedIds);

        return new Completion(Stages[weakest], ProofOf(model), Cap(held, counts), counts);
    }

    /// <summary>
    ///     One goal's completion. Its in-scope children are the features advancing
    ///     it, the journeys serving it and the requirements citing it. A later
    ///     feature stays out of the stage and the counts and names itself in
    ///     <c>heldBy</c>.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <param name="goal">The goal whose completion is wanted.</param>
    /// <returns>The goal completion.</returns>
    static Completion GoalCompletion(ProjectModel model, Record goal)
    {
        var features = Live(model, RecordTypes.Feature)
            .Where(feature => feature.Ids(RecordFields.Advances).Contains(goal.Id, StringComparer.Ordinal))
            .ToList();
        var parked = features
            .Where(feature => feature.Field(RecordFields.Lifecycle) == "later")
            .ToList();
        var inScopeFeatures = features.Except(parked).ToList();
        var journeys = Live(model, RecordTypes.Journey)
            .Where(journey => journey.Ids(RecordFields.Serves).Contains(goal.Id, StringComparer.Ordinal))
            .ToList();
        var requirements = Live(model, RecordTypes.Requirement)
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
                        && childCompletion.Stage == "proven"),
                    children.Count);
            }
        }

        AddCount("features proven", inScopeFeatures);
        AddCount("journeys proven", journeys);
        AddCount("requirements proven", requirements);

        var childStages = new List<(string Id, string Stage)>();

        foreach (var child in inScopeFeatures.Concat(journeys).Concat(requirements))
        {
            if (model.Completion.TryGetValue(child.Id, out var completion))
            {
                childStages.Add((child.Id, completion.Stage));
            }
        }

        var parkedIds = parked.Select(feature => feature.Id).ToList();

        if (childStages.Count == 0)
        {
            counts["children proven"] = new CompletionCount(0, 0);

            return new Completion(
                "unfulfilled", ProofOf(model),
                Cap([goal.Id, .. parkedIds], counts), counts);
        }

        var weakest = childStages.Min(entry => Rank(entry.Stage));
        var held = childStages
            .Where(entry => Rank(entry.Stage) == weakest)
            .Select(entry => entry.Id)
            .Concat(parkedIds);

        return new Completion(Stages[weakest], ProofOf(model), Cap(held, counts), counts);
    }

    /// <summary>The journeys citing any of the requirements, in id order.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="requirements">The requirements whose citing journeys are wanted.</param>
    /// <returns>The journeys citing one of the requirements.</returns>
    static List<Record> JourneysCiting(ProjectModel model, IReadOnlyList<Record> requirements)
    {
        var ids = requirements.Select(requirement => requirement.Id).ToHashSet(StringComparer.Ordinal);

        return Live(model, RecordTypes.Journey)
            .Where(journey => journey.Ids(RecordFields.Cites).Any(ids.Contains))
            .ToList();
    }

    /// <summary>
    ///     Whether a requirement is parked for a feature: an approved gap exception
    ///     names it and covers the missing fulfilment, or the feature carrying it
    ///     is later or refused, so its delivery is deferred or declined.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <param name="requirement">The requirement to test.</param>
    /// <param name="feature">The feature the requirement cites.</param>
    /// <returns>True when the requirement is parked for the feature.</returns>
    static bool IsParked(ProjectModel model, Record requirement, Record feature)
    {
        if (feature.Field(RecordFields.Lifecycle) is "later" or "refused")
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return model.Records.Any(exception =>
            exception.Type == RecordTypes.Exception
            && exception.Status == RecordStatuses.Approved
            && exception.Field(RecordFields.Kind) == "gap"
            && exception.Ids(RecordFields.Subject).Contains(requirement.Id, StringComparer.Ordinal)
            && exception.Ids(RecordFields.Covers).Contains("requirement-fulfillment-missing", StringComparer.Ordinal)
            && (exception.Field(RecordFields.ExpiresOn) is not { Length: > 0 } expiry
                || !DateOnly.TryParseExact(expiry, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var expires)
                || expires >= today));
    }

    /// <summary>The Behavior records fulfilling a requirement, with a technical decision serving a quality requirement.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="requirement">The requirement whose fulfillers are wanted.</param>
    /// <returns>The fulfilling records in id order.</returns>
    static List<Record> FulfillersOf(ProjectModel model, Record requirement)
    {
        var fulfillers = model.Records
            .Where(record => RecordTypes.FulfillingTypes.Contains(record.Type, StringComparer.Ordinal)
                && record.Ids(RecordFields.Fulfills).Contains(requirement.Id, StringComparer.Ordinal))
            .ToList();

        if (requirement.Field(RecordFields.Kind) == "quality")
        {
            fulfillers.AddRange(model.Records.Where(record =>
                record.Type == RecordTypes.Decision
                && Checker.UnderImplementationDecisions(model.Config, record.File)
                && record.Ids(RecordFields.BasedOn).Contains(requirement.Id, StringComparer.Ordinal)));
        }

        return fulfillers.OrderBy(record => record.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>Whether a fulfiller has code: a use case with a handler, a record a mark implements, or a proven fulfiller.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="fulfiller">The fulfilling record to test.</param>
    /// <returns>True when code for the fulfiller was found or its proof passes.</returns>
    static bool FulfillerImplemented(ProjectModel model, Record fulfiller)
    {
        if (FulfillerProven(model, fulfiller))
        {
            return true;
        }

        return fulfiller.Type switch
        {
            RecordTypes.UseCase => model.Status.TryGetValue(fulfiller.Id, out var status) && status.Implemented,
            RecordTypes.Decision => model.Follows.Any(link => link.To == fulfiller.Id),
            _ => model.Implements.Any(link => link.To == fulfiller.Id),
        };
    }

    /// <summary>Whether a fulfiller is proven: a verified use case, a tested decision, or a passing proof naming it.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="fulfiller">The fulfilling record to test.</param>
    /// <returns>True when the fulfiller is verified or tested with a passing proof that is not stale.</returns>
    static bool FulfillerProven(ProjectModel model, Record fulfiller) => fulfiller.Type switch
    {
        RecordTypes.UseCase => model.Status.TryGetValue(fulfiller.Id, out var status) && status.Verified,
        RecordTypes.Decision => model.Tested.TryGetValue(fulfiller.Id, out var tested) && tested,
        _ => model.Proves.Any(link => link.Claim == fulfiller.Id && link.Confirmed && link.Passing == true),
    };

    /// <summary>Whether a scenario is proven: a confirmed passing proof that is not stale.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="scenario">The scenario record to test.</param>
    /// <returns>True when a passing proof confirms the scenario and the proof is not stale.</returns>
    static bool ScenarioProven(ProjectModel model, Record scenario) =>
        !model.Stale.Contains(scenario.Id)
        && model.Proves.Any(link => link.Claim == scenario.Id && link.Confirmed && link.Passing == true);

    /// <summary>The scenarios of a use case, by identifier home.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="useCase">The use case whose scenarios are wanted.</param>
    /// <returns>The scenario records the use case holds, in id order.</returns>
    static List<Record> ScenariosOf(ProjectModel model, Record useCase)
    {
        if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            return [];
        }

        return model.Records
            .Where(record => record.Type == RecordTypes.Scenario
                && !record.Derived
                && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                && home == name)
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>One journey step's stage from the records serving it.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="step">The journey step id, as <c>journey/&lt;name&gt;#&lt;step&gt;</c>.</param>
    /// <returns>Unfulfilled when nothing serves the step, specified when its serving record has no code, implemented when code is found and no passing proof exercises it, and proven when a passing proof exercises it.</returns>
    static string StepStage(ProjectModel model, string step)
    {
        if (model.ExercisedSteps.Contains(step))
        {
            return "proven";
        }

        var servers = model.Records
            .Where(record => RecordTypes.StepServingTypes.Contains(record.Type, StringComparer.Ordinal)
                && record.Ids(RecordFields.Serves).Contains(step, StringComparer.Ordinal))
            .ToList();

        if (servers.Count == 0)
        {
            return "unfulfilled";
        }

        return servers.Any(server => FulfillerImplemented(model, server))
            ? "implemented"
            : "specified";
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
