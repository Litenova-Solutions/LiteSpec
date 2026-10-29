using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     One marked aggregate method's raised events for the act-event check, by
///     event record id. The host keeps the method's own location and reads it
///     back through the finding's method index.
/// </summary>
public sealed class ActMethodRaises
{
    /// <summary>Creates the method's raised events.</summary>
    /// <param name="raised">The raised event ids, in source order.</param>
    /// <exception cref="ArgumentNullException">The raised ids are null.</exception>
    public ActMethodRaises(IReadOnlyList<string> raised)
    {
        if (raised is null)
        {
            throw new ArgumentNullException(nameof(raised));
        }

        Raised = raised;
    }

    /// <summary>The raised event ids, in source order.</summary>
    public IReadOnlyList<string> Raised { get; }
}

/// <summary>One decided act-event finding: the code, the method index, and the event.</summary>
public sealed class ActEventFinding
{
    /// <summary>Creates the finding.</summary>
    /// <param name="code">The finding code: <c>act-event-undeclared</c> or <c>act-event-unraised</c>.</param>
    /// <param name="method">The index of the method the finding sits on.</param>
    /// <param name="eventId">The event record id.</param>
    /// <exception cref="ArgumentNullException">The code or event id is null.</exception>
    public ActEventFinding(string code, int method, string eventId)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        if (eventId is null)
        {
            throw new ArgumentNullException(nameof(eventId));
        }

        Code = code;
        Method = method;
        EventId = eventId;
    }

    /// <summary>The finding code: <c>act-event-undeclared</c> or <c>act-event-unraised</c>.</summary>
    public string Code { get; }

    /// <summary>The index of the method the finding sits on.</summary>
    public int Method { get; }

    /// <summary>The event record id.</summary>
    public string EventId { get; }
}

/// <summary>
///     The act-event check both hosts run over one use case's marked methods.
/// </summary>
/// <follows>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</follows>
public static class ActEvents
{
    /// <summary>
    ///     Decides one use case's act-event findings: <c>act-event-undeclared</c>
    ///     once per method raising an event the use case does not list, and
    ///     <c>act-event-unraised</c> once per listed event no marked method raises,
    ///     located on the first marked method. A use case with no marked method
    ///     yields nothing: that case is <c>use-case-act-uncalled</c> or
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
                findings.Add(new ActEventFinding("act-event-unraised", 0, id));
            }
        }

        return findings;
    }
}
