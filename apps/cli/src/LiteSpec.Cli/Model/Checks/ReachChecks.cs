using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The reach checks: every use case sits in a journey, every declared code literal is recorded, and every screen call matches an endpoint.</summary>
/// <follows>litespec/core/model#connect-every-use-case-to-intent</follows>
/// <follows>litespec/core/layers#write-each-layer-from-the-layer-above-it</follows>
internal static class ReachChecks
{
    /// <summary>Reports use-case-journey-missing.</summary>
    internal static void RunReach(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.Size == "small")
        {
            return;
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ModelBuilder.BackgroundOnly(model, useCase))
            {
                continue;
            }

            var servesStep = useCase.Ids(RecordFields.Serves)
                .Any(id => Identifiers.TrySplitStep(id, out _, out _));

            if (!servesStep)
            {
                findings.Add(new Finding("use-case-journey-missing", $"Use case '{useCase.Id}' sits in no journey.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports behavior-unrecorded.</summary>
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

    /// <summary>The route template with parameter constraints removed, so `{id:guid}` reads `{id}`.</summary>
    private static string Template(string route) => TemplateConstraint.Replace(route, "{$1}");

    private static readonly Regex TemplateConstraint = new(@"\{(?<name>[^}:]+):[^}]+\}", RegexOptions.Compiled);
}
