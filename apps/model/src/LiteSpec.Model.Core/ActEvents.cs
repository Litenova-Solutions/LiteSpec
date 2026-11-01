using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The act-event check both hosts run over one use case's marked methods.
/// </summary>
/// <follows>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</follows>
public static class ActEvents
{
    /// <summary>
    ///     Decides one use case's act-event findings: <c>act-event-undeclared</c>
    ///     once per method raising an event the use case does not list, and
    ///     <c>act-event-unimplemented</c> once per listed event no marked method raises,
    ///     located on the first marked method. A use case with no marked method
    ///     yields nothing: that case is <c>use-case-act-missing</c> or
    ///     <c>mark-missing</c>.
    /// </summary>
    /// <param name="raises">The event ids the use case lists in raises, in record order.</param>
    /// <param name="methods">The marked methods' raised events, in source order.</param>
    /// <returns>The decided findings: undeclared in method order, then unraised in raises order.</returns>
    /// <exception cref="ArgumentNullException">The raises or methods are null.</exception>
    public static IReadOnlyList<ActEventFinding> Check(
        IReadOnlyList<string> raises,
        IReadOnlyList<ActMethodRaises> methods)
    {
        if (raises is null)
        {
            throw new ArgumentNullException(nameof(raises));
        }

        if (methods is null)
        {
            throw new ArgumentNullException(nameof(methods));
        }

        var findings = new List<ActEventFinding>();
        var declared = new HashSet<string>(raises, StringComparer.Ordinal);

        for (var index = 0; index < methods.Count; index++)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var id in methods[index].Raised)
            {
                if (seen.Add(id) && !declared.Contains(id))
                {
                    findings.Add(new ActEventFinding("act-event-undeclared", index, id));
                }
            }
        }

        if (methods.Count == 0)
        {
            return findings;
        }

        var raised = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in methods)
        {
            foreach (var id in method.Raised)
            {
                raised.Add(id);
            }
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in raises)
        {
            if (reported.Add(id) && !raised.Contains(id))
            {
                findings.Add(new ActEventFinding("act-event-unimplemented", 0, id));
            }
        }

        return findings;
    }
}
