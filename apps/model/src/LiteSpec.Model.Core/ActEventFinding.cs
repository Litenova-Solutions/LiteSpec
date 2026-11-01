using System;

namespace LiteSpec.Model;

/// <summary>One decided act-event finding: the code, the method index, and the event.</summary>
public sealed class ActEventFinding
{
    /// <summary>Initializes a new instance of the <see cref="ActEventFinding"/> class.Creates the finding.</summary>
    /// <param name="code">The finding code: <c>act-event-undeclared</c> or <c>act-event-unimplemented</c>.</param>
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

    /// <summary>Gets the finding code: <c>act-event-undeclared</c> or <c>act-event-unimplemented</c>.</summary>
    public string Code { get; }

    /// <summary>Gets the index of the method the finding sits on.</summary>
    public int Method { get; }

    /// <summary>Gets the event record id.</summary>
    public string EventId { get; }
}
