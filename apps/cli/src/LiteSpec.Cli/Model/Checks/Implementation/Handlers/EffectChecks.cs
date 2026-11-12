using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Model;

using static LiteSpec.Cli.Model.Checks.Implementation.Handlers.EffectEdges;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The effect checks: every dispatch a handler, reaction or job makes is an effect edge its record declares, and a message goes out only after the commit.</summary>
/// <follows>litespec/backend/application#dispatch-only-declared-effect-edges</follows>
/// <follows>litespec/backend/application#send-messages-after-commit</follows>
internal static class EffectChecks
{
    /// <summary>Runs the effect checks.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckEffectUndeclared(model, findings);
        CheckEffectBeforeCommit(model, findings);
    }

    /// <summary>Reports each dispatch a handler or host makes that its record declares no edge for.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEffectUndeclared(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Dispatches is not { Count: > 0 }
                || !model.ById.TryGetValue(handler.UseCaseId, out var useCase)
                || useCase.Type != RecordTypes.UseCase)
            {
                continue;
            }

            var edges = UseCaseEdges(useCase);

            foreach (var dispatch in handler.Dispatches)
            {
                if (!EdgeDeclared(model, edges, dispatch))
                {
                    findings.Add(new Finding(
                        "effect-undeclared",
                        $"Use case '{handler.UseCaseId}' {dispatch.Verb} '{DispatchTarget(dispatch)}'{ViaText(dispatch)}, which no effects entry of the use case declares.",
                        dispatch.File,
                        dispatch.Line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var host in HostRow.All(model))
        {
            if (host.Dispatches is not { Count: > 0 }
                || (host.RecordId ?? host.UseCaseId) is not { } holderId
                || !model.ById.TryGetValue(holderId, out var record)
                || record.Type is not (RecordTypes.Reaction or RecordTypes.Job or RecordTypes.UseCase))
            {
                continue;
            }

            // A reaction inside a use-case folder with no tag of its own belongs to that use case,
            // so its dispatches answer to the use case's effects.
            // Running the use case it sits with is the reaction's own trigger, not an edge.
            var edges = record.Type == RecordTypes.UseCase
                ? [.. UseCaseEdges(record), ("runs", record.Id)]
                : HolderEdges(record);
            var kind = record.Type switch
            {
                RecordTypes.UseCase => "Use case",
                RecordTypes.Reaction => "Reaction",
                _ => "Job",
            };

            foreach (var dispatch in host.Dispatches)
            {
                if (!EdgeDeclared(model, edges, dispatch))
                {
                    findings.Add(new Finding(
                        "effect-undeclared",
                        $"{kind} '{record.Id}' {dispatch.Verb} '{DispatchTarget(dispatch)}'{ViaText(dispatch)}, which no declared edge of the {record.Type} names.",
                        dispatch.File,
                        dispatch.Line,
                        record.Id));
                }
            }
        }
    }

    /// <summary>One dispatch's target as the message reads it: the port and method for calls, else the message.</summary>
    /// <param name="dispatch">The scanned dispatch whose target is shown.</param>
    /// <returns>The port and method for a call dispatch; otherwise the dispatched message name.</returns>
    private static string DispatchTarget(CodeDispatch dispatch) =>
        dispatch.Verb == "calls" ? $"{dispatch.Detail}.{dispatch.Target}" : dispatch.Target;

    /// <summary>The helper a one-hop attribution found the dispatch through, or nothing when direct.</summary>
    /// <param name="dispatch">The scanned dispatch whose attribution is shown.</param>
    /// <returns>The via clause naming the helper the dispatch was found through, or an empty string when direct.</returns>
    private static string ViaText(CodeDispatch dispatch) =>
        dispatch.Via is { Length: > 0 } via ? $" via '{via}'" : "";

    /// <summary>Reports each command use case that sends or calls inline, before its commit.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEffectBeforeCommit(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            if (useCase.Raw.ValueKind != JsonValueKind.Object
                || !useCase.Raw.TryGetProperty(RecordFields.Effects, out var effects)
                || effects.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in effects.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || !entry.TryGetProperty(RecordFields.Timing, out var timing)
                    || timing.ValueKind != JsonValueKind.String
                    || timing.GetString() != "inline")
                {
                    continue;
                }

                foreach (var verb in new[] { RecordFields.Sends, RecordFields.Calls })
                {
                    if (entry.TryGetProperty(verb, out var effect)
                        && effect.ValueKind == JsonValueKind.String
                        && effect.GetString() is { Length: > 0 } target)
                    {
                        findings.Add(new Finding(
                            "effect-timing-invalid",
                            $"Use case '{useCase.Id}' {verb} '{target}' inline.",
                            useCase.File,
                            useCase.Line,
                            useCase.Id));
                    }
                }
            }
        }
    }
}
