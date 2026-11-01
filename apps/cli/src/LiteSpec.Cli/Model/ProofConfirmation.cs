using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>
///     Confirms what a test proves: whether a backend or browser test's claim on a
///     scenario, a rule, or a self-confirming record holds through the public
///     boundary the test drives.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class ProofConfirmation
{
    /// <summary>Computes the proves links of every backend and browser test.</summary>
    /// <param name="model">The project model.</param>
    public static void Compute(ProjectModel model)
    {
        foreach (var test in model.Tests)
        {
            var passing = model.TestRun is not null
                && model.TestRun.OutcomeByMethod.TryGetValue(test.Name, out var outcome)
                    ? outcome == "Passed"
                    : (bool?)null;

            foreach (var claim in test.Claims)
            {
                model.Proves.Add(new ProvesLink(
                    test.Name, claim, Confirm(model, test, claim), passing, Indirect(model, claim)));
            }
        }

        foreach (var test in model.BrowserTests)
        {
            var passing = model.TestRun?.PassingOfTitle(test.Title);

            foreach (var claim in test.Proves)
            {
                model.Proves.Add(new ProvesLink(
                    test.Title, claim, BrowserConfirm(model, test, claim), passing, Indirect(model, claim)));
            }
        }

        ComputeExercised(model);
    }

    /// <summary>
    ///     A journey step is exercised when a passing test proved a scenario of a
    ///     use case that serves the step.
    /// </summary>
    /// <param name="model">The project model.</param>
    private static void ComputeExercised(ProjectModel model)
    {
        foreach (var link in model.Proves)
        {
            if (link.Passing != true || !link.Confirmed)
            {
                continue;
            }

            if (!model.ById.TryGetValue(link.Claim, out var scenario) || scenario.Type != RecordTypes.Scenario)
            {
                continue;
            }

            if (!Identifiers.TrySplit(scenario.Id, out _, out var home, out _) || home is null
                || ModelBuilder.UseCaseOf(model, home) is not { } useCase)
            {
                continue;
            }

            foreach (var step in useCase.Ids(RecordFields.Serves))
            {
                if (Identifiers.TrySplitStep(step, out _, out _))
                {
                    model.ExercisedSteps.Add(step);
                }
            }
        }
    }

    /// <summary>
    ///     Confirms a browser test's claim: a scenario confirms when one of its
    ///     <c>gotos</c> matches the <c>address</c> of a screen that offers the
    ///     scenario's use case (a <c>[param]</c> or <c>{}</c> segment matches any
    ///     segment), and its <c>codes</c> hold the refusal code (a refusal
    ///     scenario) or one of its asserted texts is the result's sentence or the
    ///     scenario's Then text (a success scenario). Rule and provision claims
    ///     follow the same rules as a backend test's claims.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The browser test.</param>
    /// <param name="claim">The record the test claims to prove.</param>
    /// <returns>True when the browser test confirms the claim.</returns>
    public static bool BrowserConfirm(ProjectModel model, BrowserTestInfo test, string claim)
    {
        if (claim.StartsWith("litespec/", StringComparison.Ordinal))
        {
            return Authoring.Provisions.IsProvisionId(claim)
                && (model.Provisions.Count == 0 || model.Provisions.Contains(claim));
        }

        if (!model.ById.TryGetValue(claim, out var claimed))
        {
            return false;
        }

        if (claimed.Type is RecordTypes.Requirement or RecordTypes.Decision or RecordTypes.ExternalSystem or RecordTypes.IntegrationEvent)
        {
            return true;
        }

        if (claimed.Type == RecordTypes.Rule)
        {
            var governed = claimed.Ids(RecordFields.Governs);

            return test.Proves.Any(other =>
                other != claim
                && model.ById.TryGetValue(other, out var scenario)
                && scenario.Type == RecordTypes.Scenario
                && Identifiers.TrySplit(other, out _, out var home, out _)
                && home is not null
                && ModelBuilder.UseCaseOf(model, home) is { } useCase
                && governed.Contains(useCase.Id, StringComparer.Ordinal)
                && BrowserConfirm(model, test, other));
        }

        if (claimed.Type != RecordTypes.Scenario)
        {
            return false;
        }

        if (!Identifiers.TrySplit(claim, out _, out var scenarioHome, out _) || scenarioHome is null
            || ModelBuilder.UseCaseOf(model, scenarioHome) is not { } scenarioUseCase)
        {
            return false;
        }

        var reached = model.Records
            .Where(record => record.Type == RecordTypes.Screen
                && record.Field(RecordFields.Address) is { } address
                && record.Ids(RecordFields.Offers).Contains(scenarioUseCase.Id, StringComparer.Ordinal))
            .Any(screen => test.Gotos.Any(target =>
                Readers.FrontendFactsRules.AddressMatches(screen.Field(RecordFields.Address)!, target)));

        if (!reached)
        {
            return false;
        }

        var resultId = claimed.Ids(RecordFields.Result) is { Count: > 0 } claimedIds ? claimedIds[0] : null;
        var result = resultId is not null && model.ById.TryGetValue(resultId, out var found) ? found : null;

        if (result?.Field(RecordFields.Outcome) == "refusal")
        {
            return result.Field(RecordFields.Code) is { } code && test.Codes.Contains(code, StringComparer.Ordinal);
        }

        var sentences = new HashSet<string>(StringComparer.Ordinal);

        if (claimed.Field(RecordFields.Then) is { Length: > 0 } then && then.Trim() is { Length: > 0 } trimmedThen)
        {
            sentences.Add(trimmedThen);
        }

        if (result?.Field(RecordFields.Meaning) is { Length: > 0 } meaning && meaning.Trim() is { Length: > 0 } trimmedMeaning)
        {
            sentences.Add(trimmedMeaning);
        }

        return sentences.Count > 0 && test.Texts.Any(text => sentences.Contains(text.Trim()));
    }

    /// <summary>Whether the claim is indirect: a scenario of a background-only use case.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="claim">The record the test claims to prove.</param>
    /// <returns>True when the claim is indirect.</returns>
    private static bool Indirect(ProjectModel model, string claim)
    {
        if (!model.ById.TryGetValue(claim, out var claimed) || claimed.Type != RecordTypes.Scenario)
        {
            return false;
        }

        return Identifiers.TrySplit(claim, out _, out var home, out _)
            && home is not null
            && ModelBuilder.UseCaseOf(model, home) is { } useCase
            && UseCaseFacts.BackgroundOnly(model, useCase);
    }

    /// <summary>
    ///     Confirms a proves claim. A steps test proves through the public boundary: a
    ///     when step of the use case's steps class or generated-client operation plus a
    ///     then step, asserting the refusal code for a refusal. A refusal also confirms
    ///     from the HTTP status the project's refusal table maps to its code, or from a
    ///     client code the project's refusal map translates to it. An integration test
    ///     proves by constructing the message it sends or requesting the use case's own
    ///     endpoint, plus an assertion. No test constructs a handler, an aggregate, or a
    ///     repository directly. A rule claim confirms through a scenario the same test
    ///     proves, or through an integration fact that resolves a service from the host
    ///     and calls the message type of a governed use case or the governed aggregate.
    ///     A requirement, decision, external-system, or integration-event claim
    ///     confirms on its own once it resolves: no step, message, or effect analysis
    ///     applies, and its passing run marks it tested. A provision claim confirms
    ///     when the carried release holds it.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <param name="claim">The record the test claims to prove.</param>
    /// <returns>True when the test confirms the claim.</returns>
    public static bool Confirm(ProjectModel model, ScenarioInfo test, string claim)
    {
        if (claim.StartsWith("litespec/", StringComparison.Ordinal))
        {
            return Authoring.Provisions.IsProvisionId(claim)
                && (model.Provisions.Count == 0 || model.Provisions.Contains(claim));
        }

        if (!model.ById.TryGetValue(claim, out var claimed))
        {
            return false;
        }

        if (claimed.Type == RecordTypes.Rule)
        {
            return ConfirmRule(model, test, claimed);
        }

        if (claimed.Type is RecordTypes.Requirement or RecordTypes.Decision or RecordTypes.ExternalSystem or RecordTypes.IntegrationEvent)
        {
            return true;
        }

        if (claimed.Type != RecordTypes.Scenario)
        {
            return false;
        }

        if (!Identifiers.TrySplit(claim, out _, out var home, out _)
            || home is null
            || ModelBuilder.UseCaseOf(model, home) is not { } useCase)
        {
            return false;
        }

        if (ConstructsInside(test, model))
        {
            return false;
        }

        var refusal = RefusalOf(model, claimed);

        if (test.Steps.Count > 0)
        {
            if (UseCaseFacts.BackgroundOnly(model, useCase))
            {
                // The trigger may arrive through an entry the model does not link to the reaction, such as a
                // processor webhook, so any when step sets it off; the assertion is what ties the proof to it.
                var setOff = Triggers(model, test, useCase) || test.Steps.Any(step => step.Phase == "when");
                var asserted = refusal is null ? AssertsEffect(model, test) : Asserts(model, test, refusal) || AssertsEffect(model, test);

                return setOff && asserted;
            }

            // The use case runs in a when step; or it runs in a given step and a later when step observes what it
            // changed; or a when step drives a use case whose fact or effect runs it. Each needs a then step.
            var driven = test.Steps.Any(step => step.Phase == "when" && Reaches(model, step, home, useCase));
            var observed = !driven && ObservedLater(model, test, home, useCase);
            var downstream = !driven && !observed && Triggers(model, test, useCase) && AssertsEffect(model, test);

            if (!driven && !observed && !downstream)
            {
                return false;
            }

            if (!test.Steps.Any(step => step.Phase == "then"))
            {
                return false;
            }

            return refusal is null || Asserts(model, test, refusal);
        }

        var pascal = Naming.Pascal(home);

        // An integration test constructs the message it sends, or requests the use case's own endpoint.
        var sendsMessage = Builds(test, pascal + "Command") || Builds(test, pascal + "Query");
        var requestsEndpoint = model.Code.Endpoints
            .Where(endpoint => endpoint.UseCaseId == useCase.Id && endpoint.Route.Length > 0)
            .Any(endpoint => test.Routes.Any(route => RouteMatches(endpoint.Route, route)));

        if (!sendsMessage && !requestsEndpoint)
        {
            return false;
        }

        if (!test.HasAssertion)
        {
            return false;
        }

        if (refusal is null)
        {
            return true;
        }

        return Asserts(model, test, refusal);
    }

    /// <summary>Whether the test builds the message type, plainly or namespace-qualified.</summary>
    /// <param name="test">The scenario test.</param>
    /// <param name="want">The message type name the test must build.</param>
    /// <returns>True when the test builds the message type.</returns>
    private static bool Builds(ScenarioInfo test, string want) =>
        test.CreatedTypes.Any(type =>
            type == want || type.EndsWith("." + want, StringComparison.Ordinal));

    /// <summary>
    ///     Whether the test reaches past the public boundary: it constructs a handler,
    ///     an aggregate, or a repository directly instead of driving the use case through
    ///     its steps, its message, or its endpoint.
    /// </summary>
    /// <param name="test">The scenario test.</param>
    /// <param name="model">The project model.</param>
    /// <returns>True when the test constructs a handler, an aggregate, or a repository directly.</returns>
    private static bool ConstructsInside(ScenarioInfo test, ProjectModel model)
    {
        if (test.CreatedTypes.Count == 0)
        {
            return false;
        }

        if (test.CreatedTypes.Any(type =>
            type.EndsWith("Handler", StringComparison.Ordinal)
            || type.EndsWith("Repository", StringComparison.Ordinal)))
        {
            return true;
        }

        return model.Records.Any(record =>
            record.Type == RecordTypes.Aggregate
            && Identifiers.TrySplit(record.Id, out _, out _, out var aggregate)
            && aggregate is not null
            && test.CreatedTypes.Contains(Naming.Pascal(aggregate), StringComparer.Ordinal));
    }

    /// <summary>
    ///     Whether a path a test requests is the endpoint's route: the same segments, where a route
    ///     parameter matches any segment and an interpolation hole matches only a parameter. A route mapped
    ///     inside a group may omit the group's prefix, so the path may carry leading segments the route lacks.
    /// </summary>
    /// <param name="template">The endpoint's route template.</param>
    /// <param name="path">The path the test requests.</param>
    /// <returns>True when the path is the endpoint's route.</returns>
    private static bool RouteMatches(string template, string path)
    {
        static string[] Segments(string value) => value.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        var route = Segments(template);
        var requested = Segments(path);

        if (route.Length == 0 || requested.Length < route.Length)
        {
            return false;
        }

        var offset = requested.Length - route.Length;

        for (var index = 0; index < route.Length; index++)
        {
            if (!SegmentMatches(route[index], requested[offset + index]))
            {
                return false;
            }
        }

        // A route of parameters alone says nothing about which endpoint was asked.
        return route.Any(segment => !(segment.StartsWith('{') && segment.EndsWith('}')));
    }

    /// <summary>
    ///     Whether a requested segment fills a template segment: a parameter matches any non-empty text, also
    ///     inside a segment, so <c>{gateId}:close</c> matches <c>{}:close</c> and <c>8f3a:close</c>.
    /// </summary>
    /// <param name="wanted">The template segment.</param>
    /// <param name="given">The requested segment.</param>
    /// <returns>True when the requested segment fills the template segment.</returns>
    private static bool SegmentMatches(string wanted, string given)
    {
        if (!wanted.Contains('{', StringComparison.Ordinal))
        {
            return string.Equals(wanted, given, StringComparison.OrdinalIgnoreCase);
        }

        var pattern = "^" + string.Concat(Regex.Split(wanted, @"(\{[^}]*\})")
            .Where(part => part.Length > 0)
            .Select(part => part.StartsWith('{') && part.EndsWith('}') ? ".+?" : Regex.Escape(part))) + "$";

        return given.Length > 0 && Regex.IsMatch(given, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>
    ///     Confirms a rule claim through the test's own scenarios: the test proves the
    ///     rule only when it also proves a scenario of a use case the rule governs.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <param name="rule">The rule record.</param>
    /// <returns>True when the test confirms the rule claim.</returns>
    private static bool ConfirmRule(ProjectModel model, ScenarioInfo test, Record rule)
    {
        var governed = rule.Ids(RecordFields.Governs);

        if (governed.Count == 0)
        {
            return false;
        }

        foreach (var claim in test.Claims)
        {
            if (!model.ById.TryGetValue(claim, out var claimed)
                || claimed.Type != RecordTypes.Scenario
                || !Identifiers.TrySplit(claim, out _, out var home, out _)
                || home is null
                || ModelBuilder.UseCaseOf(model, home) is not { } useCase
                || !Confirm(model, test, claim))
            {
                continue;
            }

            if (governed.Contains(useCase.Id, StringComparer.Ordinal))
            {
                return true;
            }

            if (UseCaseFacts.AggregateOf(model, useCase) is { } holder
                && governed.Contains(holder.Id, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return ConfirmRuleFact(model, test, rule);
    }

    /// <summary>
    ///     Confirms a rule claim from an integration fact: the test carries no steps,
    ///     resolves a service from the host, calls the message type of a governed use
    ///     case or calls onto the governed aggregate, and asserts afterwards.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <param name="rule">The rule record.</param>
    /// <returns>True when an integration fact confirms the rule claim.</returns>
    private static bool ConfirmRuleFact(ProjectModel model, ScenarioInfo test, Record rule)
    {
        if (test.Steps.Count > 0
            || test.ResolvedServices.Count == 0
            || !test.HasAssertion
            || ConstructsInside(test, model))
        {
            return false;
        }

        foreach (var governed in rule.Ids(RecordFields.Governs))
        {
            if (!model.ById.TryGetValue(governed, out var target))
            {
                continue;
            }

            if (target.Type == RecordTypes.UseCase
                && Identifiers.TrySplit(governed, out _, out _, out var operation)
                && operation is not null
                && (Builds(test, Naming.Pascal(operation) + "Command")
                    || Builds(test, Naming.Pascal(operation) + "Query")))
            {
                return true;
            }

            if (target.Type == RecordTypes.Aggregate
                && Identifiers.TrySplit(governed, out _, out _, out var aggregate)
                && aggregate is not null
                && CallsAggregate(test, Naming.Pascal(aggregate)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Whether the test calls onto the aggregate: a static call on its type, or work
    ///     through a resolved store whose stem names it, such as a repository or ledger.
    /// </summary>
    /// <param name="test">The scenario test.</param>
    /// <param name="pascal">The aggregate's PascalCase name.</param>
    /// <returns>True when the test calls onto the aggregate.</returns>
    private static bool CallsAggregate(ScenarioInfo test, string pascal)
    {
        if (test.StaticCalls.Any(call => call.StartsWith(pascal + ".", StringComparison.Ordinal)))
        {
            return true;
        }

        return test.ResolvedServices.Any(service => ServiceStem(service) == pascal);
    }

    /// <summary>The aggregate a store type serves: without its interface prefix and store suffixes.</summary>
    /// <param name="service">The store type name.</param>
    /// <returns>The aggregate a store type serves.</returns>
    private static string ServiceStem(string service)
    {
        var stem = service;

        if (stem.StartsWith('I') && stem.Length > 1 && char.IsUpper(stem[1]))
        {
            stem = stem[1..];
        }

        var cut = true;

        while (cut)
        {
            cut = false;

            foreach (var suffix in new[] { "Repository", "Ledger", "Store" })
            {
                if (stem.EndsWith(suffix, StringComparison.Ordinal) && stem.Length > suffix.Length)
                {
                    stem = stem[..^suffix.Length];
                    cut = true;
                    break;
                }
            }
        }

        return stem;
    }

    /// <summary>The refusal result a scenario ends in, or null when it ends in a success.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="scenario">The scenario record.</param>
    /// <returns>The linked result record when its outcome is refusal; otherwise null.</returns>
    private static Record? RefusalOf(ProjectModel model, Record scenario)
    {
        var resultId = scenario.Ids(RecordFields.Result) is { Count: > 0 } scenarioIds ? scenarioIds[0] : null;

        return resultId is not null
            && model.ById.TryGetValue(resultId, out var result)
            && result.Field(RecordFields.Outcome) == "refusal"
                ? result
                : null;
    }

    /// <summary>
    ///     Whether one step reaches the use case: it belongs to the use case's steps class, it calls the use
    ///     case's generated-client operation, it requests the use case's endpoint route itself, or it looks its
    ///     argument up in its class's probe table and requests the route found there.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="step">The scenario step.</param>
    /// <param name="home">The use case's home name.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when the step reaches the use case.</returns>
    private static bool Reaches(ProjectModel model, ScenarioStep step, string home, Record useCase)
    {
        var pascal = Naming.Pascal(home);
        var stepsClass = StepsClassOf(step);

        if (stepsClass == pascal + "Steps")
        {
            return true;
        }

        if (model.StepCalls.TryGetValue($"{stepsClass}.{step.Method}", out var called) && called.Contains(pascal))
        {
            return true;
        }

        var routes = new List<string>();

        if (model.StepFacts.TryGetValue($"{stepsClass}.{step.Method}", out var facts))
        {
            routes.AddRange(facts.Routes);
        }

        if (model.ProbeRoutes.TryGetValue(stepsClass, out var table))
        {
            routes.AddRange(step.Arguments.Select(argument => table.GetValueOrDefault(argument)).OfType<string>());
        }

        return routes.Count > 0
            && model.Code.Endpoints
                .Where(endpoint => endpoint.UseCaseId == useCase.Id && endpoint.Route.Length > 0)
                .Any(endpoint => routes.Any(route => RouteMatches(endpoint.Route, route)));
    }

    /// <summary>
    ///     The steps class a step runs on: the declared type of its receiver, else the receiver's name with the
    ///     Steps suffix, since a scenario base class may declare the property instead of the scenario class.
    /// </summary>
    /// <param name="step">The scenario step.</param>
    /// <returns>The steps class a step runs on.</returns>
    private static string StepsClassOf(ScenarioStep step) =>
        step.ReceiverType is { Length: > 0 } type ? type : step.Receiver.Length > 0 ? char.ToUpperInvariant(step.Receiver[0]) + step.Receiver[1..] + "Steps" : "";

    /// <summary>
    ///     Whether a given step runs the use case and a later when step, followed by a then step, observes what
    ///     it changed: a listing window is changed in a given, and a refused order shows the window holds.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <param name="home">The use case's home name.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when a later step observes what a given step changed.</returns>
    private static bool ObservedLater(ProjectModel model, ScenarioInfo test, string home, Record useCase)
    {
        var cause = -1;

        for (var index = 0; index < test.Steps.Count; index++)
        {
            var step = test.Steps[index];

            if (cause < 0)
            {
                if (step.Phase == "given" && Reaches(model, step, home, useCase))
                {
                    cause = index;
                }

                continue;
            }

            if (step.Phase == "when")
            {
                return test.Steps.Skip(index + 1).Any(later => later.Phase == "then");
            }
        }

        return false;
    }

    /// <summary>The events that set a background use case off: the triggeredBy of each reaction running it.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>The events that set a background use case off.</returns>
    private static HashSet<string> TriggerEvents(ProjectModel model, Record useCase)
    {
        var trigger = new HashSet<string>(StringComparer.Ordinal);

        foreach (var reaction in model.Records.Where(record => record.Type == RecordTypes.Reaction))
        {
            if (!reaction.EdgeIds(RecordFields.Runs).Contains(useCase.Id, StringComparer.Ordinal))
            {
                continue;
            }

            foreach (var id in reaction.Ids(RecordFields.TriggeredBy))
            {
                if (model.ById.TryGetValue(id, out var target)
                    && target.Type is RecordTypes.Event or RecordTypes.IntegrationEvent)
                {
                    trigger.Add(id);
                }
            }
        }

        return trigger;
    }

    /// <summary>
    ///     Whether a when step sets the use case off from upstream: it drives a declared producer, a use case
    ///     that raises an event a reaction running the use case reacts to or whose own effects run it, or it
    ///     is a step of a declared job trigger.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when a when step sets the use case off from upstream.</returns>
    private static bool Triggers(ProjectModel model, ScenarioInfo test, Record useCase)
    {
        var trigger = TriggerEvents(model, useCase);
        var producers = model.Records
            .Where(record => record.Type == RecordTypes.UseCase
                && record.Id != useCase.Id
                && (record.Ids(RecordFields.Raises).Any(trigger.Contains)
                    || record.EdgeIds(RecordFields.Runs).Contains(useCase.Id, StringComparer.Ordinal)))
            .ToList();

        foreach (var producer in producers)
        {
            if (Identifiers.TrySplit(producer.Id, out _, out _, out var name)
                && name is not null
                && test.Steps.Any(step => step.Phase == "when" && Reaches(model, step, name, producer)))
            {
                return true;
            }
        }

        var jobs = model.Records.Any(record => record.Type == RecordTypes.Job
            && record.EdgeIds(RecordFields.Runs).Contains(useCase.Id, StringComparer.Ordinal));

        return jobs && test.Steps.Any(step =>
            step.Phase == "when"
            && model.Config.JobTriggerSteps.Any(trigger =>
                string.Equals(step.ReceiverType, trigger, StringComparison.Ordinal)
                || string.Equals(step.Receiver, trigger.EndsWith("Steps", StringComparison.Ordinal) ? trigger[..^"Steps".Length] : trigger, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    ///     Whether a then step asserts an effect rather than only the outcome of a request: a step of a class
    ///     named in <c>proof.outcomeOnlySteps</c> states that a call succeeded or was refused, which a job or
    ///     a reaction does not answer, so it proves nothing about what the use case did.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <returns>True when a then step asserts an effect.</returns>
    private static bool AssertsEffect(ProjectModel model, ScenarioInfo test) =>
        test.Steps.Any(step =>
            step.Phase == "then"
            && !model.Config.OutcomeOnlySteps.Any(only =>
                string.Equals(step.ReceiverType, only, StringComparison.Ordinal)
                || string.Equals(step.Receiver, only.EndsWith("Steps", StringComparison.Ordinal) ? only[..^"Steps".Length] : only, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    ///     Whether the test asserts the refusal: the exact code, a client code the
    ///     project's refusal map translates to it, or the HTTP status the project's
    ///     refusal table maps to it, written in the test or asserted inside a then step
    ///     it runs.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="test">The scenario test.</param>
    /// <param name="refusal">The refusal result.</param>
    /// <returns>True when the test asserts the refusal.</returns>
    private static bool Asserts(ProjectModel model, ScenarioInfo test, Record refusal)
    {
        if (refusal.Field(RecordFields.Code) is not { } code)
        {
            return false;
        }

        var thenFacts = test.Steps
            .Where(step => step.Phase == "then")
            .Select(step => model.StepFacts.GetValueOrDefault($"{StepsClassOf(step)}.{step.Method}"))
            .OfType<StepFacts>()
            .ToList();
        var codes = test.AssertedCodes.Concat(thenFacts.SelectMany(facts => facts.Codes)).ToHashSet(StringComparer.Ordinal);
        var statuses = test.Statuses.Concat(thenFacts.SelectMany(facts => facts.Statuses)).ToHashSet();

        if (codes.Contains(code))
        {
            return true;
        }

        if (codes.Any(asserted =>
            model.Config.RefusalAliases.TryGetValue(asserted, out var declared)
            && declared == code))
        {
            return true;
        }

        return model.Config.RefusalStatuses.TryGetValue(code, out var status)
            && statuses.Contains(status);
    }
}
