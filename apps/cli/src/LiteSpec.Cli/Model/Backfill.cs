using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Model;

/// <summary>
///     Backfills Behavior drafts from code during adoption: for each use case,
///     reaction and job a handler folder or an anchor tag places, it reads the
///     repositories, dispatches and refusal codes the code model already holds and
///     drafts the missing frontmatter. A use case gains changes or reads, effects
///     entries and refusal results; a reaction or job gains reads and effects entries.
///     Every touched record is marked transcribed. Existing entries are never removed
///     or rewritten; only missing entries are appended.
/// </summary>
/// <follows>litespec/core/agent#mark-transcribed-records-during-adoption</follows>
static class Backfill
{
    static readonly JsonSerializerOptions Write = new()
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>One record's planned writes.</summary>
    sealed record RecordPlan(
        Record Record,
        IReadOnlyList<string> Changes,
        IReadOnlyList<string> Reads,
        IReadOnlyList<(string Verb, string Target)> Effects,
        IReadOnlyList<(string Code, string Id, string Title)> Results,
        bool MarkTranscribed)
    {
        /// <summary>Whether the plan writes anything at all.</summary>
        public bool HasWrites =>
            Changes.Count + Reads.Count + Effects.Count + Results.Count > 0;
    }

    /// <summary>One reaction or job the code places: the repositories it takes and the dispatches it makes.</summary>
    sealed record PlacedWork(
        IReadOnlyList<CodeRepository>? Repositories,
        IReadOnlyList<CodeDispatch>? Dispatches);

