using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The async settle checks: a reaction or job that runs a use case writing an aggregate names the pending, done and refused states the run settles, entered and distinct so a reader tells them apart.</summary>
/// <follows>litespec/core/automation#settle-every-async-outcome-in-visible-states</follows>
internal static class AsyncSettleChecks
{
    /// <summary>The reaction and job field naming the visible states of the settled aggregate.</summary>
    const string Settles = "settles";

    /// <summary>The settles field naming the state the run leaves.</summary>
    const string Pending = "pending";

    /// <summary>The settles field naming the state the run enters.</summary>
    const string Done = "done";

    /// <summary>The settles field naming the state a terminal refusal enters.</summary>
    const string Refused = "refused";

    /// <summary>The outcome naming work waiting on another attempt.</summary>
    const string RetryScheduled = "retry-scheduled";

    /// <summary>The failure mode that tries the run again.</summary>
    const string RetryMode = "retry";

    /// <summary>Reports each run use case writing an aggregate whose reaction or job names no usable pending, done and refused states.</summary>
    /// <param name="model">The loaded project model that holds every reaction, job, use case and aggregate.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var holder in model.Records.Where(record => record.Type is RecordTypes.Reaction or RecordTypes.Job))
        {
            var runs = holder.EdgeIds(RecordFields.Runs)
                .Select(Identifiers.BaseId)
                .Where(id => id.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);

            foreach (var runId in runs)
            {
                CheckRun(model, holder, runId, findings);
            }
        }
    }

    /// <summary>Reports the unsettled states of one use case a reaction or job runs.</summary>
    /// <param name="model">The loaded project model that resolves the run use case and the aggregate it writes.</param>
    /// <param name="holder">The reaction or job record running the use case.</param>
    /// <param name="runId">The run use case identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRun(ProjectModel model, Record holder, string runId, List<Finding> findings)
    {
        // An unknown run target is reference-unknown through the citation checks,
        // and a run target of another type is left to the shape checks.
        if (!model.ById.TryGetValue(runId, out var useCase) || useCase.Type != RecordTypes.UseCase)
        {
            return;
        }

        // A run writing nothing settles nothing, and a run writing several
        // aggregates is reviewed with its atomicity rule.
        var writes = useCase.Ids(RecordFields.Writes);

        if (writes.Count != 1)
        {
            return;
        }

        var aggregateId = Identifiers.BaseId(writes[0]);

        // An unknown written aggregate is reference-unknown through the use-case checks.
        if (!model.ById.TryGetValue(aggregateId, out var aggregate) || aggregate.Type != RecordTypes.Aggregate)
        {
            return;
        }

        if (holder.Raw.ValueKind != JsonValueKind.Object
            || !holder.Raw.TryGetProperty(Settles, out var settles)
            || settles.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding(
                "async-settles-missing",
                $"{Label(holder.Type)} '{holder.Id}' runs '{runId}' writing '{aggregateId}' but names no settles; name the pending, done and refused states the run settles.",
                holder.File, holder.Line, SubjectOf(holder)));
            return;
        }

        // A missing aggregate is record-invalid through the schema checks.
        var settledId = Str(settles, RecordFields.Aggregate);

        if (settledId is not { Length: > 0 })
        {
            return;
        }

        if (!string.Equals(settledId, aggregateId, StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "async-pending-missing",
                $"{Label(holder.Type)} '{holder.Id}' runs '{runId}' writing '{aggregateId}' but settles '{settledId}'; settle the aggregate the run writes.",
                holder.File, holder.Line, SubjectOf(holder)));

            // The citation checks read no settles field, so an unknown settled
            // aggregate is reported here rather than left to them.
            if (!model.ById.ContainsKey(Identifiers.BaseId(settledId)))
            {
                findings.Add(new Finding(
                    "reference-unknown",
                    $"{Label(holder.Type)} '{holder.Id}' settles '{settledId}', which names no written aggregate.",
                    holder.File, holder.Line, SubjectOf(holder)));
            }

            return;
        }

        var states = StateNames(aggregate);
        var pending = SettledState(holder, settles, states, Pending, "pending", findings);
        var done = SettledState(holder, settles, states, Done, "done", findings);
        var refused = SettledState(holder, settles, states, Refused, "refused", findings);
        var refusedPresent = Str(settles, Refused) is { Length: > 0 };

        if (pending is not null && done is not null && string.Equals(pending, done, StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "async-pending-missing",
                $"{Label(holder.Type)} '{holder.Id}' settles '{aggregateId}' from pending '{pending}' to done '{done}'; the reader tells pending from done, so name two states.",
                holder.File, holder.Line, SubjectOf(holder)));
        }

        if (pending is not null)
        {
            CheckPendingEntered(holder, useCase, aggregate, aggregateId, pending, findings);
        }

        if (refused is not null)
        {
            if ((pending is not null && string.Equals(refused, pending, StringComparison.Ordinal))
                || (done is not null && string.Equals(refused, done, StringComparison.Ordinal)))
            {
                findings.Add(new Finding(
                    "async-refused-unrecorded",
                    $"{Label(holder.Type)} '{holder.Id}' settles '{aggregateId}' with refused '{refused}', which repeats a settled state; name a state of its own.",
                    holder.File, holder.Line, SubjectOf(holder)));
            }
        }
        else if (!refusedPresent)
        {
            CheckRefusedOrRetry(holder, runId, aggregateId, findings);
        }
    }

    /// <summary>Reports a pending state that the settled run itself enters, with no earlier transition reaching it.</summary>
    /// <param name="holder">The reaction or job record running the use case.</param>
    /// <param name="useCase">The run use case record.</param>
    /// <param name="aggregate">The settled aggregate record.</param>
    /// <param name="aggregateId">The settled aggregate identifier.</param>
    /// <param name="pending">The pending state name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPendingEntered(
        Record holder, Record useCase, Record aggregate, string aggregateId, string pending, List<Finding> findings)
    {
        var entering = TransitionsOf(aggregate).Where(transition => transition.To == pending).ToList();

        // A pending state no transition reaches is state-unreached through the
        // lifecycle checks. A transition naming no mover is read as an earlier
        // transition, so only a pending entered solely by this run reports here.
        if (entering.Count == 0
            || entering.Any(transition => transition.MovedBy is not { Length: > 0 }
                || (!string.Equals(transition.MovedBy, useCase.Id, StringComparison.Ordinal)
                    && !string.Equals(transition.MovedBy, holder.Id, StringComparison.Ordinal))))
        {
            return;
        }

        findings.Add(new Finding(
            "async-pending-missing",
            $"{Label(holder.Type)} '{holder.Id}' settles '{aggregateId}' from pending '{pending}', which no earlier transition reaches; reach the pending state before the run moves to done.",
            holder.File, holder.Line, SubjectOf(holder)));
    }

    /// <summary>Reports a settled run with no refused state and no declared retry back to pending.</summary>
    /// <param name="holder">The reaction or job record running the use case.</param>
    /// <param name="runId">The run use case identifier.</param>
    /// <param name="aggregateId">The settled aggregate identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRefusedOrRetry(Record holder, string runId, string aggregateId, List<Finding> findings)
    {
        var retries = holder.OutcomeNames().Contains(RetryScheduled, StringComparer.Ordinal)
            || OnFailureMode(holder) == RetryMode;

        if (!retries)
        {
            findings.Add(new Finding(
                "async-refused-unrecorded",
                $"{Label(holder.Type)} '{holder.Id}' runs '{runId}' writing '{aggregateId}' with no refused state and no declared retry; name the refused state in settles, or declare the retry that returns the run to pending.",
                holder.File, holder.Line, SubjectOf(holder)));
        }
    }

    /// <summary>Reads one settled state name, reporting a name that is no state of the settled aggregate.</summary>
    /// <param name="holder">The reaction or job record naming the state.</param>
    /// <param name="settles">The settles object naming the state.</param>
    /// <param name="states">The state names of the settled aggregate.</param>
    /// <param name="field">The settles field naming the state.</param>
    /// <param name="role">The state role named in the finding: pending, done or refused.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>The state name, or null when it is missing or names no state.</returns>
    private static string? SettledState(
        Record holder, JsonElement settles, HashSet<string> states, string field, string role, List<Finding> findings)
    {
        var name = Str(settles, field);

        // A missing pending or done state is record-invalid through the schema
        // checks; a missing refused state is read by the caller instead.
        if (name is not { Length: > 0 })
        {
            return null;
        }

        if (!states.Contains(name))
        {
            findings.Add(new Finding(
                "reference-unknown",
                $"{Label(holder.Type)} '{holder.Id}' settles with {role} '{name}', which is no state of the settled aggregate.",
                holder.File, holder.Line, SubjectOf(holder)));
            return null;
        }

        return name;
    }

    /// <summary>The failure mode the reaction or job declares on its record.</summary>
    /// <param name="holder">The reaction or job record whose failure handling is read.</param>
    /// <returns>The onFailure mode, or null when the record states none.</returns>
    private static string? OnFailureMode(Record holder) =>
        holder.Raw.ValueKind == JsonValueKind.Object
        && holder.Raw.TryGetProperty(RecordFields.OnFailure, out var policy)
        && policy.ValueKind == JsonValueKind.Object
            ? Str(policy, RecordFields.Mode)
            : null;

    /// <summary>The aggregate's state names.</summary>
    /// <param name="aggregate">The aggregate record whose declared states are read.</param>
    /// <returns>The declared state names with ordinal comparison, or an empty set when the aggregate declares no states array.</returns>
    private static HashSet<string> StateNames(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.States, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return value.EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(RecordFields.Name, out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() is { Length: > 0 })
            .Select(entry => entry.GetProperty(RecordFields.Name).GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
