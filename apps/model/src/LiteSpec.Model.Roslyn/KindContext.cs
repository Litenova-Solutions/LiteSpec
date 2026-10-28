using System;
using System.Collections.Generic;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Type names the host found elsewhere that syntax alone cannot resolve: the
///     aggregate roots, domain exceptions, and marked ports known through
///     inheritance across files. The CLI collects these across files before it
///     reads kinds; an analyzer fills them from symbols. Names are simple type
///     names without qualification or type arguments, compared with ordinal
///     comparison, and trusted as given.
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
    /// <exception cref="ArgumentNullException">Any collection is null.</exception>
    public KindContext(
        IEnumerable<string> aggregateRoots,
        IEnumerable<string> domainExceptions,
        IEnumerable<string> markedPorts)
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
    }

    /// <summary>An empty context: no cross-file names are known.</summary>
    public static KindContext Empty { get; } = new KindContext();

    /// <summary>Simple names of known aggregate roots.</summary>
    public IReadOnlyCollection<string> AggregateRoots { get; }

    /// <summary>Simple names of known domain exceptions.</summary>
    public IReadOnlyCollection<string> DomainExceptions { get; }

    /// <summary>Simple names of known marked ports.</summary>
    public IReadOnlyCollection<string> MarkedPorts { get; }
}