    /// <summary>The code facts and record indexes one backfill run resolves against.</summary>
    sealed class Facts
    {
        public Dictionary<string, List<HandlerInfo>> HandlersByUseCase { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, List<PlacedWork>> PlacedByRecord { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, HashSet<string>> Declaring { get; } = new(StringComparer.Ordinal);
        public List<Record> Aggregates { get; } = [];
        public List<Record> UseCases { get; } = [];
        public List<Record> Notifications { get; } = [];
        public List<Record> IntegrationEvents { get; } = [];
        public List<Record> ExternalSystems { get; } = [];
    }

    /// <summary>
    ///     Runs backfill over the module, or every module when none is named. Prints a
    ///     per-module completeness table, then each planned write. Writes nothing on a
    ///     dry run. Returns the lines and whether the named module holds no records.
    /// </summary>
    public static (IReadOnlyList<string> Lines, bool UnknownModule) Run(
        ProjectModel model, string? module, bool dryRun)
    {
        var targets = model.Records
            .Where(record => record.Type is "use-case" or "reaction" or "job"
                && !record.Derived
                && record.Status != "retired"
                && (module is null || HomeOf(record.Id) == module))
            .OrderBy(record => record.File, StringComparer.Ordinal)
            .ThenBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        if (module is not null && targets.Count == 0)
        {
            return ([], true);
        }

        var facts = BuildFacts(model);
        var plans = new List<RecordPlan>();

        foreach (var record in targets)
        {
            if (PlanFor(model, facts, record) is { HasWrites: true } plan)
            {
                plans.Add(plan);
            }
        }

        var lines = new List<string>();

        lines.AddRange(TableLines(model, targets));

        foreach (var plan in plans)
        {
            lines.Add(Describe(model, plan, dryRun));
        }

        // Screens are scoped per frontend and entry point, not per module: one
        // transcribed draft per route of the frontend's entry points that
        // carries no screen mark, whatever module was named.
        var screenPlans = ScreenPlans(model);

        foreach (var plan in screenPlans)
        {
            lines.Add(DescribeScreen(model, plan, dryRun));
        }

        var files = plans.Select(plan => plan.Record.File).Distinct(StringComparer.OrdinalIgnoreCase)
            .Concat(screenPlans.Select(plan => plan.File))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var records = plans.Count + screenPlans.Count;

        lines.Add(dryRun
            ? $"(dry run) {Files(files)} would be updated, {Records(records)} backfilled."
            : $"{Files(files)} updated, {Records(records)} backfilled.");

        if (!dryRun)
        {
            Apply(plans);
            ApplyScreens(screenPlans);
        }

        return (lines, false);
    }

    /// <summary>One drafted screen: its file, id, entry point, address, offers and serves.</summary>
    sealed record ScreenPlan(
        string File,
        string Id,
        string EntryPoint,
        string Address,
        IReadOnlyList<string> Offers,
        IReadOnlyList<string> Serves,
        string Owner);

    /// <summary>One transcribed screen draft per unmarked route, per frontend and entry point.</summary>
    static List<ScreenPlan> ScreenPlans(ProjectModel model)
    {
        var plans = new List<ScreenPlan>();
        var taken = new HashSet<string>(model.Records.Select(record => record.Id), StringComparer.Ordinal);

        foreach (var frontend in model.Config.Frontends)
        {
            var entryPoints = FrontendRecords.EntryPointsOf(model, frontend);

            if (entryPoints.Count == 0)
            {
                continue;
            }

            var entryPoint = entryPoints[0];
            var entryName = Identifiers.TrySplit(entryPoint, out _, out _, out var parsed) ? parsed : entryPoint;
            var owner = (model.ById.TryGetValue(entryPoint, out var point) ? point.Owner : null)
                ?? model.Config.Owners.FirstOrDefault()
                ?? "unknown";

            foreach (var route in model.Screens.Where(screen =>
                screen.Frontend == frontend.Name && screen.ScreenId is null))
            {
                var name = ScreenName(route.Route);
                var id = $"screen/{entryName}.{name}";
                var attempt = 2;

                while (!taken.Add(id))
                {
                    id = $"screen/{entryName}.{name}-{attempt++}";
                }

                var serves = route.UseCases
                    .SelectMany(useCaseId => model.ById.TryGetValue(useCaseId, out var useCase)
                        ? useCase.Ids("serves")
                        : [])
                    .Where(step => Identifiers.TrySplitStep(step, out _, out _))
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToList();

                plans.Add(new ScreenPlan(
                    Path.Combine(model.Config.BehaviorDocs, "screens", $"{entryName}.{id[(id.LastIndexOf('.') + 1)..]}.md"),
                    id, entryPoint, route.Route, [.. route.UseCases], serves, owner));
            }
        }

        return plans;
    }

    /// <summary>
    ///     The screen name an address drafts: its plain segments joined, keeping the last five
    ///     words so a collision suffix stays within the six the name rules allow, or
    ///     <c>home</c> for the root.
    /// </summary>
    static string ScreenName(string address)
    {
        var segments = address.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => !segment.StartsWith("[", StringComparison.Ordinal)
                && !segment.StartsWith("{", StringComparison.Ordinal)
                && !segment.StartsWith("$", StringComparison.Ordinal)
                && !(segment.StartsWith("(", StringComparison.Ordinal) && segment.EndsWith(")", StringComparison.Ordinal)))
            .ToList();

        var words = segments
            .SelectMany(segment => segment.ToLowerInvariant().Split(['-', '_', '.'], StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        return words.Count == 0 ? "home" : string.Join("-", words.TakeLast(5));
    }

    /// <summary>One line per drafted screen: its path, id, offers and serves.</summary>
    static string DescribeScreen(ProjectModel model, ScreenPlan plan, bool dryRun)
    {
        var parts = new List<string>();

        if (plan.Offers.Count > 0)
        {
            parts.Add($"+offers {string.Join(",", plan.Offers)}");
        }

        if (plan.Serves.Count > 0)
        {
            parts.Add($"+serves {string.Join(",", plan.Serves)}");
        }

        var prefix = dryRun ? "(dry run) " : "";

        return $"{prefix}{model.Config.Relative(plan.File)}: {plan.Id} {string.Join(" ", parts)} (source transcribed)".TrimEnd();
    }

    /// <summary>Writes every drafted screen as a transcribed draft page.</summary>
    static void ApplyScreens(List<ScreenPlan> plans)
    {
        var decideBy = DateTime.UtcNow.Date.AddDays(30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        foreach (var plan in plans)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(plan.File)!);

            var frontmatter = new JsonObject
            {
                ["type"] = "screen",
                ["id"] = plan.Id,
                ["title"] = TitleOf(plan.Id[(plan.Id.LastIndexOf('.') + 1)..]),
                ["owner"] = plan.Owner,
                ["status"] = "draft",
                ["decider"] = plan.Owner,
                ["decideBy"] = decideBy,
                ["entryPoint"] = plan.EntryPoint,
                ["address"] = plan.Address,
                ["source"] = "transcribed",
            };

            if (plan.Offers.Count > 0)
            {
                frontmatter["offers"] = new JsonArray(plan.Offers.Select(id => JsonValue.Create(id)).ToArray());
            }

            if (plan.Serves.Count > 0)
            {
                frontmatter["serves"] = new JsonArray(plan.Serves.Select(id => JsonValue.Create(id)).ToArray());
            }

            var title = TitleOf(plan.Id[(plan.Id.LastIndexOf('.') + 1)..]);

            File.WriteAllText(
                plan.File,
                "---\n" + frontmatter.ToJsonString(Write) + "\n---\n\n"
                + $"# {title}\n\n## Summary\n\nState what a person can do on this page.\n");
        }
    }

    static string Files(int count) => count == 1 ? "1 file" : $"{count} files";
    static string Records(int count) => count == 1 ? "1 record" : $"{count} records";

    /// <summary>The module home of an identifier, or the empty string when it carries none.</summary>
    static string HomeOf(string id) =>
        Identifiers.TrySplit(id, out _, out var home, out _) && home is not null ? home : "";

    /// <summary>Collects the handler, reaction, job and record indexes one run resolves against.</summary>
    static Facts BuildFacts(ProjectModel model)
    {
        var facts = new Facts();

        foreach (var handler in model.Code.Handlers)
        {
            if (!facts.HandlersByUseCase.TryGetValue(handler.UseCaseId, out var placed))
            {
                placed = [];
                facts.HandlersByUseCase[handler.UseCaseId] = placed;
            }

            placed.Add(handler);
        }

        foreach (var reaction in model.Code.Reactions)
        {
            if (reaction.RecordId is not null)
            {
                AddPlaced(facts, reaction.RecordId, reaction.Repositories, reaction.Dispatches);
            }
        }

        foreach (var job in model.Code.Jobs)
        {
            if (job.RecordId is not null)
            {
                AddPlaced(facts, job.RecordId, job.Repositories, job.Dispatches);
            }
        }

        foreach (var literal in model.Code.Literals)
        {
            if (!literal.Declares || literal.Class is null)
            {
                continue;
            }

            if (!facts.Declaring.TryGetValue(literal.Class, out var codes))
            {
                codes = new HashSet<string>(StringComparer.Ordinal);
                facts.Declaring[literal.Class] = codes;
            }

            codes.Add(literal.Value);
        }

        foreach (var record in model.Records)
        {
            switch (record.Type)
            {
                case "aggregate": facts.Aggregates.Add(record); break;
                case "use-case": facts.UseCases.Add(record); break;
                case "notification": facts.Notifications.Add(record); break;
                case "integration-event": facts.IntegrationEvents.Add(record); break;
                case "external-system": facts.ExternalSystems.Add(record); break;
            }
        }

        return facts;
    }

    /// <summary>Indexes one reaction or job under the record its mark names.</summary>
    static void AddPlaced(
        Facts facts,
        string recordId,
        IReadOnlyList<CodeRepository>? repositories,
        IReadOnlyList<CodeDispatch>? dispatches)
    {
        if (!facts.PlacedByRecord.TryGetValue(recordId, out var placed))
        {
            placed = [];
            facts.PlacedByRecord[recordId] = placed;
        }

        placed.Add(new PlacedWork(repositories, dispatches));
    }

    /// <summary>Plans the missing entries for one record, or an empty plan when code places none there.</summary>
    static RecordPlan PlanFor(ProjectModel model, Facts facts, Record record)
    {
        if (record.Type == "use-case")
        {
            return PlanUseCase(model, facts, record);
        }

        if (!facts.PlacedByRecord.TryGetValue(record.Id, out var placed))
        {
            return Empty(record);
        }

        return PlanReactionJob(facts, record, placed);
    }

    static RecordPlan Empty(Record record) =>
        new(record, [], [], [], [], false);

    static RecordPlan PlanUseCase(ProjectModel model, Facts facts, Record record)
    {
        if (!facts.HandlersByUseCase.TryGetValue(record.Id, out var handlers))
        {
            return Empty(record);
        }

        var module = HomeOf(record.Id);
        var declared = new HashSet<string>(record.Ids("changes"), StringComparer.Ordinal);

        declared.UnionWith(record.Ids("reads"));

        var missing = handlers
            .SelectMany(handler => handler.Repositories ?? [])
            .Select(repository => ResolveAggregate(facts, module, repository.Interface))
            .Where(id => id is not null && !declared.Contains(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        // A query reads without changing, so its repositories are reads; a command's are changes.
        List<string> changes = record.Field("kind") == "query" ? [] : missing;
        List<string> reads = record.Field("kind") == "query" ? missing : [];

        var edges = DeclaredEffects(record);
        var planned = new HashSet<(string Verb, string Target)>();
        var effects = new List<(string Verb, string Target)>();

        foreach (var dispatch in OrderedDispatches(handlers.SelectMany(handler => handler.Dispatches ?? [])))
        {
            var target = ResolveEffect(facts, module, dispatch);

            if (target is null
                || edges.Contains((dispatch.Verb, target))
                || !planned.Add((dispatch.Verb, target)))
            {
                continue;
            }

            effects.Add((dispatch.Verb, target));
        }

        var produced = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handler in handlers)
        {
            produced.UnionWith(ProducedCodes(handler.Refusals, handler.Thrown, facts.Declaring));
        }

        var known = DeclaredCodes(model, record);
        var taken = new HashSet<string>(model.Records.Select(entry => entry.Id), StringComparer.Ordinal);
        var results = new List<(string Code, string Id, string Title)>();

        if (Identifiers.TrySplit(record.Id, out _, out _, out var operation) && operation.Length > 0)
        {
            foreach (var code in produced.Where(code => !known.Contains(code)).Order(StringComparer.Ordinal))
            {
                var slug = code.Contains('.') ? code[(code.LastIndexOf('.') + 1)..] : code;
                var id = $"result/{operation}.{slug}";
                var attempt = 2;

                while (!taken.Add(id))
                {
                    id = $"result/{operation}.{slug}-{attempt++}";
                }

                results.Add((code, id, TitleOf(slug)));
            }
        }

        return Finish(record, changes, reads, effects, results);
    }

    static RecordPlan PlanReactionJob(Facts facts, Record record, List<PlacedWork> placed)
    {
        var reads = MissingReads(facts, record, placed);
        var module = HomeOf(record.Id);
        var edges = DeclaredEffects(record);
        var planned = new HashSet<(string Verb, string Target)>();
        var effects = new List<(string Verb, string Target)>();

        foreach (var dispatch in OrderedDispatches(placed.SelectMany(info => info.Dispatches ?? [])))
        {
            var target = ResolveEffect(facts, module, dispatch);

            if (target is null
                || edges.Contains((dispatch.Verb, target))
                || !planned.Add((dispatch.Verb, target)))
            {
                continue;
            }

            effects.Add((dispatch.Verb, target));
        }

        return Finish(record, [], reads, effects, []);
    }

    /// <summary>Aggregate ids the code takes that the reaction or job declares in neither changes nor reads.</summary>
    static List<string> MissingReads(Facts facts, Record record, List<PlacedWork> placed)
    {
        // A reaction or job never writes an aggregate directly: it writes through the
        // use cases it runs, so every repository it takes is a read.
        var module = HomeOf(record.Id);
        var declared = new HashSet<string>(record.Ids("reads"), StringComparer.Ordinal);

        return placed
            .SelectMany(info => info.Repositories ?? [])
            .Select(repository => ResolveAggregate(facts, module, repository.Interface))
            .Where(id => id is not null && !declared.Contains(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    static RecordPlan Finish(
        Record record,
        List<string> changes,
        List<string> reads,
        List<(string Verb, string Target)> effects,
        List<(string Code, string Id, string Title)> results)
    {
        var plan = new RecordPlan(record, changes, reads, effects, results, false);

        return !plan.HasWrites || record.Field("source") == "transcribed"
            ? plan
            : plan with { MarkTranscribed = true };
    }

    /// <summary>One line per planned record: its relative path, id and added entries.</summary>
    static string Describe(ProjectModel model, RecordPlan plan, bool dryRun)
    {
        var parts = new List<string>();

        if (plan.Changes.Count > 0)
        {
            parts.Add($"+changes {string.Join(",", plan.Changes)}");
        }

        if (plan.Reads.Count > 0)
        {
            parts.Add($"+reads {string.Join(",", plan.Reads)}");
        }

        if (plan.Effects.Count > 0)
        {
            parts.Add($"+effects {string.Join(",", plan.Effects.Select(edge => $"{edge.Verb}:{edge.Target}"))}");
        }

        if (plan.Results.Count > 0)
        {
            parts.Add($"+results {string.Join(",", plan.Results.Select(result => result.Code))}");
        }

        var source = plan.MarkTranscribed ? " (source transcribed)" : "";
        var prefix = dryRun ? "(dry run) " : "";

        return $"{prefix}{model.Config.Relative(plan.Record.File)}: {plan.Record.Id} {string.Join(" ", parts)}{source}";
    }

    /// <summary>The per-module completeness table: live records with each field's declared share.</summary>
    static List<string> TableLines(ProjectModel model, List<Record> targets)
    {
        var lines = new List<string> { "Module Records Changes Reads Effects Results" };
        var width = 6;

        var rows = targets
            .GroupBy(record => HomeOf(record.Id), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => RowFor(model, group.Key, group.ToList()))
            .ToList();

        foreach (var row in rows)
        {
            width = Math.Max(width, row.Module.Length);
        }

        foreach (var row in rows)
        {
            lines.Add($"{row.Module.PadRight(width)} {row.Records,-7} {row.Changes,-7} {row.Reads,-5} {row.Effects,-7} {row.Results}");
        }

        return lines;
    }

    sealed record ModuleRow(string Module, int Records, string Changes, string Reads, string Effects, string Results);

    static ModuleRow RowFor(ProjectModel model, string module, List<Record> records)
    {
        var useCases = records.Where(record => record.Type == "use-case").ToList();
        var changes = useCases.Count(record => record.Ids("changes").Count > 0);
        var reads = records.Count(record => record.Ids("reads").Count > 0);
        var effects = records.Count(HasEdges);
        var results = useCases.Count(record => HasResults(model, record));

        return new ModuleRow(
            module,
            records.Count,
            useCases.Count == 0 ? "n/a" : $"{changes}/{useCases.Count}",
            $"{reads}/{records.Count}",
            $"{effects}/{records.Count}",
            useCases.Count == 0 ? "n/a" : $"{results}/{useCases.Count}");
    }

    /// <summary>Whether the record declares its edges in its effects entries.</summary>
    static bool HasEdges(Record record) =>
        record.Type switch
        {
            "use-case" or "reaction" or "job" => DeclaredEffects(record).Count > 0,
            _ => false,
        };

    /// <summary>Whether the use case carries a written result for its own operation.</summary>
    static bool HasResults(ProjectModel model, Record useCase)
    {
        if (!Identifiers.TrySplit(useCase.Id, out _, out _, out var operation))
        {
            return false;
        }

        return model.Records.Any(record =>
            record.Type == "result"
            && !record.Derived
            && Identifiers.TrySplit(record.Id, out _, out var home, out _)
            && home == operation);
    }

    /// <summary>Dispatches in a stable order: file, line, verb, then target.</summary>
    static List<CodeDispatch> OrderedDispatches(IEnumerable<CodeDispatch> dispatches) =>
        dispatches
            .OrderBy(dispatch => dispatch.File, StringComparer.Ordinal)
            .ThenBy(dispatch => dispatch.Line)
            .ThenBy(dispatch => dispatch.Verb, StringComparer.Ordinal)
            .ThenBy(dispatch => dispatch.Target, StringComparer.Ordinal)
            .ToList();

    /// <summary>Every edge a use case, reaction or job declares in its effects entries, as verb and target pairs.</summary>
    static HashSet<(string Verb, string Target)> DeclaredEffects(Record useCase)
    {
        var edges = new HashSet<(string Verb, string Target)>();

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty("effects", out var effects)
            || effects.ValueKind != JsonValueKind.Array)
        {
            return edges;
        }

        foreach (var entry in effects.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var verb in new[] { "runs", "sends", "emits", "calls" })
            {
                if (entry.TryGetProperty(verb, out var target)
                    && target.ValueKind == JsonValueKind.String
                    && target.GetString() is { Length: > 0 } id)
                {
                    edges.Add((verb, id));
                }
            }
        }

        return edges;
    }

    /// <summary>Resolves one dispatch to the record id its edge declares, or null when none fits.</summary>
    static string? ResolveEffect(Facts facts, string module, CodeDispatch dispatch) =>
        dispatch.Verb switch
        {
            "runs" => ResolveUseCase(facts, module, dispatch.Target),
            "emits" => ResolveIntegrationEvent(facts, dispatch.Target),
            "sends" => ResolveNotification(facts, dispatch),
            "calls" => ResolveCall(facts, dispatch),
            _ => null,
        };

    /// <summary>The aggregate a repository interface serves, preferring the record's own module.</summary>
    static string? ResolveAggregate(Facts facts, string module, string face)
    {
        if (RepositoryStem(face) is not { } stem)
        {
            return null;
        }

        var candidates = facts.Aggregates
            .Where(record => Identifiers.TrySplit(record.Id, out _, out _, out var name) && name == stem)
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        var home = candidates.Where(record => HomeOf(record.Id) == module).ToList();

        return home.Count > 0 ? home[0].Id : candidates.Count == 1 ? candidates[0].Id : null;
    }

    /// <summary>The aggregate name a repository interface serves, in kebab-case, or null when it names none.</summary>
    static string? RepositoryStem(string face)
    {
        var type = face;
        var generic = type.IndexOf('<');

        // A generic repository serves its first type argument.
        if (generic >= 0)
        {
            var args = type[(generic + 1)..].TrimEnd('>');
            type = args.Split(',')[0].Trim();

            var dot = type.LastIndexOf('.');

            if (dot >= 0)
            {
                type = type[(dot + 1)..];
            }

            return type.Length > 0 ? Naming.Kebab(type) : null;
        }

        if (type.StartsWith("I", StringComparison.Ordinal) && type.Length > 1 && char.IsUpper(type[1]))
        {
            type = type[1..];
        }

        if (!type.EndsWith("Repository", StringComparison.Ordinal) || type.Length <= "Repository".Length)
        {
            return null;
        }

        return Naming.Kebab(type[..^"Repository".Length]);
    }

    /// <summary>The use case a dispatched message names, preferring the record's own module.</summary>
    static string? ResolveUseCase(Facts facts, string module, string message)
    {
        var stems = new HashSet<string>(StringComparer.Ordinal) { Naming.Kebab(message) };

        foreach (var suffix in new[] { "Command", "Query" })
        {
            if (message.EndsWith(suffix, StringComparison.Ordinal) && message.Length > suffix.Length)
            {
                stems.Add(Naming.Kebab(message[..^suffix.Length]));
            }
        }

        var candidates = facts.UseCases
            .Where(record => Identifiers.TrySplit(record.Id, out _, out _, out var operation) && stems.Contains(operation))
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        var home = candidates.Where(record => HomeOf(record.Id) == module).ToList();

        return home.Count > 0 ? home[0].Id : candidates.Count == 1 ? candidates[0].Id : null;
    }

    /// <summary>The integration event a published message names, or null when none fits alone.</summary>
    static string? ResolveIntegrationEvent(Facts facts, string message)
    {
        var candidates = facts.IntegrationEvents
            .Where(record => MessageMatches(message, record.Id))
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        return candidates.Count == 1 ? candidates[0].Id : null;
    }

    /// <summary>The notification a send names: by template key, else by message type.</summary>
    static string? ResolveNotification(Facts facts, CodeDispatch dispatch)
    {
        if (dispatch.Detail == "template")
        {
            return facts.Notifications
                .Where(record => record.Field("template") == dispatch.Target)
                .OrderBy(record => record.Id, StringComparer.Ordinal)
                .Select(record => record.Id)
                .FirstOrDefault();
        }

        var candidates = facts.Notifications
            .Where(record => MessageMatches(dispatch.Target, record.Id))
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        return candidates.Count == 1 ? candidates[0].Id : null;
    }

    /// <summary>The external-system operation a port call names, or null when none fits alone.</summary>
    static string? ResolveCall(Facts facts, CodeDispatch dispatch)
    {
        var name = dispatch.Target;

        if (name.EndsWith("Async", StringComparison.Ordinal) && name.Length > "Async".Length)
        {
            name = name[..^"Async".Length];
        }

        var operation = Naming.Kebab(name);

        var candidates = facts.ExternalSystems
            .Where(record => OperationNames(record).Contains(operation))
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .ToList();

        return candidates.Count == 1 ? $"{candidates[0].Id}#{operation}" : null;
    }

    /// <summary>The operation names an external-system record declares.</summary>
    static HashSet<string> OperationNames(Record system)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (system.Raw.ValueKind == JsonValueKind.Object
            && system.Raw.TryGetProperty("operations", out var operations)
            && operations.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in operations.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && entry.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && name.GetString() is { Length: > 0 } operation)
                {
                    names.Add(operation);
                }
            }
        }

        return names;
    }

