using System;
using System.Collections.Generic;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Type names the host found elsewhere that syntax alone cannot resolve: the
///     aggregate roots, domain exceptions, marked ports, command handlers, query
///     handlers, event handlers, job hosts, domain events, and technical classes
///     known through inheritance or another part across files. The CLI collects
///     these across files before it reads kinds; an analyzer fills them from
///     symbols. Names are simple type names without qualification or type
///     arguments, compared with ordinal comparison, and trusted as given.
/// </summary>
public sealed class KindContext
{
    /// <summary>Creates an empty context: no cross-file names are known.</summary>
    public KindContext()
        : this(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())
    {
    }

    /// <summary>Creates a context from the names the host collected.</summary>
    /// <param name="aggregateRoots">Simple names of known aggregate roots.</param>
    /// <param name="domainExceptions">Simple names of known domain exceptions.</param>
    /// <param name="markedPorts">Simple names of known marked ports.</param>
    /// <param name="commandHandlers">Simple names of types known to carry <c>ICommandHandler</c>, directly or through a base. Null means none are known.</param>
    /// <param name="queryHandlers">Simple names of types known to carry <c>IQueryHandler</c>, directly or through a base. Null means none are known.</param>
    /// <param name="eventHandlers">Simple names of types known to carry <c>IEventHandler</c> or <c>IDomainEventReaction</c>, directly or through a base. Null means none are known.</param>
    /// <param name="jobHosts">Simple names of types known to derive <c>BackgroundService</c> or implement <c>IHostedService</c>, directly or through a base, before the Job or Worker name check. Null means none are known.</param>
    /// <param name="domainEvents">Simple names of types known to carry <c>IDomainEvent</c>, directly or through a base, including interfaces that extend it. Null means none are known.</param>
    /// <param name="technicalClasses">Simple names of types known to be technical classes through another partial part. Null means none are known.</param>
    /// <exception cref="ArgumentNullException">Any of <paramref name="aggregateRoots" />, <paramref name="domainExceptions" /> or <paramref name="markedPorts" /> is null.</exception>
    public KindContext(
        IEnumerable<string> aggregateRoots,
        IEnumerable<string> domainExceptions,
        IEnumerable<string> markedPorts,
        IEnumerable<string>? commandHandlers = null,
        IEnumerable<string>? queryHandlers = null,
        IEnumerable<string>? eventHandlers = null,
        IEnumerable<string>? jobHosts = null,
        IEnumerable<string>? domainEvents = null,
        IEnumerable<string>? technicalClasses = null)
    {
        if (aggregateRoots is null)
        {
            throw new ArgumentNullException(nameof(aggregateRoots));
        }

        if (domainExceptions is null)
        {
            throw new ArgumentNullException(nameof(domainExceptions));
        }

        if (markedPorts is null)
        {
            throw new ArgumentNullException(nameof(markedPorts));
        }

        AggregateRoots = new HashSet<string>(aggregateRoots, StringComparer.Ordinal);
        DomainExceptions = new HashSet<string>(domainExceptions, StringComparer.Ordinal);
        MarkedPorts = new HashSet<string>(markedPorts, StringComparer.Ordinal);
        CommandHandlers = new HashSet<string>(commandHandlers ?? Array.Empty<string>(), StringComparer.Ordinal);
        QueryHandlers = new HashSet<string>(queryHandlers ?? Array.Empty<string>(), StringComparer.Ordinal);
        EventHandlers = new HashSet<string>(eventHandlers ?? Array.Empty<string>(), StringComparer.Ordinal);
        JobHosts = new HashSet<string>(jobHosts ?? Array.Empty<string>(), StringComparer.Ordinal);
        DomainEvents = new HashSet<string>(domainEvents ?? Array.Empty<string>(), StringComparer.Ordinal);
        TechnicalClasses = new HashSet<string>(technicalClasses ?? Array.Empty<string>(), StringComparer.Ordinal);
    }

    /// <summary>An empty context: no cross-file names are known.</summary>
    public static KindContext Empty { get; } = new KindContext();

    /// <summary>Simple names of known aggregate roots.</summary>
    public IReadOnlyCollection<string> AggregateRoots { get; }

    /// <summary>Simple names of known domain exceptions.</summary>
    public IReadOnlyCollection<string> DomainExceptions { get; }

    /// <summary>Simple names of known marked ports.</summary>
    public IReadOnlyCollection<string> MarkedPorts { get; }

    /// <summary>Simple names of types known to carry <c>ICommandHandler</c>, directly or through a base.</summary>
    public IReadOnlyCollection<string> CommandHandlers { get; }

    /// <summary>Simple names of types known to carry <c>IQueryHandler</c>, directly or through a base.</summary>
    public IReadOnlyCollection<string> QueryHandlers { get; }

    /// <summary>Simple names of types known to carry <c>IEventHandler</c> or <c>IDomainEventReaction</c>, directly or through a base.</summary>
    public IReadOnlyCollection<string> EventHandlers { get; }

    /// <summary>Simple names of types known to derive <c>BackgroundService</c> or implement <c>IHostedService</c>, directly or through a base, before the Job or Worker name check.</summary>
    public IReadOnlyCollection<string> JobHosts { get; }

    /// <summary>Simple names of types known to carry <c>IDomainEvent</c>, directly or through a base, including interfaces that extend it.</summary>
    public IReadOnlyCollection<string> DomainEvents { get; }

    /// <summary>Simple names of types known to be technical classes through another partial part.</summary>
    public IReadOnlyCollection<string> TechnicalClasses { get; }
}
