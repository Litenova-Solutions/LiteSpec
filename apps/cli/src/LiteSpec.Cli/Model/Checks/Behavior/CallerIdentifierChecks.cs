using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The caller-identifier checks: every required caller-supplied identifier is returned by a query the caller reaches through the same entry point, or by a query of its module when no entry point reaches it.</summary>
/// <follows>litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query</follows>
internal static class CallerIdentifierChecks
{
    /// <summary>A placeholder in an idempotency key template, written between braces.</summary>
    private static readonly Regex Placeholder = new(@"\{(?<name>[^{}]+)\}", RegexOptions.CultureInvariant);

    /// <summary>Reports each required caller-supplied identifier that no query the caller reaches returns.</summary>
    /// <param name="model">The loaded project model that holds every use case and entry point.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var queries = model.Records
            .Where(record => !record.Derived && record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query")
            .ToList();

        var returned = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            // A duplicate id is identifier-duplicate, so the first record wins here.
            returned.TryAdd(query.Id, ReturnedIdentifies(query));
        }

        var byEntryPoint = new Dictionary<string, List<Record>>(StringComparer.Ordinal);
        var byModule = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            foreach (var point in query.Ids(RecordFields.ReachedThrough).Distinct(StringComparer.Ordinal))
            {
                var basePoint = Identifiers.BaseId(point);

                if (!byEntryPoint.TryGetValue(basePoint, out var reached))
                {
                    reached = [];
                    byEntryPoint[basePoint] = reached;
                }

                reached.Add(query);
            }

