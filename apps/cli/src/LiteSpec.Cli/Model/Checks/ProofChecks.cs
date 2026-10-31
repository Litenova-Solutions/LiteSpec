using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The proof checks: every use case has a success result and, at the large size, a scenario, every written refusal is thrown, and every claimed proof is reached and steady.</summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
/// <follows>litespec/core/model#give-every-use-case-its-results</follows>
/// <follows>litespec/core/model#report-unverified-use-cases</follows>
internal static class ProofChecks
{
    /// <summary>
    ///     Whether a refusal code matches a template the project composes at run time and its key is a literal in
    ///     the code, the way a commit answers a lost uniqueness key from the key's scope.
    /// </summary>
    /// <param name="model">The loaded project model that holds the refusal templates and the scanned code literals.</param>
    /// <param name="code">The refusal code under test.</param>
    /// <returns>True when a composed refusal template matches the code and its key appears as a code literal; otherwise false.</returns>
    private static bool IsComposedFromLiteral(ProjectModel model, string code) =>
        model.Config.RefusalComposed.Any(template => RefusalTemplate.TryMatch(template, code, out var key) && model.Code.KeyLiterals.Contains(key));

    /// <summary>Reports use-case-success-missing, use-case-scenario-missing, use-case-unimplemented, scenario-result-unknown, refusal-unimplemented and proves-unreached.</summary>
    /// <param name="model">The loaded project model that holds every record, each test and the proof links.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var results = model.Records.Where(record => record.Type == RecordTypes.Result).ToList();
        var thrown = model.Code.Literals.Select(literal => literal.Value).ToHashSet(StringComparer.Ordinal);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
            {
                continue;
            }

            var own = results.Where(result =>
                Identifiers.TrySplit(result.Id, out _, out var home, out _) && home == name).ToList();

            if (!own.Any(result => result.Field(RecordFields.Outcome) == "success"))
            {
                findings.Add(new Finding("use-case-success-missing", $"Use case '{useCase.Id}' has no success result.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (model.Config.Size == "large"
                && !model.Records.Any(record => record.Type == RecordTypes.Scenario
                    && Identifiers.TrySplit(record.Id, out _, out var scenarioHome, out _) && scenarioHome == name))
            {
                findings.Add(new Finding("use-case-scenario-missing", $"Use case '{useCase.Id}' has no scenario.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (model.Status.TryGetValue(useCase.Id, out var status) && !status.Implemented)
            {
                findings.Add(new Finding("use-case-unimplemented", $"Use case '{useCase.Id}' has no handler, reaction or job.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }

        foreach (var scenario in model.Records.Where(record => record.Type == RecordTypes.Scenario))
        {
            var resultIds = scenario.Ids(RecordFields.Result);
            var resultId = resultIds.Count > 0 ? resultIds[0] : null;

            if (resultId is null)
            {
                continue;
            }

            if (!model.ById.TryGetValue(resultId, out var result) || result.Type != RecordTypes.Result)
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
        foreach (var result in results.Where(left => !left.Derived && left.Field(RecordFields.Outcome) == "refusal"))
        {
            if (result.Field(RecordFields.Code) is { } code && !thrown.Contains(code) && !IsComposedFromLiteral(model, code))
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

    /// <summary>
    ///     Whether the scenario ends where it may: a reaction or job scenario in a result of
    ///     the use case its holder runs, any other scenario in a result of its own
    ///     use case, written or derived, both named result/.&lt;use-case&gt;.&lt;name&gt;.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the scenario page and its holder.</param>
    /// <param name="scenario">The scenario record whose ending result is tested.</param>
    /// <param name="result">The result record the scenario names as its ending.</param>
    /// <returns>True when the result belongs to the use case the scenario proves; otherwise false.</returns>
    private static bool ResultBelongsToScenario(ProjectModel model, Record scenario, Record result)
    {
        if (scenario.PageId is not null
            && model.ById.TryGetValue(scenario.PageId, out var page)
            && page.Type is RecordTypes.Reaction or RecordTypes.Job
            && page.EdgeIds(RecordFields.Runs) is { Count: > 0 } runIds
            && runIds[0] is { } runs
            && model.ById.TryGetValue(runs, out var target)
            && target.Type == RecordTypes.UseCase
            && Identifiers.TrySplit(runs, out _, out _, out var operation))
        {
            return Identifiers.TrySplit(result.Id, out _, out var home, out _)
                && home == operation;
        }

        var scenarioHome = Identifiers.TrySplit(scenario.Id, out _, out var left, out _) ? left : null;

        var resultHome = Identifiers.TrySplit(result.Id, out _, out var right, out _) ? right : null;

        return scenarioHome is not null && scenarioHome == resultHome;
    }

    /// <summary>Reports a use case holding scenarios with no passing proof.</summary>
    /// <param name="model">The loaded project model that holds every record and each computed proof status.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunUnverified(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var own = model.Records.Any(record => record.Type == RecordTypes.Scenario
                && Identifiers.TrySplit(record.Id, out _, out var home, out _) && home == name);

            if (!own)
            {
                continue;
            }

            if (model.Status.TryGetValue(useCase.Id, out var status) && !status.Verified)
            {
                findings.Add(new Finding("use-case-unverified", $"Use case '{useCase.Id}' has scenarios with no passing proof.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports a test that both passed and failed within the runs read.</summary>
    /// <param name="model">The loaded project model that holds each test, each browser test and the read test runs.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunProofInvalid(ProjectModel model, List<Finding> findings)
    {
        if (model.TestRun is null)
        {
            return;
        }

        foreach (var name in model.TestRun.Flaky.Order(StringComparer.Ordinal))
        {
            var test = model.Tests.FirstOrDefault(candidate => candidate.Name == name);

            if (test is not null)
            {
                var subject = test.Claims
                    .Where(claim => model.ById.ContainsKey(claim))
                    .Order(StringComparer.Ordinal)
                    .FirstOrDefault();

                findings.Add(new Finding("proof-invalid", $"Test '{name}' passed and failed within the runs read.", test.File, test.Line, subject));
                continue;
            }

            var browser = model.BrowserTests.FirstOrDefault(candidate => candidate.Title == name);

            if (browser is not null)
            {
                var subject = browser.Proves
                    .Where(claim => model.ById.ContainsKey(claim))
                    .Order(StringComparer.Ordinal)
                    .FirstOrDefault();

                findings.Add(new Finding("proof-invalid", $"Test '{name}' passed and failed within the runs read.", browser.File, browser.Line, subject));
            }
        }
    }
}
