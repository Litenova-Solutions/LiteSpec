using System.Text.Json;
using LiteSpec.Cli.Model.Checks.RecordShape;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The single-aggregate checks: a command changes one aggregate instance, and every command belongs to the aggregate it changes.</summary>
/// <follows>litespec/core/use-cases#change-one-aggregate-in-each-command</follows>
internal static class SingleAggregateChecks
{
    /// <summary>Reports a command writing more than one aggregate without a decision naming it, and a command use case at module level.</summary>
    /// <param name="model">The loaded project model that holds every use case and decision.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record =>
            !record.Derived && record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            CheckWrites(model, useCase, findings);
            CheckModuleLevel(model, useCase, findings);
        }
    }

    /// <summary>Reports a command whose writes name more than one aggregate, unless its consistency boundBy rule names a decision.</summary>
    /// <param name="model">The loaded project model that resolves each written aggregate and holds every decision.</param>
    /// <param name="useCase">The command whose writes are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWrites(ProjectModel model, Record useCase, List<Finding> findings)
    {
        // A name no aggregate holds is reference-unknown, so only held aggregates count here.
        var written = useCase.Ids(RecordFields.Writes)
            .Select(Identifiers.BaseId)
            .Distinct(StringComparer.Ordinal)
            .Where(id => model.ById.TryGetValue(id, out var target) && target.Type == RecordTypes.Aggregate)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (written.Count < 2)
        {
            return;
        }

        // A decision records the judgment that two aggregates belong in one transaction. The use
        // case cites the invariant in consistency boundBy, and that rule names the decision in
        // basedOn, so every link points up: use case to rule by boundBy, rule to decision by basedOn.
        if (BasedOnDecision(model, useCase))
        {
            return;
        }

        findings.Add(new Finding(
            "use-case-writes-invalid",
            $"Command '{useCase.Id}' writes {written.Count} aggregates ('{string.Join("', '", written)}'); a command changes one aggregate, or its consistency boundBy rule states the invariant needing both in one transaction and names its decision in basedOn.",
            useCase.File,
            useCase.Line,
            SubjectOf(useCase)));
    }

    /// <summary>Reports a command use case at module level, where only module-wide reads sit.</summary>
    /// <param name="model">The loaded project model that holds the project configuration.</param>
    /// <param name="useCase">The command whose folder is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleLevel(ProjectModel model, Record useCase, List<Finding> findings)
    {
        if (!FolderChecks.UnderRoot(model.Config, useCase.File, out var segments))
        {
            return;
        }

        if (segments is ["behavior", "modules", _, "use-cases", ..])
        {
            findings.Add(new Finding(
                "record-placement-invalid",
                $"Command '{useCase.Id}' sits at module level; every command belongs to the aggregate it changes.",
                useCase.File,
                useCase.Line,
                SubjectOf(useCase)));
        }
    }

    /// <summary>Whether the use case's consistency boundBy names a rule that governs it and names, in its basedOn, a decision record the model holds.</summary>
    /// <param name="model">The loaded project model that holds every rule and decision.</param>
    /// <param name="useCase">The command whose boundBy is judged.</param>
    /// <returns>True when such a rule exists.</returns>
    private static bool BasedOnDecision(ProjectModel model, Record useCase)
    {
        var bound = BoundByOf(useCase)
            .Select(Identifiers.BaseId)
            .ToHashSet(StringComparer.Ordinal);

        if (bound.Count == 0)
        {
            return false;
        }

        return model.Records
            .Where(rule => !rule.Derived && rule.Type == RecordTypes.Rule)
            .Where(rule => bound.Contains(Identifiers.BaseId(rule.Id)))
            .Where(rule => rule.Ids(RecordFields.Governs).Select(Identifiers.BaseId).Contains(useCase.Id, StringComparer.Ordinal))
            .SelectMany(rule => rule.Ids(RecordFields.BasedOn).Select(Identifiers.BaseId))
            .Any(id => model.ById.TryGetValue(id, out var target) && target.Type == RecordTypes.Decision);
    }

    /// <summary>The rule identifiers a use case's consistency boundBy names.</summary>
    /// <param name="useCase">The use case record whose consistency boundBy is read.</param>
    /// <returns>The bound rule identifiers as a single value or an array, or an empty list when it binds none.</returns>
    private static IReadOnlyList<string> BoundByOf(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency)
            || consistency.ValueKind != JsonValueKind.Object
            || !consistency.TryGetProperty(RecordFields.BoundBy, out var bound))
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
}
