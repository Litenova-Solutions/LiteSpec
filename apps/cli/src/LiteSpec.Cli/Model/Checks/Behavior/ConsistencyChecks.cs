using System.Text.Json;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The consistency checks: each use case states a known consistency mode, a command writing several aggregates states its consistency, an atomic command writes the group its rule binds, a command states its replay and any conflict, each reservation names its release, a command bound by a rule over an aggregate it only reads names that aggregate in its guards, and a guard never reaches into another module.</summary>
/// <follows>litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands</follows>
/// <follows>litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit</follows>
/// <follows>litespec/core/domain-model#bind-every-atomic-write-group-in-its-rule</follows>
/// <follows>litespec/core/domain-model#release-every-reservation-by-a-named-use-case</follows>
/// <follows>litespec/core/use-cases#state-what-a-replay-does</follows>
internal static class ConsistencyChecks
{
    /// <summary>The consistency modes a command may state.</summary>
    private static readonly string[] Modes = ["atomic", "per-record"];

    /// <summary>The winners a conflict may name when two writes meet.</summary>
    private static readonly string[] Winners = ["first-write", "last-write", "merge"];

    /// <summary>The replay answers a command may state for a repeat.</summary>
    private static readonly string[] Replays = ["returns-first-result", "refuses", "acts-again"];

    /// <summary>Reports each use case with a consistency mode outside its set, a command writing several aggregates with no consistency, a query that states consistency or idempotency, and each command whose replay, recognition or conflict is missing or unknown.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunConsistency(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            var kind = useCase.Field(RecordFields.Kind);
            var mode = ConsistencyMode(useCase);
            var writes = useCase.Ids(RecordFields.Writes).Count;

