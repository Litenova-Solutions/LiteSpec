using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape;

/// <summary>The link direction checks: every link points from its record up to its promise, never down a layer, and every flow step naming a system actor is stood for by an external system.</summary>
/// <follows>litespec/core/model#write-links-on-the-record-that-points</follows>
/// <follows>litespec/core/model#point-every-link-up</follows>
internal static class LinkDirectionChecks
{
    /// <summary>Reports a link from a higher layer to a lower one, and a flow step naming a system actor no external system represents.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckDirection(model, findings);
        CheckStepSystemActors(model, findings);
    }

    /// <summary>Reports link-direction-invalid for a link whose target stands on a lower layer than its record.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDirection(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Derived || record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // The product stands above the record layers: its links state scope, not dependencies.
            if (!RecordTypes.LayerByType.TryGetValue(record.Type, out var source) || source == "root")
            {
                continue;
            }

            var reported = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in record.Raw.EnumerateObject())
            {
                if (!RecordTypes.LinkTargets.ContainsKey(property.Name)
                    && !RecordTypes.NestedLinkFields.Contains(property.Name)
                    && property.Name is not (RecordFields.Refusal or RecordFields.Provision))
                {
                    continue;
                }

                foreach (var target in ModelBuilder.TargetsOf(property.Value))
                {
                    if (!model.ById.TryGetValue(Identifiers.BaseId(target), out var resolved)
                        || !RecordTypes.LayerByType.TryGetValue(resolved.Type, out var layer)
                        || Rank(source) >= Rank(layer))
                    {
                        continue;
                    }

                    // A name no record holds is reference-unknown, so only a held lower record reports here.
                    if (reported.Add(property.Name + "\0" + target))
                    {
                        findings.Add(new Finding(
                            "link-direction-invalid",
                            $"Record '{record.Id}' links down to '{target}' in '{property.Name}'; every link points from the lower record to the higher one.",
                            record.File,
                            record.Line,
                            SubjectOf(record)));
                    }
                }
            }
        }
    }

    /// <summary>Reports a flow step naming a system actor that no external system represents.</summary>
    /// <param name="model">The loaded project model that holds every flow, actor and external system.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckStepSystemActors(ProjectModel model, List<Finding> findings)
    {
        var represented = model.Records
            .Where(record => record.Type == RecordTypes.ExternalSystem)
            .SelectMany(record => record.Ids(RecordFields.Represents))
            .Select(Identifiers.BaseId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var flow in model.Records.Where(record => !record.Derived && record.Type == RecordTypes.Flow))
        {
            if (flow.Raw.ValueKind != JsonValueKind.Object
                || !flow.Raw.TryGetProperty(RecordFields.Steps, out var steps)
                || steps.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var reported = new HashSet<string>(StringComparer.Ordinal);

            foreach (var step in steps.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object
                    || !step.TryGetProperty(RecordFields.Actor, out var actor)
                    || actor.ValueKind != JsonValueKind.String
                    || actor.GetString() is not { } actorId
                    || !model.ById.TryGetValue(Identifiers.BaseId(actorId), out var resolved)
                    || resolved.Type != RecordTypes.Actor
                    || resolved.Field(RecordFields.Kind) != "system"
                    || represented.Contains(Identifiers.BaseId(actorId)))
                {
                    continue;
                }

                if (reported.Add(actorId))
                {
                    findings.Add(new Finding(
                        "record-invalid",
                        $"Flow '{flow.Id}' holds a step naming '{actorId}', which no external system represents.",
                        flow.File,
                        flow.Line,
                        SubjectOf(flow)));
                }
            }
        }
    }

    /// <summary>The layer rank for direction: the root above, then intent, behavior, and governance last.</summary>
    /// <param name="layer">The record layer name.</param>
    /// <returns>The rank, lower for the higher layer.</returns>
    private static int Rank(string layer) => layer switch
    {
        "root" => 0,
        "intent" => 1,
        "behavior" => 2,
        _ => 3,
    };
}