    /// <summary>
    ///     Whether a dispatched message names a declared edge: the message stem meets the
    ///     identifier after its slash, or its last segment, with dots reading as dashes.
    /// </summary>
    static bool MessageMatches(string message, string id)
    {
        var stem = message;

        foreach (var suffix in new[] { "IntegrationEvent", "Notification", "Event", "Message" })
        {
            if (stem.EndsWith(suffix, StringComparison.Ordinal) && stem.Length > suffix.Length)
            {
                stem = stem[..^suffix.Length];
                break;
            }
        }

        var kebab = Naming.Kebab(stem);
        var rest = id.Contains('/') ? id[(id.IndexOf('/') + 1)..] : id;
        var normalized = rest.Replace('.', '-');
        var last = rest.Contains('.') ? rest[(rest.LastIndexOf('.') + 1)..] : rest;

        return kebab == normalized || kebab == last;
    }

    /// <summary>
    ///     Every refusal code a class produces: its declaring literals plus the codes of the
    ///     exception and failure types it constructs.
    /// </summary>
    static HashSet<string> ProducedCodes(
        IReadOnlyList<CodeRefusal>? refusals,
        IReadOnlyList<CodeThrown>? thrown,
        Dictionary<string, HashSet<string>> declaring)
    {
        var produced = new HashSet<string>(StringComparer.Ordinal);

        if (refusals is not null)
        {
            foreach (var refusal in refusals)
            {
                produced.Add(refusal.Code);
            }
        }

        if (thrown is not null)
        {
            foreach (var construction in thrown)
            {
                if (!declaring.TryGetValue(construction.Type, out var codes))
                {
                    continue;
                }

                foreach (var code in codes)
                {
                    produced.Add(code);
                }
            }
        }

        return produced;
    }

