using System.Text.Json;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The record-graph and advisory checks: orphans, steps, loops, queries,
///     erasure, reactions, jobs and prose.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
internal static class GraphChecks
{
    /// <summary>Reports a use case, reaction, job, notification or setting naming neither serves nor fulfills.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunOrphaned(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record =>
            RecordTypes.UpLinkedTypes.Contains(record.Type, StringComparer.Ordinal)))
        {
            if (record.Ids(RecordFields.Serves).Count == 0 && record.Ids(RecordFields.Fulfills).Count == 0)
            {
                findings.Add(new Finding("record-link-missing", $"{Label(record.Type)} '{record.Id}' names neither serves nor fulfills.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a journey step served by no use case, reaction or job.</summary>
    /// <param name="model">The loaded project model that holds every journey and serving record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunStepUnserved(ProjectModel model, List<Finding> findings)
    {
        var served = model.Records
            .Where(record => RecordTypes.StepServingTypes.Contains(record.Type, StringComparer.Ordinal))
            .SelectMany(record => record.Ids(RecordFields.Serves))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var journey in model.Records.Where(record => record.Type == RecordTypes.Journey))
        {
            foreach (var step in journey.StepIds().Order(StringComparer.Ordinal))
            {
                if (!served.Contains(step))
                {
                    findings.Add(new Finding("step-use-case-missing", $"Journey step '{step}' is served by no {OrList(RecordTypes.StepServingTypes)}.", journey.File, journey.Line, step));
                }
            }
        }
    }

    /// <summary>Reports an effect loop with no stated end.</summary>
    /// <param name="model">The loaded project model that holds every record and its effect edges.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunEffectLoop(ProjectModel model, List<Finding> findings)
    {
        var graph = EffectGraph(model);
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var start in graph.Keys.Order(StringComparer.Ordinal))
        {
            var path = new List<string> { start };
            var inPath = new HashSet<string>(StringComparer.Ordinal) { start };

            WalkLoop(model, graph, start, path, inPath, reported, findings);
        }
    }

    /// <summary>
    ///     The declared effect edges: use-case effects and raises, reaction triggers
    ///     reversed into event edges with the reaction effects, and job effects.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and its effect edges.</param>
    /// <returns>The effect graph by source identifier, with each target list in ordinal order.</returns>
    private static Dictionary<string, List<string>> EffectGraph(ProjectModel model)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Edge(string from, string to)
        {
            var baseId = Identifiers.BaseId(to);

            if (!model.ById.ContainsKey(baseId))
            {
                return;
            }

            if (!graph.TryGetValue(from, out var targets))
            {
                targets = [];
                graph[from] = targets;
            }

            if (!targets.Contains(baseId, StringComparer.Ordinal))
            {
                targets.Add(baseId);
            }
        }

        foreach (var record in model.Records)
        {
            if (record.Type == RecordTypes.UseCase)
            {
                foreach (var entry in EffectEntries(record))
                {
                    if (EffectTarget(entry) is { } target)
                    {
                        Edge(record.Id, target);
                    }
                }

                foreach (var raised in record.Ids(RecordFields.Raises))
                {
                    Edge(record.Id, raised);
                }
            }
            else if (record.Type == RecordTypes.Reaction)
            {
                foreach (var trigger in ReactionEventTriggers(record))
                {
                    var baseId = Identifiers.BaseId(trigger);

                    if (model.ById.TryGetValue(baseId, out var resolved)
                        && resolved.Type is RecordTypes.Event or RecordTypes.IntegrationEvent)
                    {
                        Edge(baseId, record.Id);
                    }
                }

                foreach (var entry in EffectEntries(record))
                {
                    if (EffectTarget(entry) is { } target)
                    {
                        Edge(record.Id, target);
                    }
                }
            }
            else if (record.Type == RecordTypes.Job)
            {
                foreach (var entry in EffectEntries(record))
                {
                    if (EffectTarget(entry) is { } target)
                    {
                        Edge(record.Id, target);
                    }
                }
            }
        }

        foreach (var targets in graph.Values)
        {
            targets.Sort(StringComparer.Ordinal);
        }

        return graph;
    }

    /// <summary>Walks the effect graph depth-first from one node, reporting each loop back to the start.</summary>
    /// <param name="model">The loaded project model that resolves each record in a found loop.</param>
    /// <param name="graph">The effect graph by source identifier.</param>
    /// <param name="current">The node the walk stands on.</param>
    /// <param name="path">The nodes on the current walk, starting with the start node.</param>
    /// <param name="inPath">The nodes on the current walk, for constant-time membership.</param>
    /// <param name="reported">The loop keys already reported, so each loop reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void WalkLoop(
        ProjectModel model,
        Dictionary<string, List<string>> graph,
        string current,
        List<string> path,
        HashSet<string> inPath,
        HashSet<string> reported,
        List<Finding> findings)
    {
        if (!graph.TryGetValue(current, out var targets))
        {
            return;
        }

        foreach (var next in targets)
        {
            if (next == path[0] && path.Count >= 1)
            {
                ReportLoop(model, path, reported, findings);
            }
            else if (inPath.Add(next))
            {
                path.Add(next);
                WalkLoop(model, graph, next, path, inPath, reported, findings);
                path.RemoveAt(path.Count - 1);
                inPath.Remove(next);
            }
        }
    }

    /// <summary>Reports one found effect loop once, unless a domain event and a skipping reaction exempt it.</summary>
    /// <param name="model">The loaded project model that resolves each record in the loop.</param>
    /// <param name="path">The nodes on the walk that closes the loop.</param>
    /// <param name="reported">The loop keys already reported, so each loop reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportLoop(
        ProjectModel model, List<string> path, HashSet<string> reported, List<Finding> findings)
    {
        var turn = path.IndexOf(path.Min(StringComparer.Ordinal)!);
        var rotated = path.Skip(turn).Concat(path.Take(turn)).ToList();
        var key = string.Join(" -> ", rotated);

        if (!reported.Add(key) || LoopExempt(model, rotated))
        {
            return;
        }

        var holder = rotated
            .Select(id => model.ById[id])
            .First(record => record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job);

        findings.Add(new Finding("effect-loop-invalid", $"{Label(holder.Type)} '{holder.Id}' holds an effect loop with no stated end: {key} -> {rotated[0]}.", holder.File, holder.Line, holder.Id));
    }

    /// <summary>
    ///     Whether the loop is allowed: it passes through a domain event, and a
    ///     reaction in the loop states its skip condition as the end condition.
    /// </summary>
    /// <param name="model">The loaded project model that resolves each record in the loop.</param>
    /// <param name="rotated">The loop nodes in canonical order, starting at the smallest identifier.</param>
    /// <returns>True when the loop passes through a domain event and a reaction that states its skip condition; otherwise false.</returns>
    private static bool LoopExempt(ProjectModel model, IReadOnlyList<string> rotated) =>
        rotated.Any(id => model.ById.TryGetValue(id, out var record) && record.Type == RecordTypes.Event)
        && rotated.Any(id => model.ById.TryGetValue(id, out var record)
            && record.Type == RecordTypes.Reaction
            && record.Field(RecordFields.SkippedWhen) is { Length: > 0 });

    /// <summary>The reaction event triggers: the events its triggeredBy names, one id or a list.</summary>
    /// <param name="record">The reaction record whose triggeredBy field is read.</param>
    /// <returns>The triggering event identifiers the reaction names.</returns>
    private static List<string> ReactionEventTriggers(Record record) =>
        record.Ids(RecordFields.TriggeredBy).Where(id => id.Length > 0).ToList();

    /// <summary>Reports a use case with a consistency mode outside atomic, per-record or compensated, or changing aggregates atomically with no cites rule.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunConsistency(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            var mode = ConsistencyMode(useCase);

            if (mode is not null && mode is not ("atomic" or "per-record" or "compensated"))
            {
                findings.Add(new Finding("field-invalid", $"Use case '{useCase.Id}' has consistency mode '{mode}' outside atomic, per-record, compensated.", useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            if (mode != "atomic")
            {
                continue;
            }

            var changes = useCase.Ids(RecordFields.Changes);

            if (changes.Count > 1 && CitesCount(useCase) == 0)
            {
                findings.Add(new Finding("consistency-cites-missing", $"Use case '{useCase.Id}' changes {changes.Count} aggregates atomically with no cites rule.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports an atomic use case whose changes differ from its atomicity rule's binds.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAtomicGroupDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ConsistencyMode(useCase) != "atomic")
            {
                continue;
            }

            var changes = useCase.Ids(RecordFields.Changes);

            if (changes.Count < 2)
            {
                continue;
            }

            foreach (var cited in ConsistencyCites(useCase).Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(cited, out var rule) || rule.Type != RecordTypes.Rule || rule.Field(RecordFields.Kind) != "atomicity")
                {
                    continue;
                }

                var binds = rule.Ids(RecordFields.Binds).Order(StringComparer.Ordinal).ToList();
                var written = changes.Order(StringComparer.Ordinal).ToList();

                if (binds.Count > 0 && !binds.SequenceEqual(written, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("atomic-group-drift", $"Use case '{useCase.Id}' changes '{string.Join("', '", written)}' but its rule '{cited}' binds '{string.Join("', '", binds)}'.", useCase.File, useCase.Line, SubjectOf(useCase)));
                }
            }
        }
    }

    /// <summary>Reports an atomic group binding more than four aggregates.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAtomicGroupInvalid(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "atomicity"))
        {
            var binds = rule.Ids(RecordFields.Binds);

            if (binds.Count > 4)
            {
                findings.Add(new Finding("atomic-group-invalid", $"Rule '{rule.Id}' binds {binds.Count} aggregates.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ConsistencyMode(useCase) != "atomic")
            {
                continue;
            }

            var changes = useCase.Ids(RecordFields.Changes);

            if (changes.Count > 4 && ConsistencyCites(useCase).Count == 0)
            {
                findings.Add(new Finding("atomic-group-invalid", $"Use case '{useCase.Id}' changes {changes.Count} aggregates atomically.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The consistency cites rule ids of the use case.</summary>
    /// <param name="useCase">The use case record whose consistency cites are read.</param>
    /// <returns>The cited rule identifiers as a single value or an array, or an empty list when it cites none.</returns>
    private static IReadOnlyList<string> ConsistencyCites(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency)
            || consistency.ValueKind != JsonValueKind.Object
            || !consistency.TryGetProperty(RecordFields.Cites, out var cites))
        {
            return [];
        }

        if (cites.ValueKind == JsonValueKind.String && cites.GetString() is { Length: > 0 } single)
        {
            return [single];
        }

        if (cites.ValueKind == JsonValueKind.Array)
        {
            return
            [
                .. cites.EnumerateArray()
                          .Where(entry => entry.ValueKind == JsonValueKind.String)
                          .Select(entry => entry.GetString()!),
            ];
        }

        return [];
    }

    /// <summary>The consistency mode a use case declares, as an object field or a leading word.</summary>
    /// <param name="useCase">The use case record whose consistency field is read.</param>
    /// <returns>The declared mode text, or null when the use case declares no consistency.</returns>
    private static string? ConsistencyMode(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency))
        {
            return null;
        }

        if (consistency.ValueKind == JsonValueKind.Object
            && consistency.TryGetProperty(RecordFields.Mode, out var mode)
            && mode.ValueKind == JsonValueKind.String)
        {
            return mode.GetString();
        }

        if (consistency.ValueKind == JsonValueKind.String
            && consistency.GetString() is { } text)
        {
            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        return null;
    }

    /// <summary>Counts the consistency cites of a use case across its string and array forms.</summary>
    /// <param name="useCase">The use case record whose consistency cites are counted.</param>
    /// <returns>The number of cited rules, or zero when it cites none.</returns>
    private static int CitesCount(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency)
            || consistency.ValueKind != JsonValueKind.Object
            || !consistency.TryGetProperty(RecordFields.Cites, out var cites))
        {
            return 0;
        }

        return cites.ValueKind switch
        {
            JsonValueKind.Array => cites.GetArrayLength(),
            JsonValueKind.String when cites.GetString() is { Length: > 0 } => 1,
            _ => 0,
        };
    }

    /// <summary>Reports a query use case declaring no output in include groups.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunQueryOutput(ProjectModel model, List<Finding> findings)
    {
        foreach (var query in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query"))
        {
            if (!HasIncludeGroups(query))
            {
                findings.Add(new Finding("query-output-missing", $"Use case '{query.Id}' declares no output in include groups.", query.File, query.Line, SubjectOf(query)));
            }
        }
    }

    /// <summary>Decides whether a query declares output in include groups.</summary>
    /// <param name="query">The query use case record whose output field is inspected.</param>
    /// <returns>True when the output holds a non-empty include object; otherwise false.</returns>
    private static bool HasIncludeGroups(Record query) =>
        query.Raw.ValueKind == JsonValueKind.Object
        && query.Raw.TryGetProperty(RecordFields.Output, out var output)
        && output.ValueKind == JsonValueKind.Object
        && output.TryGetProperty(RecordFields.Include, out var include)
        && include.ValueKind == JsonValueKind.Object
        && include.EnumerateObject().Any();

    /// <summary>Reports a query use case declaring no paging.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunPaging(ProjectModel model, List<Finding> findings)
    {
        foreach (var query in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query"))
        {
            if (!query.Has(RecordFields.Paging)
                || (query.Raw.TryGetProperty(RecordFields.Paging, out var paging) && paging.ValueKind == JsonValueKind.Null))
            {
                findings.Add(new Finding("paging-undeclared", $"Use case '{query.Id}' declares no paging.", query.File, query.Line, SubjectOf(query)));
            }
        }
    }

    /// <summary>Reports a query widening a group through a cited record that is no requirement.</summary>
    /// <param name="model">The loaded project model that holds every use case record and each cited target.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunWidening(ProjectModel model, List<Finding> findings)
    {
        foreach (var query in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query"))
        {
            if (query.Raw.ValueKind != JsonValueKind.Object
                || !query.Raw.TryGetProperty(RecordFields.Output, out var output)
                || output.ValueKind != JsonValueKind.Object
                || !output.TryGetProperty(RecordFields.Include, out var include)
                || include.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var group in include.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                foreach (var cited in LinkStrings(group.Value, RecordFields.Cites).Order(StringComparer.Ordinal))
                {
                    // An unknown target is left to reference-unknown.
                    if (!model.ById.TryGetValue(cited, out var target))
                    {
                        continue;
                    }

                    if (target.Type != RecordTypes.Requirement)
                    {
                        findings.Add(new Finding("widening-cites-missing", $"Use case '{query.Id}' widens '{group.Name}' through '{cited}', which is no requirement.", query.File, query.Line, SubjectOf(query)));
                    }
                }
            }
        }
    }

    /// <summary>The aggregate data classes, in array or map form.</summary>
    /// <param name="aggregate">The aggregate record whose data classes field is read.</param>
    /// <returns>Each declared data class position, or an empty list when the aggregate declares none.</returns>
    private static List<DataClassPosition> DataClassesOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.DataClasses, out var classes))
        {
            return [];
        }

        var found = new List<DataClassPosition>();

        if (classes.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in classes.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    found.Add(ReadClass(name, entry));
                }
            }
        }
        else if (classes.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in classes.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    found.Add(ReadClass(property.Name, property.Value));
                }
            }
        }

        return found;
    }

    /// <summary>Reads one data class position from its JSON object form.</summary>
    /// <param name="name">The data class name as declared.</param>
    /// <param name="element">The JSON object that holds the class position fields.</param>
    /// <returns>The data class position with each declared field read.</returns>
    private static DataClassPosition ReadClass(string name, JsonElement element) => new(
        name,
        Str(element, RecordFields.Classification) ?? "",
        Str(element, RecordFields.OnErasure),
        Str(element, RecordFields.EnforcedBy),
        LinkStrings(element, RecordFields.Fields),
        ExceptionsOf(element));

    /// <summary>Reads the field exceptions of one data class object.</summary>
    /// <param name="element">The JSON object that holds the exceptions array.</param>
    /// <returns>Each field exception with its erasure position, or an empty list when it declares none.</returns>
    private static List<DataClassException> ExceptionsOf(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(RecordFields.Exceptions, out var exceptions)
            || exceptions.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<DataClassException>();

        foreach (var entry in exceptions.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && Str(entry, RecordFields.Field) is { Length: > 0 } field)
            {
                found.Add(new DataClassException(
                    field,
                    Str(entry, RecordFields.OnErasure),
                    Str(entry, RecordFields.EnforcedBy)));
            }
        }

        return found;
    }

    /// <summary>Reports an aggregate data class or field holding personal data with no erasure position.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunErasure(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            var classes = DataClassesOf(aggregate);

            foreach (var position in classes.OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (Personal(position.Classification) && position.OnErasure is not { Length: > 0 })
                {
                    findings.Add(new Finding("erasure-undeclared", $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' with {position.Classification} data and no erasure position.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            var listed = classes.Any(position => position.Fields.Count > 0);

            foreach (var field in DataOf(aggregate)
                .Where(entry => Personal(entry.Classification))
                .OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (!FieldCovered(classes, listed, field.Name))
                {
                    findings.Add(new Finding("erasure-undeclared", $"Aggregate '{aggregate.Id}' holds field '{field.Name}' with {field.Classification} data and no erasure position.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }
        }
    }

    /// <summary>
    ///     Whether a declared erasure position covers the field: its own exception,
    ///     a class listing it, or any class where no class lists fields.
    /// </summary>
    /// <param name="classes">The data class positions declared on the aggregate.</param>
    /// <param name="listed">Whether any class lists its fields explicitly.</param>
    /// <param name="field">The data field name under test.</param>
    /// <returns>True when an exception, a listing class, or an unlisted class erasure covers the field; otherwise false.</returns>
    private static bool FieldCovered(IReadOnlyList<DataClassPosition> classes, bool listed, string field) =>
        classes.Any(position => position.Exceptions.Any(exception =>
            exception.Field == field && exception.OnErasure is { Length: > 0 }))
        || classes.Any(position => position is { OnErasure: { Length: > 0 } }
            && (!listed || position.Fields.Contains(field, StringComparer.Ordinal)));

    /// <summary>Decides whether a data classification holds personal data.</summary>
    /// <param name="classification">The classification text as declared.</param>
    /// <returns>True for the personal and sensitive classifications, ignoring case; otherwise false.</returns>
    private static bool Personal(string classification) =>
        classification.Equals("personal", StringComparison.OrdinalIgnoreCase)
        || classification.Equals("sensitive", StringComparison.OrdinalIgnoreCase);

    /// <summary>Reports an aggregate data class or field with no enforcedBy.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRetention(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            foreach (var position in DataClassesOf(aggregate).OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (position.EnforcedBy is not { Length: > 0 })
                {
                    findings.Add(new Finding("retention-enforcement-missing", $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' with no enforcedBy.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                foreach (var exception in position.Exceptions.OrderBy(entry => entry.Field, StringComparer.Ordinal))
                {
                    if (exception.OnErasure is { Length: > 0 }
                        && exception.EnforcedBy is not { Length: > 0 }
                        && position.EnforcedBy is not { Length: > 0 })
                    {
                        findings.Add(new Finding("retention-enforcement-missing", $"Aggregate '{aggregate.Id}' holds field '{exception.Field}' of data class '{position.Name}' with no enforcedBy.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                }
            }
        }
    }

    /// <summary>Reports a setting that sets a default with no requirement behind it.</summary>
    /// <param name="model">The loaded project model that holds every setting record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunSettingDefault(ProjectModel model, List<Finding> findings)
    {
        foreach (var setting in model.Records.Where(record => record.Type == RecordTypes.Setting))
        {
            var carries = setting.Raw.ValueKind == JsonValueKind.Object
                && setting.Raw.TryGetProperty(RecordFields.Default, out var value)
                && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

            if (carries && setting.Ids(RecordFields.Fulfills).Count == 0)
            {
                findings.Add(new Finding("setting-requirement-missing", $"Setting '{setting.Id}' sets a default with no requirement behind it.", setting.File, setting.Line, SubjectOf(setting)));
            }
        }
    }

    /// <summary>Reports a reservation rule with no releasedBy, or naming never with no reason.</summary>
    /// <param name="model">The loaded project model that holds every rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunReservation(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "reservation"))
        {
            if (rule.Raw.ValueKind != JsonValueKind.Object
                || !rule.Raw.TryGetProperty(RecordFields.ReleasedBy, out var releasedBy)
                || releasedBy.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                findings.Add(new Finding("reservation-release-missing", $"Rule '{rule.Id}' reserves with no releasedBy.", rule.File, rule.Line, SubjectOf(rule)));
                continue;
            }

            // A releasing record id; the link checks own its target.
            if (releasedBy.ValueKind == JsonValueKind.String
                && releasedBy.GetString() is { Length: > 0 } id
                && Identifiers.TrySplit(id, out _, out _, out _))
            {
                continue;
            }

            if (releasedBy.ValueKind == JsonValueKind.Object
                && releasedBy.TryGetProperty(RecordFields.Mode, out var mode)
                && mode.ValueKind == JsonValueKind.String
                && mode.GetString() == "never")
            {
                if (!releasedBy.TryGetProperty(RecordFields.Reason, out var reason)
                    || reason.ValueKind != JsonValueKind.String
                    || reason.GetString() is not { Length: > 0 })
                {
                    findings.Add(new Finding("reservation-release-missing", $"Rule '{rule.Id}' names never with no reason.", rule.File, rule.Line, SubjectOf(rule)));
                }

                continue;
            }

            findings.Add(new Finding("field-invalid", $"Rule '{rule.Id}' holds releasedBy as neither a record id nor a never object.", rule.File, rule.Line, SubjectOf(rule)));
        }
    }

    /// <summary>Reports a notification sent by no reaction, job or use case.</summary>
    /// <param name="model">The loaded project model that holds every notification and holder record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunNotificationSender(ProjectModel model, List<Finding> findings)
    {
        var sent = new HashSet<string>(StringComparer.Ordinal);

        foreach (var holder in model.Records.Where(record =>
            record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            foreach (var entry in EffectEntries(holder))
            {
                if (Str(entry, RecordFields.Sends) is { Length: > 0 } target)
                {
                    sent.Add(target);
                }
            }
        }

        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification))
        {
            if (!sent.Contains(notification.Id))
            {
                findings.Add(new Finding("notification-sender-missing", $"Notification '{notification.Id}' is sent by no reaction, job or use case.", notification.File, notification.Line, SubjectOf(notification)));
            }
        }
    }

    /// <summary>Reports a requirement naming an except with no fallback, or a fallback carrying its own except.</summary>
    /// <param name="model">The loaded project model that holds every requirement record and fallback target.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunExceptFallback(ProjectModel model, List<Finding> findings)
    {
        foreach (var requirement in model.Records.Where(record => record.Type == RecordTypes.Requirement))
        {
            foreach (var entry in ExceptEntries(requirement))
            {
                var fallbacks = LinkStrings(entry, RecordFields.Fallback);

                if (fallbacks.Count == 0)
                {
                    findings.Add(new Finding("except-fallback-missing", $"Requirement '{requirement.Id}' names an except with no fallback.", requirement.File, requirement.Line, SubjectOf(requirement)));
                    continue;
                }

                foreach (var cited in fallbacks.Order(StringComparer.Ordinal))
                {
                    // An unknown target is left to reference-unknown.
                    if (!model.ById.TryGetValue(cited, out var target))
                    {
                        continue;
                    }

                    if (target.Type == RecordTypes.Requirement && ExceptEntries(target).Count > 0)
                    {
                        findings.Add(new Finding("except-fallback-missing", $"Requirement '{requirement.Id}' names fallback '{cited}' which carries its own except.", requirement.File, requirement.Line, SubjectOf(requirement)));
                    }
                }
            }
        }
    }

    /// <summary>Reports a manual job with no guard.</summary>
    /// <param name="model">The loaded project model that holds every job record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunJobGuard(ProjectModel model, List<Finding> findings)
    {
        foreach (var job in model.Records.Where(record => record.Type == RecordTypes.Job && record.Field(RecordFields.Kind) == "manual"))
        {
            if (!FieldPresent(job, "guard"))
            {
                findings.Add(new Finding("job-guard-missing", $"Job '{job.Id}' is a manual job with no guard.", job.File, job.Line, SubjectOf(job)));
            }
        }
    }

    /// <summary>Decides whether a record field carries a present value in its JSON form.</summary>
    /// <param name="record">The record whose field is tested.</param>
    /// <param name="field">The field name under test.</param>
    /// <returns>True when the field holds a non-empty string, a non-empty array, or any other value; otherwise false.</returns>
    private static bool FieldPresent(Record record, string field)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(field, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => false,
            JsonValueKind.String => value.GetString() is { Length: > 0 },
            JsonValueKind.Array => value.GetArrayLength() > 0,
            _ => true,
        };
    }

    /// <summary>Reports schedule-invalid.</summary>
    /// <param name="model">The loaded project model that holds every job record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunSchedule(ProjectModel model, List<Finding> findings)
    {
        foreach (var job in model.Records.Where(record => record.Type == RecordTypes.Job))
        {
            var structured = StructuredSchedule(job);

            if (job.Field(RecordFields.Kind) == "scheduled" && !structured)
            {
                var missing = !job.Has(RecordFields.Schedule);

                findings.Add(new Finding("schedule-invalid", missing
                    ? $"Job '{job.Id}' runs on time with no structured schedule."
                    : $"Job '{job.Id}' carries a schedule no tool can read.", job.File, job.Line, SubjectOf(job)));
            }
            else if (job.Field(RecordFields.Kind) != "scheduled" && job.Has(RecordFields.Schedule) && !structured)
            {
                findings.Add(new Finding("schedule-invalid", $"Job '{job.Id}' carries a schedule no tool can read.", job.File, job.Line, SubjectOf(job)));
            }
        }
    }

    /// <summary>Decides whether a job carries a schedule a tool can read.</summary>
    /// <param name="job">The job record whose schedule field is inspected.</param>
    /// <returns>True when the schedule object names an interval, a cron form, or startup; otherwise false.</returns>
    private static bool StructuredSchedule(Record job) =>
        job.Raw.ValueKind == JsonValueKind.Object
        && job.Raw.TryGetProperty(RecordFields.Schedule, out var schedule)
        && schedule.ValueKind == JsonValueKind.Object
        && (Str(schedule, RecordFields.Every) is { Length: > 0 } || Str(schedule, RecordFields.Cron) is { Length: > 0 } || Str(schedule, RecordFields.At) == "startup");

    /// <summary>Reports prose-missing.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunProse(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records
            .Where(record => !record.Derived)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (!record.IsEntry && record.Field(RecordFields.Summary) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no summary field.", record.File, record.Line, SubjectOf(record)));
            }

            if (record.Type is RecordTypes.Goal or RecordTypes.Problem or RecordTypes.Requirement or RecordTypes.Rule
                && record.Field(RecordFields.Statement) is not { Length: > 0 })
            {
                findings.Add(new Finding("prose-missing", $"{ProseLabel(record.Type)} '{record.Id}' holds no statement field.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>
    ///     The prose label a prose finding shows for a record type: the type in
    ///     words with its first letter turned, such as Use case.
    /// </summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label, or the generic record label when the type holds none.</returns>
    private static string ProseLabel(string type)
    {
        if (type.Length == 0)
        {
            return "Record";
        }

        var words = type.Replace('-', ' ');

        return char.ToUpper(words[0], System.Globalization.CultureInfo.InvariantCulture) + words[1..];
    }

    /// <summary>Runs the many-aggregates check.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunManyAggregates(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record =>
            record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            if (ConsistencyMode(useCase) == "atomic" && CitesCount(useCase) > 1)
            {
                findings.Add(new Finding("atomic-group-duplicate", $"Use case '{useCase.Id}' saves more than one atomic write group.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports reaction-trigger-unknown.</summary>
    /// <param name="model">The loaded project model that holds every reaction record and event.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunReactionTrigger(ProjectModel model, List<Finding> findings)
    {
        foreach (var reaction in model.Records.Where(record => record.Type == RecordTypes.Reaction))
        {
            var triggers = ReactionEventTriggers(reaction);

            if (triggers.Count == 0)
            {
                findings.Add(new Finding("reaction-trigger-unknown", $"Reaction '{reaction.Id}' names no trigger.", reaction.File, reaction.Line, SubjectOf(reaction)));
                continue;
            }

            foreach (var trigger in triggers.Order(StringComparer.Ordinal))
            {
                var baseId = Identifiers.BaseId(trigger);

                if (!model.ById.TryGetValue(baseId, out var resolved))
                {
                    findings.Add(new Finding("reaction-trigger-unknown", $"Reaction '{reaction.Id}' names trigger '{trigger}' which no event holds.", reaction.File, reaction.Line, SubjectOf(reaction)));
                }
                else if (resolved.Type is not (RecordTypes.Event or RecordTypes.IntegrationEvent))
                {
                    findings.Add(new Finding("reaction-trigger-unknown", $"Reaction '{reaction.Id}' names trigger '{trigger}' which is a {resolved.Type}.", reaction.File, reaction.Line, SubjectOf(reaction)));
                }
            }
        }
    }

    /// <summary>The contract field types a use-case input or payload field may carry.</summary>
    private static readonly HashSet<string> ContractTypes = new(StringComparer.Ordinal)
    {
        "text", "integer", "decimal", "money", "boolean", "date", "date-time", "duration", "email", "choice", "id",
    };

    /// <summary>Reports a use-case input or integration-event payload field outside its typed shape.</summary>
    /// <param name="model">The loaded project model that holds every use case and integration event record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunTypedInputs(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            CheckContractFields(model, useCase, "input", "Use case", findings);
        }

        foreach (var integration in model.Records.Where(record => record.Type == RecordTypes.IntegrationEvent))
        {
            CheckContractFields(model, integration, "payload", "Integration event", findings);

            if (integration.Raw.ValueKind == JsonValueKind.Object
                && integration.Raw.TryGetProperty(RecordFields.Dataschema, out var schema)
                && schema.ValueKind == JsonValueKind.String
                && schema.GetString() is { Length: > 0 } url
                && !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                findings.Add(new Finding("field-invalid", $"Integration event '{integration.Id}' holds dataschema '{url}', which is no URL.", integration.File, integration.Line, SubjectOf(integration)));
            }
        }
    }

    /// <summary>Reports one contract field holding no type, no required, or a dangling of.</summary>
    /// <param name="model">The loaded project model that resolves each id field target.</param>
    /// <param name="record">The use case or integration event record whose contract fields are judged.</param>
    /// <param name="array">The contract array field name, input or payload.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractFields(ProjectModel model, Record record, string array, string label, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(array, out var fields)
            || fields.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in fields.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = Str(entry, RecordFields.Name) ?? "(no name)";
            var type = Str(entry, RecordFields.Type);

            if (type is not { Length: > 0 })
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with no type.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (!ContractTypes.Contains(type))
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with type '{type}' outside {string.Join(", ", ContractTypes.Order(StringComparer.Ordinal))}.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (!entry.TryGetProperty(RecordFields.Required, out var required)
                || required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' with no required.", record.File, record.Line, SubjectOf(record)));
            }

            CheckContractOf(model, record, label, array, name, type, entry, findings);
        }
    }

    /// <summary>Reports an of that names no record for an id, or no choices for a choice.</summary>
    /// <param name="model">The loaded project model that resolves the id field target.</param>
    /// <param name="record">The record that holds the contract field.</param>
    /// <param name="label">The record kind description that opens each finding.</param>
    /// <param name="array">The contract array field name, input or payload.</param>
    /// <param name="name">The contract field name quoted in each finding.</param>
    /// <param name="type">The contract field type that decides the of shape.</param>
    /// <param name="entry">The contract field object whose of value is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractOf(ProjectModel model, Record record, string label, string array, string name, string type, JsonElement entry, List<Finding> findings)
    {
        var has = entry.TryGetProperty(RecordFields.Of, out var of);

        if (type == "id")
        {
            if (!has || of.ValueKind != JsonValueKind.String || of.GetString() is not { Length: > 0 } target)
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type id with no of record.", record.File, record.Line, SubjectOf(record)));
                return;
            }

            if (!model.ById.ContainsKey(Identifiers.BaseId(target)))
            {
                findings.Add(new Finding("reference-unknown", $"{label} '{record.Id}' holds {array} field '{name}' of '{target}', which no record holds.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (type == "choice")
        {
            if (!has || of.ValueKind != JsonValueKind.Array || of.GetArrayLength() == 0)
            {
                findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type choice with no of choices.", record.File, record.Line, SubjectOf(record)));
            }

            return;
        }

        if (has)
        {
            findings.Add(new Finding("field-invalid", $"{label} '{record.Id}' holds {array} field '{name}' of type {type} with an of that only id and choice carry.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a scenario exercise naming no rule.</summary>
    /// <param name="model">The loaded project model that holds every scenario record and rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunExercises(ProjectModel model, List<Finding> findings)
    {
        foreach (var scenario in model.Records.Where(record => record.Type == RecordTypes.Scenario))
        {
            foreach (var id in scenario.Ids(RecordFields.Exercises).Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(id, out var target))
                {
                    findings.Add(new Finding("reference-unknown", $"Scenario '{scenario.Id}' exercises '{id}', which no record holds.", scenario.File, scenario.Line, SubjectOf(scenario)));
                }
                else if (target.Type != RecordTypes.Rule)
                {
                    findings.Add(new Finding("field-invalid", $"Scenario '{scenario.Id}' exercises '{id}', which is a {target.Type}.", scenario.File, scenario.Line, SubjectOf(scenario)));
                }
            }
        }
    }

    /// <summary>The door auth values an entry point may state.</summary>
    private static readonly HashSet<string> DoorAuth = new(StringComparer.Ordinal)
    {
        "session", "token", "device", "link", "app", "none",
    };

    /// <summary>Reports a door stating an auth outside its six values.</summary>
    /// <param name="model">The loaded project model that holds every entry point record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunDoors(ProjectModel model, List<Finding> findings)
    {
        foreach (var point in model.Records.Where(record => record.Type == RecordTypes.EntryPoint))
        {
            foreach (var auth in point.Ids(RecordFields.Auth).Order(StringComparer.Ordinal))
            {
                if (!DoorAuth.Contains(auth))
                {
                    findings.Add(new Finding("field-invalid", $"Entry point '{point.Id}' holds auth '{auth}' outside {string.Join(", ", DoorAuth)}.", point.File, point.Line, SubjectOf(point)));
                }
            }
        }
    }

    /// <summary>The one target an effect entry names, across its four verbs.</summary>
    /// <param name="entry">The effect entry whose target is read.</param>
    /// <returns>The first non-empty target across the four edge verbs, or null when the entry names none.</returns>
    private static string? EffectTarget(JsonElement entry) =>
        (Str(entry, RecordFields.Runs) ?? Str(entry, RecordFields.Sends) ?? Str(entry, RecordFields.Emits) ?? Str(entry, RecordFields.Calls)) is { Length: > 0 } target
            ? target
            : null;

    /// <summary>One field holding one id or an array of ids, read from any element.</summary>
    /// <param name="element">The JSON object that holds the field.</param>
    /// <param name="name">The field name whose identifiers are read.</param>
    /// <returns>The single identifier or the array identifiers, or an empty list when the field is absent or misshaped.</returns>
    private static IReadOnlyList<string> LinkStrings(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.String && value.GetString() is { } single)
        {
            return [single];
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return
            [
                .. value.EnumerateArray()
                          .Where(entry => entry.ValueKind == JsonValueKind.String)
                          .Select(entry => entry.GetString()!),
            ];
        }

        return [];
    }
}