            if (kind == "query" && (useCase.Has(RecordFields.Consistency) || useCase.Has(RecordFields.Idempotency)))
            {
                findings.Add(new Finding("record-invalid", $"Query '{useCase.Id}' states consistency or idempotency; a read is idempotent by kind.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (mode is not null && mode is not ("atomic" or "per-record"))
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' has consistency mode '{mode}' outside {string.Join(", ", Modes)}.", useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            if (kind == "command" && writes >= 2 && !useCase.Has(RecordFields.Consistency))
            {
                findings.Add(new Finding("record-invalid", $"Command '{useCase.Id}' writes {writes} aggregates and states no consistency.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (mode == "atomic" && writes > 1 && BoundByOf(useCase).Count == 0)
            {
                findings.Add(new Finding("consistency-cites-missing", $"Use case '{useCase.Id}' writes {writes} aggregates atomically with no boundBy rule.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (kind == "command")
            {
                CheckReplay(useCase, findings);
                CheckConflicts(useCase, findings);
            }
        }
    }

    /// <summary>Reports a command whose idempotency states no replay, a state mode with no recognizedBy, or a replay answer outside its set.</summary>
    /// <param name="useCase">The command whose idempotency is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckReplay(Record useCase, List<Finding> findings)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        // A command with no idempotency states no replay: the schema forbids one under mode none.
        if (Str(idempotency, RecordFields.Mode) == "none")
        {
            return;
        }

        var replay = Str(idempotency, RecordFields.Replay);

        if (replay is null)
        {
            findings.Add(new Finding("idempotency-replay-missing", $"Command '{useCase.Id}' states idempotency with no replay: say whether a repeat returns the first result, refuses, or acts again.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
        else if (!Replays.Contains(replay, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Command '{useCase.Id}' holds replay '{replay}' outside {string.Join(", ", Replays)}.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }

        if (Str(idempotency, RecordFields.Mode) == "state"
            && Str(idempotency, RecordFields.RecognizedBy) is not { Length: > 0 })
        {
            findings.Add(new Finding("idempotency-replay-missing", $"Command '{useCase.Id}' recognizes a repeat by state with no recognizedBy naming the state it checks.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>Reports a conflict that names no refusal or an unknown winner, and a conflict on a command that writes one aggregate.</summary>
    /// <param name="useCase">The command whose conflicts entries are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckConflicts(Record useCase, List<Finding> findings)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Conflicts, out var conflicts))
        {
            return;
        }

        if (conflicts.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Command '{useCase.Id}' holds conflicts as no list.", useCase.File, useCase.Line, SubjectOf(useCase)));
            return;
        }

        foreach (var entry in conflicts.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || Str(entry, RecordFields.Subject) is not { Length: > 0 }
                || Str(entry, RecordFields.Refusal) is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Command '{useCase.Id}' holds a conflict with no subject or no refusal.", useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            var winner = Str(entry, RecordFields.Winner);

            if (winner is null || !Winners.Contains(winner, StringComparer.Ordinal))
            {
                findings.Add(new Finding("record-invalid", $"Command '{useCase.Id}' holds conflict winner '{winner ?? "none"}' outside {string.Join(", ", Winners)}.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports an atomic use case whose writes differ from its atomicity rule's binds.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAtomicityDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ConsistencyMode(useCase) != "atomic")
            {
                continue;
            }

            var writes = useCase.Ids(RecordFields.Writes);

            if (writes.Count < 2)
            {
                continue;
            }

            foreach (var cited in BoundByOf(useCase).Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(cited, out var rule) || rule.Type != RecordTypes.Rule || rule.Field(RecordFields.Kind) != "atomicity")
                {
                    continue;
                }

                var binds = rule.Ids(RecordFields.Binds).Order(StringComparer.Ordinal).ToList();
                var written = writes.Order(StringComparer.Ordinal).ToList();

                if (binds.Count > 0 && !binds.SequenceEqual(written, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("atomicity-drift", $"Use case '{useCase.Id}' writes '{string.Join("', '", written)}' but its rule '{cited}' binds '{string.Join("', '", binds)}'.", useCase.File, useCase.Line, SubjectOf(useCase)));
                }
            }
        }
    }

    /// <summary>Reports an atomic group binding more than four aggregates.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAtomicityInvalid(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "atomicity"))
        {
            var binds = rule.Ids(RecordFields.Binds);

            if (binds.Count > 4)
            {
                findings.Add(new Finding("atomicity-invalid", $"Rule '{rule.Id}' binds {binds.Count} aggregates.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ConsistencyMode(useCase) != "atomic")
            {
                continue;
            }

            var writes = useCase.Ids(RecordFields.Writes);

            if (writes.Count > 4 && BoundByOf(useCase).Count == 0)
            {
                findings.Add(new Finding("atomicity-invalid", $"Use case '{useCase.Id}' writes {writes.Count} aggregates atomically.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The rule identifiers a use case's consistency boundBy names.</summary>
    /// <param name="useCase">The use case record whose consistency boundBy is read.</param>
    /// <returns>The bound rule identifiers as a single value or an array, or an empty list when it binds none.</returns>
    private static IReadOnlyList<string> BoundByOf(Record useCase) => ConsistencyListOf(useCase, RecordFields.BoundBy);

    /// <summary>The aggregate identifiers a use case's consistency guards names.</summary>
    /// <param name="useCase">The use case record whose consistency guards is read.</param>
    /// <returns>The guarded aggregate identifiers as a single value or an array, or an empty list when it guards none.</returns>
    internal static IReadOnlyList<string> GuardsOf(Record useCase) => ConsistencyListOf(useCase, RecordFields.Guards);

    /// <summary>The identifiers a use case's consistency entry names under one member.</summary>
    /// <param name="useCase">The use case record whose consistency entry is read.</param>
    /// <param name="member">The consistency member to read.</param>
    /// <returns>The member identifiers as a single value or an array, or an empty list when it names none.</returns>
    private static IReadOnlyList<string> ConsistencyListOf(Record useCase, string member)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency)
            || consistency.ValueKind != JsonValueKind.Object
            || !consistency.TryGetProperty(member, out var bound))
        {
            return [];
        }

        if (bound.ValueKind == JsonValueKind.String && bound.GetString() is { Length: > 0 } single)
        {
            return [single];
        }

        if (bound.ValueKind == JsonValueKind.Array)
        {
            return
            [
                .. bound.EnumerateArray()
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

            findings.Add(new Finding("record-invalid", $"Rule '{rule.Id}' holds releasedBy as neither a record id nor a never object.", rule.File, rule.Line, SubjectOf(rule)));
        }
    }

    /// <summary>Runs the many-aggregates check: a command states at most one atomic write group.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunManyAggregates(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record =>
            record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            if (ConsistencyMode(useCase) == "atomic" && BoundByOf(useCase).Count > 1)
            {
                findings.Add(new Finding("atomicity-duplicate", $"Use case '{useCase.Id}' saves more than one atomic write group.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports a command bound by a rule over an aggregate it reads but does not write, and whose guards name no such aggregate; a guard reaching into another module reports instead, since no guard holds across modules.</summary>
    /// <param name="model">The loaded project model that holds every use case, rule and aggregate record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunGuard(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record =>
            record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            CheckForeignGuards(model, useCase, findings);

            var bound = BoundByOf(useCase);

            if (bound.Count == 0)
            {
                continue;
            }

            var reads = useCase.Ids(RecordFields.Reads).ToHashSet(StringComparer.Ordinal);
            var writes = useCase.Ids(RecordFields.Writes).ToHashSet(StringComparer.Ordinal);
            var guards = GuardsOf(useCase).ToHashSet(StringComparer.Ordinal);

            // One finding per unguarded aggregate: the guard is named per aggregate,
            // so a second rule over the same aggregate adds no second finding.
            var missing = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (var cited in bound.Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(cited, out var rule) || rule.Type != RecordTypes.Rule)
                {
                    continue;
                }

                // Governs names aggregates, stores and use cases; only an aggregate id takes a guard.
                foreach (var governed in rule.Ids(RecordFields.Governs))
                {
                    if (!Identifiers.TrySplit(governed, out var prefix, out _, out _) || prefix != RecordTypes.Aggregate)
                    {
                        continue;
                    }

                    if (reads.Contains(governed) && !writes.Contains(governed) && !guards.Contains(governed))
                    {
                        missing.TryAdd(governed, cited);
                    }
                }
            }

            var home = LocalityChecks.ModuleOf(model, useCase);

            foreach (var (aggregate, ruleId) in missing)
            {
                if (home is { Length: > 0 }
                    && model.ById.TryGetValue(Identifiers.BaseId(aggregate), out var target)
                    && target.Type == RecordTypes.Aggregate
                    && LocalityChecks.ModuleOf(model, target) is { Length: > 0 } foreign
                    && !foreign.Equals(home, StringComparison.Ordinal))
                {
                    findings.Add(new Finding("guard-module-foreign", $"Command '{useCase.Id}' reads '{aggregate}' in module '{foreign}' under rule '{ruleId}', which cannot hold when the command commits.", useCase.File, useCase.Line, SubjectOf(useCase)));
                    continue;
                }

                findings.Add(new Finding("guard-undeclared", $"Command '{useCase.Id}' is bound by rule '{ruleId}' over '{aggregate}' and names no guard on '{aggregate}'.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports a command whose guards name an aggregate of another module, whose rule cannot hold when the command commits.</summary>
    /// <param name="model">The loaded project model that holds every use case and aggregate record.</param>
    /// <param name="useCase">The command whose guards are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckForeignGuards(ProjectModel model, Record useCase, List<Finding> findings)
    {
        if (LocalityChecks.ModuleOf(model, useCase) is not { Length: > 0 } home)
        {
            return;
        }

        foreach (var guard in GuardsOf(useCase).Order(StringComparer.Ordinal))
        {
            if (!model.ById.TryGetValue(Identifiers.BaseId(guard), out var target)
                || target.Type != RecordTypes.Aggregate
                || LocalityChecks.ModuleOf(model, target) is not { Length: > 0 } foreign
                || foreign.Equals(home, StringComparison.Ordinal))
            {
                continue;
            }

            findings.Add(new Finding("guard-module-foreign", $"Command '{useCase.Id}' guards on '{guard}' in module '{foreign}', whose rule cannot hold when the command commits.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }
}
