using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     Each record's effective status, shared by every check host.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class Authority
{
    /// <summary>
    ///     Computes each record's effective status: draft when the record is a draft, when a draft
    ///     decision decides it, or when a requirement it fulfills is effectively a draft. Reviewed
    ///     counts as live like approved. A result or a scenario takes its use case's effective status.
    ///     A screen is draft when its entry point, a use case it offers, or a journey it serves is
    ///     effectively a draft.
    /// </summary>
    /// <param name="records">Every record.</param>
    /// <param name="useCaseHolder">The use case holding a result or scenario, when one holds it.</param>
    /// <returns>The effective status by record id.</returns>
    public static Dictionary<string, string> EffectiveStatus(
        IReadOnlyList<IRecordFacts> records,
        Func<IRecordFacts, IRecordFacts?> useCaseHolder)
    {
        var draftDecided = new HashSet<string>(
            records
                .Where(record => record.Type == "decision" && record.Status == "draft")
                .SelectMany(decision => decision.Ids("decides")),
            StringComparer.Ordinal);

        string Own(IRecordFacts record) =>
            record.Status == "draft" || draftDecided.Contains(record.Id) ? "draft" : record.Status ?? "approved";

        var effective = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var record in records.Where(record => record.Type is "product" or "goal" or "problem" or "feature" or "topic" or "requirement" or "constraint" or "decision" or "research" or "actor" or "module" or "role" or "permission" or "external-system" or "event" or "integration-event" or "aggregate" or "runbook" or "exception" or "term"))
        {
            effective[record.Id] = Own(record);
        }

        foreach (var record in records.Where(record => record.Type is "rule" or "use-case" or "reaction" or "job" or "notification" or "setting" or "entry-point" or "journey"))
        {
            var own = Own(record);
            var inherited = record.Ids("fulfills").Any(id =>
                effective.TryGetValue(id, out var status) && status == "draft");

            effective[record.Id] = Live(own) && inherited ? "draft" : own;
        }

        foreach (var record in records.Where(record => record.Type is "screen"))
        {
            var own = Own(record);
            var parents = record.Ids("entryPoint")
                .Concat(record.Ids("offers"))
                .Concat(record.Ids("serves"))
                .ToList();

            var entryPoint = record.Field("entryPoint");

            if (entryPoint is not null && !parents.Contains(entryPoint, StringComparer.Ordinal))
            {
                parents.Add(entryPoint);
            }

            var inherited = parents.Any(id =>
                effective.TryGetValue(Identifiers.BaseId(id), out var status) && status == "draft");

            effective[record.Id] = Live(own) && inherited ? "draft" : own;
        }

        foreach (var record in records.Where(record => record.Type is "result" or "scenario"))
        {
            var own = Own(record);
            var useCase = useCaseHolder(record);
            var inherited = useCase is not null
                && effective.TryGetValue(useCase.Id, out var status)
                && status == "draft";

            effective[record.Id] = Live(own) && inherited ? "draft" : own;
        }

        return effective;
    }

    /// <summary>Whether the status counts as live: approved or reviewed.</summary>
    /// <param name="status">The status to test.</param>
    /// <returns>True when the status is approved or reviewed.</returns>
    private static bool Live(string status) => status is "approved" or "reviewed";
}
