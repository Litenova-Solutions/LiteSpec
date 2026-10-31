using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The record-graph and advisory checks: orphans, steps, loops, queries,
///     erasure, approval, transcription, reactions, jobs, prose and aged questions.
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
            if (record.Status == RecordStatuses.Retired)
            {
                continue;
            }

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
            .Where(record => RecordTypes.StepServingTypes.Contains(record.Type, StringComparer.Ordinal) && record.Status != RecordStatuses.Retired)
            .SelectMany(record => record.Ids(RecordFields.Serves))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var journey in model.Records.Where(record => record.Type == RecordTypes.Journey && record.Status != RecordStatuses.Retired))
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

    /// <summary>Reports an atomic group binding more than four aggregates, a saga candidate.</summary>
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

    /// <summary>Reports a query widening a group through a cited record that is no approved requirement.</summary>
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

                    if (target.Type != RecordTypes.Requirement || !LiveApproved(model, target))
                    {
                        findings.Add(new Finding("widening-cites-missing", $"Use case '{query.Id}' widens '{group.Name}' through '{cited}', which is no approved requirement.", query.File, query.Line, SubjectOf(query)));
                    }
                }
            }
        }
    }

    /// <summary>Whether the record stands approved: by effective status, else by status.</summary>
    /// <param name="model">The loaded project model that holds the effective status of each record.</param>
    /// <param name="record">The record whose approval is tested.</param>
    /// <returns>True when the effective status, else the written status, stands approved; otherwise false.</returns>
    private static bool LiveApproved(ProjectModel model, Record record) =>
        model.EffectiveStatus.TryGetValue(record.Id, out var effective)
            ? effective == RecordStatuses.Approved
            : record.Status == RecordStatuses.Approved;

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
        Str(element, RecordFields.ReviewBy),
        LinkStrings(element, RecordFields.AcceptedBy),
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
                    Str(entry, RecordFields.EnforcedBy),
                    Str(entry, RecordFields.ReviewBy),
                    LinkStrings(entry, RecordFields.AcceptedBy)));
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

    /// <summary>Reports an aggregate whose risk stands accepted by no Intent record.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record and each acceptance target.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunRiskAcceptance(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            foreach (var position in DataClassesOf(aggregate).OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (Personal(position.Classification) && Kept(position.OnErasure))
                {
                    CheckAcceptedBy(model, aggregate, position.AcceptedBy, $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' keeping {position.Classification} data with '{position.OnErasure}'", findings);
                }

                foreach (var exception in position.Exceptions.OrderBy(entry => entry.Field, StringComparer.Ordinal))
                {
                    if (Personal(position.Classification) && Kept(exception.OnErasure))
                    {
                        var accepted = exception.AcceptedBy.Count > 0 ? exception.AcceptedBy : position.AcceptedBy;

                        CheckAcceptedBy(model, aggregate, accepted, $"Aggregate '{aggregate.Id}' holds field '{exception.Field}' of data class '{position.Name}' keeping {position.Classification} data with '{exception.OnErasure}'", findings);
                    }
                }
            }
        }
    }

    /// <summary>Decides whether an erasure position keeps the data in a form that needs acceptance.</summary>
    /// <param name="onErasure">The erasure position text, or null when none is declared.</param>
    /// <returns>True for retain, exempt and pseudonymize, ignoring case; otherwise false.</returns>
    private static bool Kept(string? onErasure) =>
        onErasure is not null
        && (onErasure.Equals("retain", StringComparison.OrdinalIgnoreCase)
            || onErasure.Equals("exempt", StringComparison.OrdinalIgnoreCase)
            || onErasure.Equals("pseudonymize", StringComparison.OrdinalIgnoreCase));

    /// <summary>Reports kept personal data whose acceptance names no Intent record.</summary>
    /// <param name="model">The loaded project model that resolves each acceptedBy target.</param>
    /// <param name="aggregate">The aggregate record the finding is placed on.</param>
    /// <param name="acceptedBy">The acceptance identifiers the position declares.</param>
    /// <param name="stem">The finding message opening that names the kept data.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAcceptedBy(
        ProjectModel model,
        Record aggregate,
        IReadOnlyList<string> acceptedBy,
        string stem,
        List<Finding> findings)
    {
        if (acceptedBy.Count == 0)
        {
            findings.Add(new Finding("risk-acceptance-missing", $"{stem} and no acceptedBy in Intent.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            return;
        }

        foreach (var cited in acceptedBy.Order(StringComparer.Ordinal))
        {
            // An unknown target is left to reference-unknown.
            if (!model.ById.TryGetValue(cited, out var target))
            {
                continue;
            }

            if (target.Type is not (RecordTypes.Decision or RecordTypes.Requirement))
            {
                findings.Add(new Finding("risk-acceptance-missing", $"{stem} and acceptedBy '{cited}', which is no Intent record.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }
        }
    }

    /// <summary>Reports an exempt aggregate with no reviewBy date or past it.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunExemption(ProjectModel model, List<Finding> findings)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            foreach (var position in DataClassesOf(aggregate).OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (Exempt(position.OnErasure))
                {
                    CheckReviewBy(aggregate, position.ReviewBy, $"Aggregate '{aggregate.Id}' holds data class '{position.Name}' that is exempt", today, findings);
                }

                foreach (var exception in position.Exceptions.OrderBy(entry => entry.Field, StringComparer.Ordinal))
                {
                    if (Exempt(exception.OnErasure))
                    {
                        CheckReviewBy(aggregate, exception.ReviewBy ?? position.ReviewBy, $"Aggregate '{aggregate.Id}' holds field '{exception.Field}' of data class '{position.Name}' that is exempt", today, findings);
                    }
                }
            }
        }
    }

    /// <summary>Decides whether an erasure position exempts the data from erasure.</summary>
    /// <param name="onErasure">The erasure position text, or null when none is declared.</param>
    /// <returns>True for the exempt position, ignoring case; otherwise false.</returns>
    private static bool Exempt(string? onErasure) =>
        onErasure is not null && onErasure.Equals("exempt", StringComparison.OrdinalIgnoreCase);

    /// <summary>Reports exempt data whose review date is missing or stands in the past.</summary>
    /// <param name="aggregate">The aggregate record the finding is placed on.</param>
    /// <param name="reviewBy">The review date text as declared, or null when none is declared.</param>
    /// <param name="stem">The finding message opening that names the exempt data.</param>
    /// <param name="today">The current date the review date is aged against.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckReviewBy(
        Record aggregate, string? reviewBy, string stem, DateOnly today, List<Finding> findings)
    {
        if (reviewBy is not { Length: > 0 })
        {
            findings.Add(new Finding("exemption-expired", $"{stem} with no reviewBy date.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
        }
        else if (ParseDay(reviewBy, out var day) && today > day)
        {
            findings.Add(new Finding("exemption-expired", $"{stem} past its reviewBy date {reviewBy}.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
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
            record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job && record.Status != RecordStatuses.Retired))
        {
            foreach (var entry in EffectEntries(holder))
            {
                if (Str(entry, RecordFields.Sends) is { Length: > 0 } target)
                {
                    sent.Add(target);
                }
            }
        }

        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification && record.Status != RecordStatuses.Retired))
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

    /// <summary>Reports an approved record whose fields differ from its approvedHash.</summary>
    /// <param name="model">The loaded project model that holds every record and its approval hash.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunApproval(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record =>
            (record.Status == RecordStatuses.Approved || record.Status == RecordStatuses.Reviewed) && !record.Derived))
        {
            if (record.ApprovedHash is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' is {record.Status} with no approvedHash.", record.File, record.Line, SubjectOf(record)));
            }
            else if (Checker.SemanticHash(record) != record.ApprovedHash)
            {
                findings.Add(new Finding("approval-reopened", $"Record '{record.Id}' holds semantic fields that differ from its approvedHash.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports transcription-status-invalid and transcription-expired.</summary>
    /// <param name="model">The loaded project model that holds every record and the adoption end state.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunTranscribed(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record =>
            record.Field(RecordFields.Provenance) == "transcribed" && record.Status == RecordStatuses.Reviewed))
        {
            // Only an Intent parent that exists can hold the record back; a missing or
            // dangling parent is record-link-missing or reference-unknown, never this code.
            var parents = record.Ids(RecordFields.Fulfills).Concat(record.Ids(RecordFields.Serves))
                .Where(parent => model.ById.ContainsKey(Identifiers.BaseId(parent)))
                .ToList();

            if (parents.Count == 0)
            {
                continue;
            }

            var open = parents
                .Order(StringComparer.Ordinal)
                .FirstOrDefault(parent => !ParentApproved(model, parent));

            if (open is not null)
            {
                findings.Add(new Finding("transcription-status-invalid", $"Record '{record.Id}' is reviewed from transcription while '{open}' is not approved.", record.File, record.Line, SubjectOf(record)));
            }
        }

        var endsWith = model.Config.TranscriptionEndsWith;

        if (endsWith is { Length: > 0 }
            && model.ById.TryGetValue(endsWith, out var decision)
            && decision.Type == RecordTypes.Decision
            && model.EffectiveStatus.TryGetValue(endsWith, out var effective)
            && effective == RecordStatuses.Approved)
        {
            foreach (var record in model.Records
                .Where(record => record.Field(RecordFields.Provenance) == "transcribed" && record.Status != RecordStatuses.Retired)
                .OrderBy(record => record.Id, StringComparer.Ordinal))
            {
                findings.Add(new Finding("transcription-expired", $"Record '{record.Id}' stays transcribed after '{endsWith}' ends adoption.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Whether the fulfills or serves parent stands approved or reviewed.</summary>
    /// <param name="model">The loaded project model that holds the effective status of each record.</param>
    /// <param name="parent">The parent identifier, with any step fragment kept.</param>
    /// <returns>True when the parent stands approved or reviewed by effective or written status; otherwise false.</returns>
    private static bool ParentApproved(ProjectModel model, string parent)
    {
        var baseId = Identifiers.BaseId(parent);

        if (!model.ById.ContainsKey(baseId))
        {
            return false;
        }

        return model.EffectiveStatus.TryGetValue(baseId, out var effective)
            ? effective is RecordStatuses.Approved or RecordStatuses.Reviewed
            : model.ById[baseId].Status is RecordStatuses.Approved or RecordStatuses.Reviewed;
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
    /// <param name="model">The loaded project model that holds every use case record and page body.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunProse(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records
            .Where(record => record.Type == RecordTypes.UseCase && record.Status != RecordStatuses.Retired)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (!Significant(model, useCase))
            {
                continue;
            }

            var body = model.Bodies.FirstOrDefault(page => page.File == useCase.File)?.Body ?? "";

            if (!HasSection(body, "Goal"))
            {
                findings.Add(new Finding("prose-missing", $"Use case '{useCase.Id}' holds no Goal section.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            // Queries carry no What Happens section, so only commands need one.
            if (useCase.Field(RecordFields.Kind) != "query" && !HasSection(body, "What Happens"))
            {
                findings.Add(new Finding("prose-missing", $"Use case '{useCase.Id}' holds no What Happens section.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>
    ///     Whether the use case needs Goal and What Happens prose: it changes more
    ///     than one aggregate, refuses beyond the generic refusals, carries risk,
    ///     or runs inline effects.
    /// </summary>
    /// <param name="model">The loaded project model that holds every result record.</param>
    /// <param name="useCase">The use case record whose weight is judged.</param>
    /// <returns>True when the use case changes several aggregates, carries risk, runs inline effects, or refuses in writing; otherwise false.</returns>
    private static bool Significant(ProjectModel model, Record useCase)
    {
        if (useCase.Ids(RecordFields.Changes).Count > 1 || HasRisk(useCase))
        {
            return true;
        }

        if (EffectEntries(useCase).Any(entry =>
            Str(entry, RecordFields.Timing) is { } timing
            && timing.Equals("inline", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            return false;
        }

        return model.Records.Any(record => record.Type == RecordTypes.Result
            && !record.Derived
            && record.Field(RecordFields.Outcome) == "refusal"
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home == name);
    }

    /// <summary>Decides whether a page body carries a section with the given heading outside fenced code.</summary>
    /// <param name="body">The page body text.</param>
    /// <param name="heading">The section heading text wanted.</param>
    /// <returns>True when a heading line names the section; otherwise false.</returns>
    private static bool HasSection(string body, string heading)
    {
        var inFence = false;

        foreach (var line in body.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence && SectionNamed(line, heading))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Decides whether one line is a heading with the given text.</summary>
    /// <param name="line">The body line under test.</param>
    /// <param name="heading">The heading text wanted, compared without case.</param>
    /// <returns>True when the line is a heading that names the text; otherwise false.</returns>
    private static bool SectionNamed(string line, string heading)
    {
        var trimmed = line.Trim();

        if (!trimmed.StartsWith('#'))
        {
            return false;
        }

        var hashes = 0;

        while (hashes < trimmed.Length && trimmed[hashes] == '#')
        {
            hashes++;
        }

        if (hashes is 0 or > 6)
        {
            return false;
        }

        var text = trimmed[hashes..].Trim().TrimEnd('#').Trim();

        return text.Equals(heading, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Runs the many-aggregates check.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunManyAggregates(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record =>
            record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command" && record.Status != RecordStatuses.Retired))
        {
            if (ConsistencyMode(useCase) == "atomic" && CitesCount(useCase) > 1)
            {
                findings.Add(new Finding("atomic-group-duplicate", $"Use case '{useCase.Id}' saves more than one atomic write group.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports question-expired.</summary>
    /// <param name="model">The loaded project model that holds every record and its questions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunQuestionAged(ProjectModel model, List<Finding> findings)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var record in model.Records.Where(record => record.Status != RecordStatuses.Retired))
        {
            if (record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty(RecordFields.Questions, out var questions)
                || questions.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in questions.EnumerateArray())
            {
                // An answered question is closed. A tier-3 question with a
                // recommendation auto-approves past the veto window, so it never
                // ages; every other unanswered question past its decide-by date
                // reports, tier 1 included.
                if (Answered(entry)
                    || (TierOf(record) == 3 && HasRecommendation(entry))
                    || !AgedPast(entry, today))
                {
                    continue;
                }

                findings.Add(new Finding("question-expired", $"Record '{record.Id}' holds a question past its decide-by date.", record.File, record.Line, SubjectOf(record)));
                break;
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

    /// <summary>Reports a saga step, compensation, exit or driver that names no written record.</summary>
    /// <param name="model">The loaded project model that holds every saga aggregate and its targets.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunSaga(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate && record.Field(RecordFields.Kind) == "saga"))
        {
            foreach (var step in SagaSteps(aggregate))
            {
                var id = Str(step, RecordFields.Id) ?? "(no id)";

                if (Str(step, RecordFields.Runs) is { Length: > 0 } runs)
                {
                    if (!model.ById.TryGetValue(runs, out var target))
                    {
                        findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' runs '{runs}' from step '{id}', which no record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                    else if (target.Type != RecordTypes.UseCase)
                    {
                        findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' runs '{runs}' from step '{id}', which is a {target.Type}.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                    }
                }
                else
                {
                    findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds step '{id}' with no runs use case.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (step.TryGetProperty(RecordFields.Compensation, out var compensation))
                {
                    CheckSagaCompensation(model, aggregate, id, compensation, findings);
                }
                else
                {
                    findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds step '{id}' with no compensation.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            foreach (var exit in SagaExits(aggregate))
            {
                if (Str(exit, RecordFields.Name) is not { Length: > 0 })
                {
                    findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds an exit with no name.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            CheckSagaDriver(model, aggregate, findings);
        }
    }

    /// <summary>The saga steps: objects in its steps array.</summary>
    /// <param name="aggregate">The saga aggregate record whose steps array is read.</param>
    /// <returns>The step objects in written order, or an empty list when the saga holds no steps array.</returns>
    private static List<JsonElement> SagaSteps(Record aggregate)
    {
        if (aggregate.Raw.ValueKind == JsonValueKind.Object
            && aggregate.Raw.TryGetProperty(RecordFields.Steps, out var steps)
            && steps.ValueKind == JsonValueKind.Array)
        {
            return steps.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    /// <summary>The saga exits: objects in its exits array.</summary>
    /// <param name="aggregate">The saga aggregate record whose exits array is read.</param>
    /// <returns>The exit objects in written order, or an empty list when the saga holds no exits array.</returns>
    private static List<JsonElement> SagaExits(Record aggregate)
    {
        if (aggregate.Raw.ValueKind == JsonValueKind.Object
            && aggregate.Raw.TryGetProperty(RecordFields.Exits, out var exits)
            && exits.ValueKind == JsonValueKind.Array)
        {
            return exits.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    /// <summary>Reports a step compensation outside its none or use-case shape.</summary>
    /// <param name="model">The loaded project model that resolves the compensating use case.</param>
    /// <param name="aggregate">The saga aggregate record the finding is placed on.</param>
    /// <param name="id">The step identifier quoted in each finding.</param>
    /// <param name="compensation">The step compensation value under test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSagaCompensation(ProjectModel model, Record aggregate, string id, JsonElement compensation, List<Finding> findings)
    {
        if (compensation.ValueKind != JsonValueKind.Object
            || Str(compensation, RecordFields.Mode) is not { } mode)
        {
            findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds step '{id}' with a compensation naming no mode.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            return;
        }

        if (mode == "none")
        {
            if (Str(compensation, RecordFields.Reason) is not { Length: > 0 })
            {
                findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds step '{id}' with a none compensation naming no reason.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }

            return;
        }

        if (mode == "use-case")
        {
            if (Str(compensation, RecordFields.UseCase) is not { Length: > 0 } useCase)
            {
                findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds step '{id}' with a use-case compensation naming no use case.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }
            else if (!model.ById.TryGetValue(useCase, out var target))
            {
                findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' compensates step '{id}' with '{useCase}', which no record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }
            else if (target.Type != RecordTypes.UseCase)
            {
                findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' compensates step '{id}' with '{useCase}', which is a {target.Type}.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }

            return;
        }

        findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds step '{id}' with compensation mode '{mode}' outside none, use-case.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
    }

    /// <summary>Reports a saga driver naming no job, reaction or person runbook.</summary>
    /// <param name="model">The loaded project model that resolves the driver and runbook targets.</param>
    /// <param name="aggregate">The saga aggregate record whose drivenBy field is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSagaDriver(ProjectModel model, Record aggregate, List<Finding> findings)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.DrivenBy, out var driven)
            || driven.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var kinds = new List<string>();

        foreach (var kind in new[] { RecordFields.Job, RecordFields.Reaction, RecordFields.Person })
        {
            if (driven.TryGetProperty(kind, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 })
            {
                kinds.Add(kind);
            }
        }

        if (kinds.Count != 1)
        {
            findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' holds a drivenBy naming no single job, reaction or person.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            return;
        }

        if (kinds[0] is "job" or "reaction")
        {
            var id = driven.GetProperty(kinds[0]).GetString()!;

            if (!model.ById.TryGetValue(id, out var target))
            {
                findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' is driven by '{id}', which no record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }
            else if (target.Type != kinds[0])
            {
                findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' is driven by '{id}', which is a {target.Type}.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }

            return;
        }

        if (Str(driven, RecordFields.Runbook) is not { Length: > 0 } runbook)
        {
            findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' is driven by a person with no runbook.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
        }
        else if (!model.ById.TryGetValue(runbook, out var target))
        {
            findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' is driven through '{runbook}', which no record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
        }
        else if (target.Type != RecordTypes.Runbook)
        {
            findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' is driven through '{runbook}', which is a {target.Type}.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
        }

        if (Str(driven, RecordFields.DueWithin) is not { Length: > 0 } within)
        {
            findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' is driven by a person with no dueWithin.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
        }
        else if (!IsoDuration.IsMatch(within))
        {
            findings.Add(new Finding("field-invalid", $"Aggregate '{aggregate.Id}' is driven by a person with dueWithin '{within}', which is not an ISO 8601 duration.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
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

    /// <summary>Reports table-drift.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record and page body.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunTableDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records
            .Where(record => record.Type == RecordTypes.Aggregate)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var body = model.Bodies.FirstOrDefault(page => page.File == aggregate.File)?.Body;

            if (body is null)
            {
                continue;
            }

            var tables = BodyTables(body);

            CheckTransitionsDrift(aggregate, tables, findings);
            CheckDataDrift(aggregate, tables, findings);
        }

        CheckRetentionDrift(model, findings);
    }

    /// <summary>Reports an aggregate whose written transitions table differs from its frontmatter transitions.</summary>
    /// <param name="aggregate">The aggregate record whose transitions are compared.</param>
    /// <param name="tables">The Markdown tables read from the aggregate page body.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTransitionsDrift(Record aggregate, IReadOnlyList<BodyTable> tables, List<Finding> findings)
    {
        var written = TransitionsOf(aggregate)
            .Select(transition => transition.From + "\0" + transition.To + "\0" + (transition.By ?? ""))
            .Order(StringComparer.Ordinal)
            .ToList();

        var rows = new List<string>();

        foreach (var table in tables)
        {
            var from = BodyColumn(table.Headers, "from");
            var to = BodyColumn(table.Headers, "to");

            if (from < 0 || to < 0)
            {
                continue;
            }

            var by = BodyColumn(table.Headers, "triggeredby");

            if (by < 0)
            {
                by = BodyColumn(table.Headers, "by");
            }

            foreach (var cells in table.Rows)
            {
                var start = CleanBodyCell(BodyCell(cells, from), false);
                var end = CleanBodyCell(BodyCell(cells, to), false);

                if (BodyCellEmpty(end))
                {
                    continue;
                }

                // An empty From is a creation: the row names the state an instance starts in.
                if (BodyCellEmpty(start))
                {
                    start = RecordReader.Creation;
                }

                var trigger = "";

                if (by >= 0)
                {
                    var cause = CleanBodyCell(BodyCell(cells, by), true);
                    trigger = BodyCellEmpty(cause) ? "" : cause;
                }

                rows.Add(start + "\0" + end + "\0" + trigger);
            }
        }

        // A table with no front-matter field lifts into the record, so only a
        // front-matter field beside a disagreeing table reports.
        if (rows.Count > 0 && written.Count > 0 && !rows.Order(StringComparer.Ordinal).SequenceEqual(written))
        {
            findings.Add(new Finding("table-drift", $"Aggregate '{aggregate.Id}' holds a transitions table that differs from its frontmatter.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
        }
    }

    /// <summary>Reports an aggregate whose written data table differs from its frontmatter data rows.</summary>
    /// <param name="aggregate">The aggregate record whose data rows are compared.</param>
    /// <param name="tables">The Markdown tables read from the aggregate page body.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDataDrift(Record aggregate, IReadOnlyList<BodyTable> tables, List<Finding> findings)
    {
        var written = DataRowsOf(aggregate).Order(StringComparer.Ordinal).ToList();
        var rows = new List<string>();

        foreach (var table in tables)
        {
            var name = BodyColumn(table.Headers, "name");
            var meaning = BodyColumn(table.Headers, "meaning");
            var classification = BodyColumn(table.Headers, "classification");

            if (name < 0 || meaning < 0 || classification < 0)
            {
                continue;
            }

            foreach (var cells in table.Rows)
            {
                var item = CleanBodyCell(BodyCell(cells, name), false);

                if (BodyCellEmpty(item))
                {
                    continue;
                }

                var sense = CleanBodyCell(BodyCell(cells, meaning), false);
                var level = CleanBodyCell(BodyCell(cells, classification), false);

                rows.Add(item + "\0" + (BodyCellEmpty(sense) ? "" : sense) + "\0" + (BodyCellEmpty(level) ? "" : level.ToLowerInvariant()));
            }
        }

        if (rows.Count > 0 && written.Count > 0 && !rows.Order(StringComparer.Ordinal).SequenceEqual(written))
        {
            findings.Add(new Finding("table-drift", $"Aggregate '{aggregate.Id}' holds a data table that differs from its frontmatter.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
        }
    }

    /// <summary>The aggregate's data rows with meaning and classification; partial entries are left to the shape check.</summary>
    /// <param name="aggregate">The aggregate record whose data array is read.</param>
    /// <returns>Each complete data row key with name, meaning and classification.</returns>
    private static List<string> DataRowsOf(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.Data, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<string>();

        foreach (var entry in value.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(RecordFields.Name, out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() is { Length: > 0 } field
                && entry.TryGetProperty(RecordFields.Meaning, out var meaning)
                && meaning.ValueKind == JsonValueKind.String
                && entry.TryGetProperty(RecordFields.Classification, out var classification)
                && classification.ValueKind == JsonValueKind.String)
            {
                found.Add(field + "\0" + (meaning.GetString() ?? "") + "\0" + (classification.GetString() ?? "").ToLowerInvariant());
            }
        }

        return found;
    }

    /// <summary>The retention table columns in written order, projected from each schedule row.</summary>
    private static readonly string[] RetentionColumns = ["dataclass", "covers", "basis", "period", "enforcedby"];

    /// <summary>Reports each retention schedule whose written table differs from its frontmatter rows.</summary>
    /// <param name="model">The loaded project model that holds each page body.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRetentionDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var page in model.Bodies.OrderBy(body => body.File, StringComparer.Ordinal))
        {
            BodyTable? table = null;

            foreach (var candidate in BodyTables(page.Body))
            {
                if (BodyColumn(candidate.Headers, "dataclass") >= 0
                    && BodyColumn(candidate.Headers, "covers") >= 0
                    && BodyColumn(candidate.Headers, "period") >= 0)
                {
                    table = candidate;
                    break;
                }
            }

            if (table is null)
            {
                continue;
            }

            var columns = RetentionColumns
                .Where(column => BodyColumn(table.Headers, column) >= 0)
                .ToList();
            var written = RetentionRowsOf(page.File, columns);

            if (written is null || written.Count == 0)
            {
                continue;
            }

            var rows = table.Rows
                .Select(cells => RetentionKey(
                    columns.Select(column => RetentionCell(table, cells, column)).ToList()))
                .Where(key => key.Length > 0)
                .Order(StringComparer.Ordinal)
                .ToList();

            if (rows.Count > 0 && !rows.SequenceEqual(written))
            {
                findings.Add(new Finding("table-drift", $"Retention schedule '{page.File}' holds a retention table that differs from its rows.", page.File, page.BodyStartLine + table.Line, null));
            }
        }
    }

    /// <summary>The retention-schedule rows of the file, projected onto the table columns; null when the file holds no schedule.</summary>
    /// <param name="file">The retention schedule file whose frontmatter rows are read.</param>
    /// <param name="columns">The table columns each row is projected onto.</param>
    /// <returns>The row keys in ordinal order, or null when the file holds no retention schedule.</returns>
    private static List<string>? RetentionRowsOf(string file, IReadOnlyList<string> columns)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        string text;

        try
        {
            text = SourceTree.Read(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (!Frontmatter.TryParse(text, out var parsed, out _)
            || parsed.Root.ValueKind != JsonValueKind.Object
            || !parsed.Root.TryGetProperty(RecordFields.Type, out var type)
            || type.ValueKind != JsonValueKind.String
            || type.GetString() != "retention-schedule"
            || !parsed.Root.TryGetProperty(RecordFields.Rows, out var rows)
            || rows.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var found = new List<string>();

        foreach (var entry in rows.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var key = RetentionKey(columns.Select(column => RetentionField(entry, column)).ToList());

            if (key.Length > 0)
            {
                found.Add(key);
            }
        }

        return found.Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>One retention row field as the table states it: covers as sorted ids, the rest as written.</summary>
    /// <param name="entry">The retention schedule row object.</param>
    /// <param name="column">The table column whose field value is wanted.</param>
    /// <returns>The field text as the table states it, or an empty string when the row holds none.</returns>
    private static string RetentionField(JsonElement entry, string column)
    {
        if (column == "covers"
            && entry.TryGetProperty(RecordFields.Covers, out var covers)
            && covers.ValueKind == JsonValueKind.Array)
        {
            return string.Join(",", covers.EnumerateArray()
                .Where(id => id.ValueKind == JsonValueKind.String && id.GetString() is { Length: > 0 })
                .Select(id => id.GetString()!)
                .Order(StringComparer.Ordinal));
        }

        var field = column == "dataclass" ? "dataClass" : column == "enforcedby" ? "enforcedBy" : column;

        return entry.TryGetProperty(field, out var value)
            && value.ValueKind == JsonValueKind.String
            && value.GetString() is { } text
            ? text.Trim()
            : "";
    }

    /// <summary>One retention table cell as the frontmatter states it: covers split and sorted, the rest trimmed.</summary>
    /// <param name="table">The retention table whose headers locate the column.</param>
    /// <param name="cells">The table row cells in column order.</param>
    /// <param name="column">The table column whose cell value is wanted.</param>
    /// <returns>The cell text as the frontmatter states it.</returns>
    private static string RetentionCell(BodyTable table, IReadOnlyList<string> cells, string column)
    {
        var text = CleanBodyCell(BodyCell(cells, BodyColumn(table.Headers, column)), false);

        if (column != "covers")
        {
            return text;
        }

        return string.Join(",", text.Split(',')
            .Select(id => id.Trim())
            .Where(id => id.Length > 0)
            .Order(StringComparer.Ordinal));
    }

    /// <summary>One retention row key; empty when the row names no data class.</summary>
    /// <param name="fields">The projected row fields in column order.</param>
    /// <returns>The joined row key, or an empty string when the row names no data class.</returns>
    private static string RetentionKey(List<string> fields) =>
        fields.Count > 0 && fields[0].Length > 0 ? string.Join("\0", fields) : "";

    /// <summary>Every Markdown table outside fenced code.</summary>
    /// <param name="body">The page body text.</param>
    /// <returns>Each Markdown table with its headers, rows and start line.</returns>
    private static List<BodyTable> BodyTables(string body)
    {
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var tables = new List<BodyTable>();
        var inFence = false;
        var index = 0;

        while (index < lines.Length)
        {
            var line = lines[index];
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal)
                || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                index++;
                continue;
            }

            if (inFence)
            {
                index++;
                continue;
            }

            if (index + 1 < lines.Length
                && line.Contains('|')
                && trimmed.Length > 0
                && TableDelimiter.IsMatch(lines[index + 1]))
            {
                var headers = BodyCells(line);
                var rows = new List<IReadOnlyList<string>>();
                var row = index + 2;

                while (row < lines.Length
                       && lines[row].Contains('|')
                       && lines[row].Trim().Length > 0
                       && BodyHeading(lines[row].Trim()) is null
                       && !lines[row].Trim().StartsWith("```", StringComparison.Ordinal)
                       && !lines[row].Trim().StartsWith("~~~", StringComparison.Ordinal))
                {
                    rows.Add(BodyCells(lines[row]));
                    row++;
                }

                if (headers.Count > 0)
                {
                    tables.Add(new BodyTable(headers, rows, index));
                }

                index = row;
                continue;
            }

            index++;
        }

        return tables;
    }

    /// <summary>Matches the delimiter row that separates a Markdown table header from its rows.</summary>
    private static readonly Regex TableDelimiter = new(@"^\s*\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$", RegexOptions.Compiled);

    /// <summary>Reads the heading text of one trimmed body line.</summary>
    /// <param name="trimmed">The body line with surrounding whitespace removed.</param>
    /// <returns>The heading text without its marks, or null when the line is no heading.</returns>
    private static string? BodyHeading(string trimmed)
    {
        if (!trimmed.StartsWith('#'))
        {
            return null;
        }

        var hashes = 0;

        while (hashes < trimmed.Length && trimmed[hashes] == '#')
        {
            hashes++;
        }

        if (hashes > 6 || (hashes < trimmed.Length && trimmed[hashes] != ' ' && trimmed[hashes] != '\t'))
        {
            return null;
        }

        return trimmed[hashes..].Trim().TrimEnd('#').Trim();
    }

    /// <summary>Splits one Markdown table line into its trimmed cells.</summary>
    /// <param name="line">The table header or row line.</param>
    /// <returns>The trimmed cell texts, or an empty list for a line holding one empty cell.</returns>
    private static List<string> BodyCells(string line)
    {
        var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

        return cells is [{ Length: 0 }] ? [] : cells;
    }

    /// <summary>Locates one column in a table header row.</summary>
    /// <param name="headers">The table header cell texts.</param>
    /// <param name="target">The normalized column name wanted.</param>
    /// <returns>The zero-based column index, or minus one when the table holds no such column.</returns>
    private static int BodyColumn(IReadOnlyList<string> headers, string target)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (BodyNorm(headers[index]) == target)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Headers compare case-insensitively, ignoring spaces, dashes and underscores.</summary>
    /// <param name="value">The header text as written.</param>
    /// <returns>The header with spaces, dashes, underscores and tabs removed, in lowercase.</returns>
    private static string BodyNorm(string value) =>
        value.Replace(" ", "").Replace("-", "").Replace("_", "").Replace("\t", "").ToLowerInvariant();

    /// <summary>Reads one cell of a table row, tolerating short rows.</summary>
    /// <param name="cells">The table row cells in column order.</param>
    /// <param name="index">The zero-based column index wanted.</param>
    /// <returns>The cell text, or an empty string when the row holds no such cell.</returns>
    private static string BodyCell(IReadOnlyList<string> cells, int index) =>
        index < cells.Count ? cells[index] : "";

    /// <summary>Trimmed text: code ticks stripped, a lone link reduced to its label, or its target for an id.</summary>
    /// <param name="cell">The table cell text as written.</param>
    /// <param name="preferTarget">Whether a link cell states its target instead of its label.</param>
    /// <returns>The trimmed cell text with code ticks stripped and any link reduced as stated.</returns>
    private static string CleanBodyCell(string cell, bool preferTarget)
    {
        var text = cell.Trim();

        while (text.Length >= 2 && text.StartsWith('`') && text.EndsWith('`'))
        {
            text = text[1..^1].Trim();
        }

        if (BodyLinkOf(text) is { } link)
        {
            if (preferTarget && link.Target.Contains('/'))
            {
                return link.Target.Trim();
            }

            return link.Label.Trim();
        }

        return text;
    }

    /// <summary>Splits one Markdown link that spans the whole cell into label and target.</summary>
    /// <param name="text">The cell text under test.</param>
    /// <returns>The link label and target, or null when the text is no single whole-cell link.</returns>
    private static (string Label, string Target)? BodyLinkOf(string text)
    {
        if (!text.StartsWith('['))
        {
            return null;
        }

        var labelEnd = text.IndexOf("](", StringComparison.Ordinal);

        if (labelEnd < 0)
        {
            return null;
        }

        var targetEnd = text.IndexOf(')', labelEnd + 2);

        if (targetEnd != text.Length - 1)
        {
            return null;
        }

        var target = text[(labelEnd + 2)..targetEnd];
        var space = target.IndexOfAny([' ', '\t', '"', '\'']);

        return (text[1..labelEnd], space < 0 ? target : target[..space]);
    }

    /// <summary>Decides whether a table cell states no value.</summary>
    /// <param name="text">The cleaned cell text under test.</param>
    /// <returns>True when the cell is empty or states a dash or a none form; otherwise false.</returns>
    private static bool BodyCellEmpty(string text) =>
        text.Length == 0
        || text is "-" or "--"
        || text.Equals("n/a", StringComparison.OrdinalIgnoreCase)
        || text.Equals("none", StringComparison.OrdinalIgnoreCase)
        || text.Equals("none.", StringComparison.OrdinalIgnoreCase);
}
