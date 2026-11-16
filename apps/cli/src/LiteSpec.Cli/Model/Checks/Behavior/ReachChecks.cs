using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The reach checks: every use case sits in a flow, every declared code literal is recorded, every screen call matches an endpoint, every Intent record reaches Behavior, every role is held, and every permission is allowed.</summary>
/// <follows>litespec/core/model#connect-every-behavior-record-to-intent</follows>
/// <follows>litespec/core/layers#write-each-layer-from-the-layer-above-it</follows>
/// <follows>litespec/core/intent#reach-every-intent-record-with-behavior</follows>
/// <follows>litespec/core/access#record-every-role-an-actor-holds</follows>
/// <follows>litespec/core/access#grant-permissions-through-roles</follows>
internal static class ReachChecks
{
    /// <summary>Reports use-case-flow-missing.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunReach(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.Size == "small")
        {
            return;
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ModelBuilder.BackgroundOnly(model, useCase) || UseCaseQueries.RunByUseCase(model, useCase))
            {
                continue;
            }

            var servesStep = useCase.Ids(RecordFields.Serves)
                .Any(id => Identifiers.TrySplitStep(id, out _, out _));

            if (!servesStep)
            {
                findings.Add(new Finding("use-case-flow-missing", $"Use case '{useCase.Id}' sits in no flow.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports behavior-unrecorded.</summary>
    /// <param name="model">The loaded project model that holds every record, the scanned code and each screen.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunBehavior(ProjectModel model, List<Finding> findings)
    {
        // A refusal result declares the code a caller sees. A rule whose refusal is a bare code
        // declares a domain guard that no caller reaches, such as one a validator pre-empts.
        var declared = model.Records
            .Where(record => record.Type == RecordTypes.Result && record.Field(RecordFields.Outcome) == "refusal")
            .Select(record => record.Field(RecordFields.Code))
            .Concat(model.Records
                .Where(record => record.Type == RecordTypes.Rule)
                .Select(record => record.Field(RecordFields.Refusal))
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

    /// <summary>The requirements the steps of one flow cite.</summary>
    /// <param name="flow">The flow record whose steps are read.</param>
    /// <returns>Each requirement identifier a step of the flow cites.</returns>
    private static IEnumerable<string> StepCitesOf(Record flow)
    {
        if (flow.Raw.ValueKind != JsonValueKind.Object
            || !flow.Raw.TryGetProperty(RecordFields.Steps, out var steps)
            || steps.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return steps.EnumerateArray().SelectMany(step => LinkStrings(step, RecordFields.Cites));
    }

    /// <summary>Whether a quality requirement is proved outside Behavior: it names its check, or a test proves it.</summary>
    /// <param name="requirement">The requirement record under inspection.</param>
    /// <param name="proved">The record ids a test proves.</param>
    /// <returns>True when the requirement is a quality requirement with a check or a proving test; otherwise false.</returns>
    private static bool CheckProved(Record requirement, HashSet<string> proved) =>
        requirement.Field(RecordFields.Kind) == "quality"
        && (requirement.Field(RecordFields.Check) is { Length: > 0 } || proved.Contains(requirement.Id));

    /// <summary>Reports intent-unreached.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunIntentUnreached(ProjectModel model, List<Finding> findings)
    {
        // Goals and constraints keep their own codes, so this check covers the other five.
        // A direction promises nothing and reaches no Behavior by design, so no check reads it.
        // A served-content entry fulfills the requirements it names for its entry point.
        var fulfilled = model.Records
            .Where(record => RecordTypes.FulfillingTypes.Contains(record.Type, StringComparer.Ordinal))
            .SelectMany(record => record.Ids(RecordFields.Fulfills))
            .Concat(model.Records.SelectMany(record => record.ServedFulfills()))
            .ToHashSet(StringComparer.Ordinal);

        // A flow reaches the requirements its steps cite; the flow itself holds no cites.
        var flowCites = model.Records
            .Where(record => record.Type == RecordTypes.Flow)
            .SelectMany(record => StepCitesOf(record))
            .ToHashSet(StringComparer.Ordinal);

        var served = model.Records
            .Where(record => RecordTypes.StepServingTypes.Contains(record.Type, StringComparer.Ordinal))
            .SelectMany(record => record.Ids(RecordFields.Serves))
            .ToHashSet(StringComparer.Ordinal);

        var proved = model.Proves.Select(link => link.Claim).ToHashSet(StringComparer.Ordinal);

        var reachingRequirements = new HashSet<string>(StringComparer.Ordinal);
        var reachingFeatures = new HashSet<string>(StringComparer.Ordinal);
        var reachingProblems = new HashSet<string>(StringComparer.Ordinal);
        var reachingFlows = new HashSet<string>(StringComparer.Ordinal);

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement && !record.Derived))
        {
            if (fulfilled.Contains(requirement.Id) || flowCites.Contains(requirement.Id) || CheckProved(requirement, proved))
            {
                reachingRequirements.Add(requirement.Id);
            }
        }

        foreach (var feature in model.Records.Where(record => record.Type == RecordTypes.Feature && !record.Derived))
        {
            if (flowCites.Contains(feature.Id) || RequirementsOf(feature.Id, model).Any(id => reachingRequirements.Contains(id)))
            {
                reachingFeatures.Add(feature.Id);
            }
        }

        var solvedProblems = model.Records
            .Where(record => record.Type == RecordTypes.Feature && reachingFeatures.Contains(record.Id))
            .SelectMany(record => record.Ids(RecordFields.Solves))
            .ToHashSet(StringComparer.Ordinal);

        var citedProblems = model.Records
            .Where(record => record.Type == RecordTypes.Requirement && reachingRequirements.Contains(record.Id))
            .SelectMany(record => record.Ids(RecordFields.Cites))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var problem in model.Records.Where(record => record.Type == RecordTypes.Problem && !record.Derived))
        {
            if (solvedProblems.Contains(problem.Id) || citedProblems.Contains(problem.Id))
            {
                reachingProblems.Add(problem.Id);
            }
        }

        foreach (var flow in model.Records.Where(record => record.Type == RecordTypes.Flow && !record.Derived))
        {
            if (flow.StepIds().Any(step => served.Contains(step)))
            {
                reachingFlows.Add(flow.Id);
            }
        }

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement && !record.Derived))
        {
            if (!reachingRequirements.Contains(requirement.Id))
            {
                findings.Add(new Finding("intent-unreached", $"Requirement '{requirement.Id}' reaches no Behavior.", requirement.File, requirement.Line, SubjectOf(requirement)));
            }
        }

        foreach (var feature in model.Records.Where(record => record.Type == RecordTypes.Feature && !record.Derived))
        {
            if (!reachingFeatures.Contains(feature.Id))
            {
                findings.Add(new Finding("intent-unreached", $"Feature '{feature.Id}' reaches no Behavior.", feature.File, feature.Line, SubjectOf(feature)));
            }
        }

        foreach (var problem in model.Records.Where(record => record.Type == RecordTypes.Problem && !record.Derived))
        {
            if (!reachingProblems.Contains(problem.Id))
            {
                findings.Add(new Finding("intent-unreached", $"Problem '{problem.Id}' reaches no Behavior.", problem.File, problem.Line, SubjectOf(problem)));
            }
        }

        // A flow holds no cites in 7.0.0; its steps cite requirements, so research is reached through a requirement.
        var citedResearch = model.Records
            .Where(record => record.Type == RecordTypes.Requirement && reachingRequirements.Contains(record.Id))
            .SelectMany(record => record.Ids(RecordFields.Cites))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var research in model.Records.Where(record => record.Type == RecordTypes.Research && !record.Derived))
        {
            if (!citedResearch.Contains(research.Id))
            {
                findings.Add(new Finding("intent-unreached", $"Research '{research.Id}' reaches no Behavior.", research.File, research.Line, SubjectOf(research)));
            }
        }

        foreach (var decision in model.Records.Where(record => record.Type == RecordTypes.Decision && !record.Derived && !MarkChecks.UnderImplementationDecisions(model.Config, record.File)))
        {
            var decides = decision.Ids(RecordFields.Decides);

            if (decides.Count == 0)
            {
                continue;
            }

            var reaches = decides.Any(id => DecidedReaches(model, id, reachingRequirements, reachingFeatures, reachingProblems, reachingFlows));

            if (!reaches)
            {
                findings.Add(new Finding("intent-unreached", $"Decision '{decision.Id}' reaches no Behavior.", decision.File, decision.Line, SubjectOf(decision)));
            }
        }
    }

