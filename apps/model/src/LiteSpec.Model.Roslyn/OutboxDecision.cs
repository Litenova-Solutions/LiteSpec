using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>One outbox decision a composition file states for a domain event type.</summary>
/// <follows>litespec/ext/outbox#decide-every-domain-event-outcome</follows>
public sealed class OutboxDecision : IEquatable<OutboxDecision>
{
    /// <summary>Initializes a new instance of the <see cref="OutboxDecision"/> class.</summary>
    /// <param name="eventType">The simple name of the decided event type.</param>
    /// <param name="ignored">Whether the decision drops the event; false when it translates it.</param>
    /// <param name="line">The 1-based line of the decision call.</param>
    /// <exception cref="ArgumentNullException">The event type is null.</exception>
    public OutboxDecision(string eventType, bool ignored, int line)
    {
        if (eventType is null)
        {
            throw new ArgumentNullException(nameof(eventType));
        }

        EventType = eventType;
        Ignored = ignored;
        Line = line;
    }

    /// <summary>Gets the simple name of the decided event type.</summary>
    public string EventType { get; }

    /// <summary>Gets a value indicating whether the decision drops the event; false when it translates it.</summary>
    public bool Ignored { get; }

    /// <summary>Gets the 1-based line of the decision call.</summary>
    public int Line { get; }

    /// <inheritdoc />
    public bool Equals(OutboxDecision? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(EventType, other.EventType, StringComparison.Ordinal)
            && Ignored == other.Ignored
            && Line == other.Line;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is OutboxDecision other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(EventType);
            hash = hash * 31 + Ignored.GetHashCode();
            hash = hash * 31 + Line.GetHashCode();
            return hash;
        }
    }

    /// <summary>Whether two decisions hold the same event type, direction, and line.</summary>
    /// <param name="left">The first decision.</param>
    /// <param name="right">The second decision.</param>
    /// <returns>True when both are null or hold the same values.</returns>
    public static bool operator ==(OutboxDecision? left, OutboxDecision? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>Whether two decisions differ.</summary>
    /// <param name="left">The first decision.</param>
    /// <param name="right">The second decision.</param>
    /// <returns>True when they hold different values.</returns>
    public static bool operator !=(OutboxDecision? left, OutboxDecision? right) => !(left == right);
}