            if (LocalityChecks.ModuleOf(model, query) is { Length: > 0 } module)
            {
                if (!byModule.TryGetValue(module, out var owned))
                {
                    owned = [];
                    byModule[module] = owned;
                }

                owned.Add(query);
            }
        }

        foreach (var useCase in model.Records
            .Where(record => !record.Derived && record.Type == RecordTypes.UseCase)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var inputs = CallerIdentifiers(useCase);

            if (inputs.Count == 0)
            {
                continue;
            }

            var points = EntryPointsOf(model, useCase);

            if (points.Count > 0)
            {
                CheckEntryPoints(useCase, inputs, points, byEntryPoint, returned, model, findings);
            }
            else
            {
                CheckModule(model, useCase, inputs, byModule, returned, findings);
            }
        }
    }

    /// <summary>Reports each caller-supplied identifier that no query through the same entry point returns.</summary>
    /// <param name="useCase">The use case whose caller-supplied identifiers are judged.</param>
    /// <param name="inputs">The required caller-supplied identifiers of the use case, each with its field label and identified record.</param>
    /// <param name="points">The entry points reaching the use case, in sorted order.</param>
    /// <param name="byEntryPoint">The queries by the base entry-point or module id their reachedThrough names.</param>
    /// <param name="returned">The identified records each query returns, by query id.</param>
    /// <param name="model">The loaded project model that resolves each identified record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEntryPoints(
        Record useCase,
        IReadOnlyList<(string Field, string Identifies)> inputs,
        IReadOnlyList<string> points,
        Dictionary<string, List<Record>> byEntryPoint,
        Dictionary<string, HashSet<string>> returned,
        ProjectModel model,
        List<Finding> findings)
    {
        foreach (var (field, identifies) in inputs)
        {
            // A dangling identifies is reference-unknown, so only held records count here.
            if (!model.ById.ContainsKey(Identifiers.BaseId(identifies)))
            {
                continue;
            }

            var lacking = new List<string>();

            foreach (var point in points)
            {
                var found = byEntryPoint.TryGetValue(point, out var queries)
                    && queries.Any(query =>
                        !query.Id.Equals(useCase.Id, StringComparison.Ordinal)
                        && returned.TryGetValue(query.Id, out var targets)
                        && targets.Contains(identifies, StringComparer.Ordinal));

                if (!found)
                {
                    lacking.Add(point);
                }
            }

            if (lacking.Count > 0)
            {
                var through = string.Join(", ", lacking.Select(point => $"'{point}'"));

                findings.Add(new Finding(
                    "caller-identifier-unreachable",
                    $"Use case '{useCase.Id}' takes '{field}' identifying '{identifies}' from the caller, but no query through {through} returns '{identifies}'.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports each caller-supplied identifier that no query of the use case module returns.</summary>
    /// <param name="model">The loaded project model that resolves each identified record and module.</param>
    /// <param name="useCase">The use case whose caller-supplied identifiers are judged.</param>
    /// <param name="inputs">The required caller-supplied identifiers of the use case, each with its field label and identified record.</param>
    /// <param name="byModule">The queries by their module name.</param>
    /// <param name="returned">The identified records each query returns, by query id.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModule(
        ProjectModel model,
        Record useCase,
        IReadOnlyList<(string Field, string Identifies)> inputs,
        Dictionary<string, List<Record>> byModule,
        Dictionary<string, HashSet<string>> returned,
        List<Finding> findings)
    {
        if (LocalityChecks.ModuleOf(model, useCase) is not { Length: > 0 } module)
        {
            return;
        }

        foreach (var (field, identifies) in inputs)
        {
            // A dangling identifies is reference-unknown, so only held records count here.
            if (!model.ById.ContainsKey(Identifiers.BaseId(identifies)))
            {
                continue;
            }

            var found = byModule.TryGetValue(module, out var queries)
                && queries.Any(query =>
                    !query.Id.Equals(useCase.Id, StringComparison.Ordinal)
                    && returned.TryGetValue(query.Id, out var targets)
                    && targets.Contains(identifies, StringComparer.Ordinal));

            if (!found)
            {
                findings.Add(new Finding(
                    "module-identifier-unreachable",
                    $"Use case '{useCase.Id}' takes '{field}' identifying '{identifies}' from the caller, but no query of module '{module}' returns '{identifies}'.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The entry points reaching a use case: the reachedThrough ids resolving to an entry point record.</summary>
    /// <param name="model">The loaded project model that resolves each reachedThrough id.</param>
    /// <param name="useCase">The use case whose reachedThrough is read.</param>
    /// <returns>Each reaching entry-point id without its fragment, in sorted order.</returns>
    private static List<string> EntryPointsOf(ProjectModel model, Record useCase)
    {
        var points = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in useCase.Ids(RecordFields.ReachedThrough))
        {
            var baseId = Identifiers.BaseId(id);

            if (model.ById.TryGetValue(baseId, out var record) && record.Type == RecordTypes.EntryPoint)
            {
                points.Add(baseId);
            }
        }

        return points.Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>The required caller-supplied identifiers of a use case: each required input with origin caller and type id, with the row identifiers of each required list input, except the idempotency key placeholders the caller mints.</summary>
    /// <param name="useCase">The use case whose input array is read.</param>
    /// <returns>Each identifier with its field label and identified record, in written order.</returns>
    private static List<(string Field, string Identifies)> CallerIdentifiers(Record useCase)
    {
        var found = new List<(string Field, string Identifies)>();

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Input, out var input)
            || input.ValueKind != JsonValueKind.Array)
        {
            return found;
        }

        var keys = IdempotencyKeyNames(useCase);

        foreach (var entry in input.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || Str(entry, RecordFields.Name) is not { Length: > 0 } name
                || Str(entry, RecordFields.Origin) != "caller"
                || !entry.TryGetProperty(RecordFields.Required, out var required)
                || required.ValueKind != JsonValueKind.True)
            {
                continue;
            }

            if (Str(entry, RecordFields.Type) == "id")
            {
                if (!keys.Contains(name)
                    && Str(entry, RecordFields.Identifies) is { Length: > 0 } target)
                {
                    found.Add((name, target));
                }

                continue;
            }

            if (Str(entry, RecordFields.Type) == "list"
                && entry.TryGetProperty(RecordFields.RowShape, out var shape)
                && shape.ValueKind == JsonValueKind.Object
                && shape.TryGetProperty(RecordFields.Fields, out var rows)
                && rows.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in rows.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Object
                        || Str(row, RecordFields.Type) != "id"
                        || !row.TryGetProperty(RecordFields.Required, out var rowRequired)
                        || rowRequired.ValueKind != JsonValueKind.True
                        || Str(row, RecordFields.Name) is not { Length: > 0 } rowName
                        || Str(row, RecordFields.Identifies) is not { Length: > 0 } rowTarget)
                    {
                        continue;
                    }

                    found.Add(($"{name}.{rowName}", rowTarget));
                }
            }
        }

        return found;
    }

    /// <summary>The placeholder names of the idempotency key template a record holds.</summary>
    /// <param name="record">The use case whose idempotency key is read.</param>
    /// <returns>Each placeholder name once, or an empty set when the record holds no key.</returns>
    private static HashSet<string> IdempotencyKeyNames(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object
            || Str(idempotency, RecordFields.Key) is not { Length: > 0 } key)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return Placeholder.Matches(key)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => name.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The identified records a query returns: each identifies of its output fields and list rows.</summary>
    /// <param name="query">The query whose output include groups are read.</param>
    /// <returns>Each returned identifies link.</returns>
    private static HashSet<string> ReturnedIdentifies(Record query)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        if (query.Raw.ValueKind != JsonValueKind.Object
            || !query.Raw.TryGetProperty(RecordFields.Output, out var output)
            || output.ValueKind != JsonValueKind.Object
            || !output.TryGetProperty(RecordFields.Include, out var include)
            || include.ValueKind != JsonValueKind.Object)
        {
            return found;
        }

        foreach (var group in include.EnumerateObject())
        {
            if (group.Value.ValueKind != JsonValueKind.Object
                || !group.Value.TryGetProperty(RecordFields.Fields, out var fields)
                || fields.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var field in fields.EnumerateArray())
            {
                if (field.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (Str(field, RecordFields.Type) == "id"
                    && Str(field, RecordFields.Identifies) is { Length: > 0 } target)
                {
                    found.Add(target);
                    continue;
                }

                if (Str(field, RecordFields.Type) == "list"
                    && field.TryGetProperty(RecordFields.RowShape, out var shape)
                    && shape.ValueKind == JsonValueKind.Object
                    && shape.TryGetProperty(RecordFields.Fields, out var rows)
                    && rows.ValueKind == JsonValueKind.Array)
                {
                    foreach (var row in rows.EnumerateArray())
                    {
                        if (row.ValueKind == JsonValueKind.Object
                            && Str(row, RecordFields.Type) == "id"
                            && Str(row, RecordFields.Identifies) is { Length: > 0 } rowTarget)
                        {
                            found.Add(rowTarget);
                        }
                    }
                }
            }
        }

        return found;
    }
}
