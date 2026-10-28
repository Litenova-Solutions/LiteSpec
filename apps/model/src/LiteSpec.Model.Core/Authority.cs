using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     Each record's effective status, shared by every check host.
/// </summary>
public static class Authority
{
    /// <summary>
    ///     Computes each record's effective status: draft when the record is a draft, when a draft
    ///     decision decides it, or when a requirement it fulfills is effectively a draft. Reviewed
    ///     counts as live like approved. A result or a scenario takes its use case's effective status.
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

        foreach (var record in records.Where(record => record.Type is "product" or "goal" or "problem" or "feature" or "requirement" or "constraint" or "decision" or "research" or "actor" or "module" or "role" or "permission" or "external-system" or "event" or "integration-event" or "aggregate" or "runbook" or "deviation" or "known-gap" or "term"))
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

    private static bool Live(string status) => status is "approved" or "reviewed";
}
