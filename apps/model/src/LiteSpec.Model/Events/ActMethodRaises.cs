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
    /// <summary>Initializes a new instance of the <see cref="ActMethodRaises"/> class.Creates the method's raised events.</summary>
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

    /// <summary>Gets the raised event ids, in source order.</summary>
    public IReadOnlyList<string> Raised { get; }
}