    /// <summary>Reports role-unused.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRoleOrphan(ProjectModel model, List<Finding> findings)
    {
        var held = new HashSet<string>(StringComparer.Ordinal);

        foreach (var actor in model.Records.Where(record => record.Type == RecordTypes.Actor))
        {
            foreach (var role in HoldsOf(actor))
            {
                held.Add(role);
            }
        }

        foreach (var role in model.Records.Where(record => record.Type == RecordTypes.Role && !record.Derived))
        {
            if (!held.Contains(role.Id))
            {
                findings.Add(new Finding("role-unused", $"Role '{role.Id}' is held by no actor.", role.File, role.Line, SubjectOf(role)));
            }
        }
    }

    /// <summary>Reports permission-unused.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunPermissionOrphan(ProjectModel model, List<Finding> findings)
    {
        var allowed = model.Records
            .Where(record => record.Type == RecordTypes.Role)
            .SelectMany(record => record.Ids(RecordFields.Allows))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in model.Records.Where(record => record.Type == RecordTypes.Permission && !record.Derived))
        {
            if (!allowed.Contains(permission.Id))
            {
                findings.Add(new Finding("permission-unused", $"Permission '{permission.Id}' is allowed by no role.", permission.File, permission.Line, SubjectOf(permission)));
            }
        }
    }

    /// <summary>Reports each screen call that matches no endpoint by route or by operation name.</summary>
    /// <param name="model">The loaded project model that holds the scanned endpoints and each screen.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
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

    /// <summary>The requirements a feature holds, by the feature folder that holds them or by identifier home.</summary>
    /// <param name="featureId">The feature identifier.</param>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The identifiers of the requirements the feature holds.</returns>
    private static List<string> RequirementsOf(string featureId, ProjectModel model)
    {
        if (!Identifiers.TrySplit(featureId, out _, out _, out var featureName))
        {
            return [];
        }

        var found = new List<string>();

        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement))
        {
            // The feature whose folder holds the requirement's folder, or the identifier home when no folder names it.
            var folder = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(requirement.File)) ?? "");

            if (string.Equals(folder, featureName, StringComparison.Ordinal)
                || (Identifiers.TrySplit(requirement.Id, out _, out var home, out _)
                    && string.Equals(home, featureName, StringComparison.Ordinal)))
            {
                found.Add(requirement.Id);
            }
        }

        return found;
    }

    /// <summary>Whether a decided record reaches Behavior: a Behavior record, or a reaching requirement, feature, problem or flow.</summary>
    /// <param name="model">The loaded project model that resolves the decided record.</param>
    /// <param name="id">The decided record identifier.</param>
    /// <param name="requirements">The reaching requirement identifiers.</param>
    /// <param name="features">The reaching feature identifiers.</param>
    /// <param name="problems">The reaching problem identifiers.</param>
    /// <param name="flows">The reaching flow identifiers.</param>
    /// <returns>True when the decided record reaches Behavior; otherwise false.</returns>
    private static bool DecidedReaches(ProjectModel model, string id, HashSet<string> requirements, HashSet<string> features, HashSet<string> problems, HashSet<string> flows)
    {
        if (!model.ById.TryGetValue(id, out var record))
        {
            return false;
        }

        if (RecordTypes.LayerByType.TryGetValue(record.Type, out var layer) && layer == "behavior")
        {
            return true;
        }

        return (record.Type == RecordTypes.Requirement && requirements.Contains(id))
            || (record.Type == RecordTypes.Feature && features.Contains(id))
            || (record.Type == RecordTypes.Problem && problems.Contains(id))
            || (record.Type == RecordTypes.Flow && flows.Contains(id));
    }

    /// <summary>The roles an actor holds, from its holds list.</summary>
    /// <param name="actor">The actor record whose holds field is read.</param>
    /// <returns>The role identifiers the actor holds.</returns>
    private static List<string> HoldsOf(Record actor) => actor.Ids(RecordFields.Holds).ToList();

    /// <summary>The route template with parameter constraints removed, so `{id:guid}` reads `{id}`.</summary>
    /// <param name="route">The route template whose parameter constraints are removed.</param>
    /// <returns>The route template with each constrained parameter reduced to its bare name.</returns>
    private static string Template(string route) => TemplateConstraint.Replace(route, "{$1}");

    /// <summary>Matches one constrained route parameter, capturing its name.</summary>
    private static readonly Regex TemplateConstraint = new(@"\{(?<name>[^}:]+):[^}]+\}", RegexOptions.Compiled);
}