    /// <summary>
    ///     Every refusal code a use case declares: its written refusal results plus the derived
    ///     generic codes, which a permission grant and a validated input each derive.
    /// </summary>
    static HashSet<string> DeclaredCodes(ProjectModel model, Record useCase)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);

        if (Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            foreach (var result in model.Records)
            {
                if (result.Type != "result" || result.Derived || result.Field("outcome") != "refusal")
                {
                    continue;
                }

                if (result.Field("code") is { } code
                    && Identifiers.TrySplit(result.Id, out _, out var home, out _)
                    && home == name)
                {
                    declared.Add(code);
                }
            }
        }

        if (ModelBuilder.GrantedPermission(useCase) is not null)
        {
            declared.Add(model.Config.ForbiddenCode);
        }

        if (ModelBuilder.HasValidatedInput(useCase))
        {
            declared.Add(model.Config.InvalidInputCode);
        }

        return declared;
    }

    /// <summary>A refusal-code slug in title words: ghost-refusal reads Ghost Refusal.</summary>
    static string TitleOf(string slug) =>
        string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part =>
            part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));

    /// <summary>Writes every plan into its page's frontmatter, one write per file.</summary>
    static void Apply(List<RecordPlan> plans)
    {
        foreach (var group in plans.GroupBy(plan => plan.Record.File, StringComparer.OrdinalIgnoreCase))
        {
            var text = File.ReadAllText(group.Key);
            var json = group.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
            var (head, node, tail) = json ? ("", JsonNode.Parse(text), "") : SplitFrontmatter(text);

            if (node is null)
            {
                continue;
            }

            foreach (var plan in group)
            {
                if (Find(node, plan.Record.Id) is not JsonObject target)
                {
                    continue;
                }

                AppendStrings(target, "changes", plan.Changes);
                AppendStrings(target, "reads", plan.Reads);
                AppendEffects(target, plan.Effects);
                AppendResults(target, plan);

                if (plan.MarkTranscribed)
                {
                    target["source"] = "transcribed";
                }
            }

            var serialized = node.ToJsonString(Write);

            File.WriteAllText(group.Key, json ? serialized + "\n" : head + serialized + tail);
        }
    }

    /// <summary>Appends the missing ids to a string array field, creating it when absent.</summary>
    static void AppendStrings(JsonObject target, string field, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        // A lone string keeps its value first when the field grows into an array.
        if (target[field] is JsonValue single && single.TryGetValue<string>(out var held))
        {
            var grown = new JsonArray { held };

            foreach (var id in ids)
            {
                if (!string.Equals(id, held, StringComparison.Ordinal))
                {
                    grown.Add(id);
                }
            }

            target[field] = grown;

            return;
        }

        if (target[field] is not JsonArray array)
        {
            target[field] = new JsonArray(ids.Select(id => JsonValue.Create(id)).ToArray());
            return;
        }

        var present = new HashSet<string>(
            array.OfType<JsonValue>()
                .Select(value => value.TryGetValue<string>(out var text) ? text : null)
                .OfType<string>(),
            StringComparer.Ordinal);

        foreach (var id in ids)
        {
            if (present.Add(id))
            {
                array.Add(id);
            }
        }
    }

    /// <summary>Appends the missing effect entries, each with its transcribed timing.</summary>
    static void AppendEffects(JsonObject target, IReadOnlyList<(string Verb, string Target)> effects)
    {
        if (effects.Count == 0)
        {
            return;
        }

        if (target["effects"] is not JsonArray array)
        {
            array = [];
            target["effects"] = array;
        }

        foreach (var (verb, id) in effects)
        {
            // A call runs in its handler's call; any other edge is drafted after commit
            // because code cannot show its timing, for a person to tighten on review.
            var entry = new JsonObject
            {
                [verb] = id,
                ["timing"] = verb == "calls" ? "inline" : "after-commit",
            };

            array.Add(entry);
        }
    }

    /// <summary>Appends the missing refusal results to the page's records array.</summary>
    static void AppendResults(JsonObject target, RecordPlan plan)
    {
        if (plan.Results.Count == 0)
        {
            return;
        }

        if (target["records"] is not JsonArray array)
        {
            array = [];
            target["records"] = array;
        }

        foreach (var (code, id, title) in plan.Results)
        {
            var entry = new JsonObject
            {
                ["type"] = "result",
                ["id"] = id,
                ["title"] = title,
            };

            if (plan.Record.Owner is { } owner)
            {
                entry["owner"] = owner;
            }

            entry["outcome"] = "refusal";
            entry["code"] = code;
            entry["meaning"] = $"Transcribed from code ({code}).";
            entry["source"] = "transcribed";

            array.Add(entry);
        }
    }

    /// <summary>The object carrying the id: the page itself, an entry, or a nested entry.</summary>
    static JsonNode? Find(JsonNode node, string id)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["id"] is JsonValue value && value.TryGetValue<string>(out var own) && own == id)
                {
                    return obj;
                }

                foreach (var (_, child) in obj)
                {
                    if (child is not null && Find(child, id) is { } found)
                    {
                        return found;
                    }
                }

                return null;

            case JsonArray array:
                foreach (var child in array)
                {
                    if (child is not null && Find(child, id) is { } found)
                    {
                        return found;
                    }
                }

                return null;

            default:
                return null;
        }
    }

    /// <summary>Splits a page into the opening fence, its frontmatter and the rest.</summary>
    static (string Head, JsonNode? Node, string Tail) SplitFrontmatter(string text)
    {
        const string fence = "---\n";
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!normalized.StartsWith(fence, StringComparison.Ordinal))
        {
            return ("", null, "");
        }

        var end = normalized.IndexOf("\n---", fence.Length, StringComparison.Ordinal);

        if (end < 0)
        {
            return ("", null, "");
        }

        var body = normalized[fence.Length..end];

        return (fence, JsonNode.Parse(body), normalized[end..]);
    }
}
