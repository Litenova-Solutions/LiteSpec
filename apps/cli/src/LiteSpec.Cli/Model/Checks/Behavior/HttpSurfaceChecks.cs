using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The HTTP surface checks: every use case reached through an api or browser entry point states its method and route with each input bound, each route parameter names a route input, a claim input takes a verified-claim origin, each cited refusal states its status, and each created location names an output field.</summary>
/// <follows>litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case</follows>
/// <follows>litespec/core/use-cases#declare-the-binding-of-every-input-of-an-http-reached-use-case</follows>
/// <follows>litespec/core/use-cases#declare-the-status-of-every-refusal-an-http-reached-use-case-cites</follows>
/// <follows>litespec/core/use-cases#declare-the-created-location-of-every-command-that-creates-a-resource</follows>
internal static class HttpSurfaceChecks
{
    /// <summary>A route parameter in a route template, written between braces.</summary>
    private static readonly Regex Parameter = new(@"\{(?<name>[^{}]+)\}", RegexOptions.CultureInvariant);

    /// <summary>Reports an HTTP-reached use case with no HTTP surface, an unbound or misbound input, a route parameter naming no route input, a cited refusal with no status, and a location parameter naming no output field.</summary>
    /// <param name="model">The loaded project model that holds every record and refusal set.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var kinds = EntryPointKinds(model);
        var statuses = RefusalStatuses(model);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!IsHttpReached(useCase, kinds))
            {
                continue;
            }

            CheckSurface(useCase, findings);
            CheckInputs(useCase, findings);
            CheckRoute(useCase, findings);
            CheckRefusals(model, useCase, statuses, findings);
            CheckCreated(useCase, findings);
        }
    }

    /// <summary>The kind of each entry point, by entry point id.</summary>
    /// <param name="model">The loaded project model that holds every entry point.</param>
    /// <returns>The entry point kinds, by id.</returns>
    private static Dictionary<string, string> EntryPointKinds(ProjectModel model)
    {
        var kinds = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in model.Records.Where(record => record.Type == RecordTypes.EntryPoint))
        {
            if (entry.Field(RecordFields.Kind) is { Length: > 0 } kind)
            {
                kinds[entry.Id] = kind;
            }
        }

        return kinds;
    }

    /// <summary>Whether the use case is reached through an api or browser entry point.</summary>
    /// <param name="useCase">The use case record whose reachedThrough is read.</param>
    /// <param name="kinds">The entry point kinds, by entry point id.</param>
    /// <returns>True when an api or browser entry point reaches the use case; otherwise false.</returns>
    private static bool IsHttpReached(Record useCase, Dictionary<string, string> kinds) =>
        useCase.Ids(RecordFields.ReachedThrough)
            .Any(id => kinds.TryGetValue(id, out var kind) && kind is "api" or "browser");

    /// <summary>Whether the refusal states its HTTP status, by refusal code.</summary>
    /// <param name="model">The loaded project model that holds every refusal set.</param>
    /// <returns>True for each code whose refusal states a status, by code.</returns>
    private static Dictionary<string, bool> RefusalStatuses(ProjectModel model)
    {
        var statuses = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var entry in model.RefusalSets)
        {
            statuses.TryAdd(entry.Code, entry.Status is not null);
        }

        return statuses;
    }

    /// <summary>Reports an HTTP-reached use case stating no HTTP surface; the shape check owns an incomplete one.</summary>
    /// <param name="useCase">The HTTP-reached use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSurface(Record useCase, List<Finding> findings)
    {
        if (!useCase.Has(RecordFields.Http))
        {
            findings.Add(new Finding(
                "http-surface-missing",
                $"Use case '{useCase.Id}' is reached through an api or browser entry point and states no HTTP method and route.",
                useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>Reports an input with no binding and a binding contradicting its origin.</summary>
    /// <param name="useCase">The HTTP-reached use case record whose inputs are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckInputs(Record useCase, List<Finding> findings)
    {
        foreach (var input in useCase.Objects(RecordFields.Input))
        {
            var name = Str(input, RecordFields.Name) ?? "";
            var origin = Str(input, RecordFields.Origin);
            var binding = Str(input, RecordFields.Binding);

            if (origin is not { Length: > 0 })
            {
                continue;
            }

            if (binding is not { Length: > 0 })
            {
                if (origin != "derived")
                {
                    findings.Add(new Finding(
                        "input-binding-missing",
                        $"Use case '{useCase.Id}' input '{name}' states no binding.",
                        useCase.File, useCase.Line, SubjectOf(useCase)));
                }

                continue;
            }

            if (binding == "claim" && origin != "verified-claim")
            {
                findings.Add(new Finding(
                    "input-binding-invalid",
                    $"Use case '{useCase.Id}' input '{name}' binds from the claim but takes origin '{origin}'; a claim input takes origin verified-claim.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
            else if (origin == "derived")
            {
                findings.Add(new Finding(
                    "input-binding-invalid",
                    $"Use case '{useCase.Id}' input '{name}' is derived and states binding '{binding}'; a derived input states no binding.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports a route parameter naming no route input of the use case.</summary>
    /// <param name="useCase">The HTTP-reached use case record whose route is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRoute(Record useCase, List<Finding> findings)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Http, out var http)
            || http.ValueKind != JsonValueKind.Object
            || Str(http, RecordFields.Route) is not { Length: > 0 } route)
        {
            return;
        }

        var names = Parameter.Matches(route)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (names.Count == 0)
        {
            return;
        }

        var routeInputs = useCase.Objects(RecordFields.Input)
            .Where(input => Str(input, RecordFields.Binding) == "route")
            .Select(input => (Name: Str(input, RecordFields.Name) ?? "", Type: Str(input, RecordFields.Type) ?? ""))
            .Where(input => input.Name.Length > 0)
            .ToList();

        foreach (var name in names)
        {
            if (!routeInputs.Any(input => NamesInput(name, input.Name, input.Type)))
            {
                findings.Add(new Finding(
                    "http-route-param-unknown",
                    $"Use case '{useCase.Id}' names route parameter '{name}' in '{route}', which no route input of the use case carries.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Whether a route parameter names a route input: the input name, or the input name plus Id for an id, which names its record while the parameter names its identity.</summary>
    /// <param name="parameter">The route parameter name.</param>
    /// <param name="input">The route input name.</param>
    /// <param name="type">The route input value type.</param>
    /// <returns>True when the parameter names the input; otherwise false.</returns>
    internal static bool NamesInput(string parameter, string input, string type) =>
        string.Equals(parameter, input, StringComparison.Ordinal)
        || (type == "id" && string.Equals(parameter, input + "Id", StringComparison.Ordinal));

    /// <summary>Reports a created-location parameter naming no output field of the use case.</summary>
    /// <param name="useCase">The HTTP-reached use case record whose location is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCreated(Record useCase, List<Finding> findings)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Http, out var http)
            || http.ValueKind != JsonValueKind.Object
            || Str(http, RecordFields.Created) is not { Length: > 0 } created)
        {
            return;
        }

        var names = Parameter.Matches(created)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (names.Count == 0)
        {
            return;
        }

        var outputs = OutputFieldsOf(useCase);

        foreach (var name in names)
        {
            if (!outputs.Any(output => NamesInput(name, output.Name, output.Type)))
            {
                findings.Add(new Finding(
                    "http-location-param-unknown",
                    $"Use case '{useCase.Id}' names location parameter '{name}' in '{created}', which no output field of the use case carries.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The output fields of a use case: its command array output, or its query groups flattened.</summary>
    /// <param name="useCase">The use case record whose output is read.</param>
    /// <returns>The output field names with their record types.</returns>
    private static List<(string Name, string Type)> OutputFieldsOf(Record useCase)
    {
        var fields = new List<(string Name, string Type)>();

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            return fields;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in output.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    fields.Add((name, Str(entry, RecordFields.Type) ?? ""));
                }
            }

            return fields;
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind != JsonValueKind.Object
                    || !group.Value.TryGetProperty(RecordFields.Fields, out var array)
                    || array.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var entry in array.EnumerateArray())
                {
                    if (entry.ValueKind == JsonValueKind.Object
                        && Str(entry, RecordFields.Name) is { Length: > 0 } name)
                    {
                        fields.Add((name, Str(entry, RecordFields.Type) ?? ""));
                    }
                }
            }
        }

        return fields;
    }

    /// <summary>Reports a refusal cited by a result of the use case that states no HTTP status.</summary>
    /// <param name="model">The loaded project model that holds every result.</param>
    /// <param name="useCase">The HTTP-reached use case record whose results are judged.</param>
    /// <param name="statuses">Whether each refusal states its status, by refusal code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRefusals(ProjectModel model, Record useCase, Dictionary<string, bool> statuses, List<Finding> findings)
    {
        foreach (var result in model.Records.Where(record =>
            record.Type == RecordTypes.Result
            && string.Equals(record.PageId, useCase.Id, StringComparison.Ordinal)
            && record.Field(RecordFields.Outcome) == "refusal"))
        {
            var refusal = result.Field(RecordFields.Refusal);

            if (RefusalCode.CodeOf(refusal) is not { Length: > 0 } code)
            {
                continue;
            }

            if (statuses.TryGetValue(code, out var stated) && !stated)
            {
                findings.Add(new Finding(
                    "refusal-status-missing",
                    $"Result '{result.Id}' of use case '{useCase.Id}' cites refusal '{refusal}' with no HTTP status.",
                    result.File, result.Line, SubjectOf(result)));
            }
        }
    }
}
