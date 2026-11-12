namespace LiteSpec.Model;

/// <summary>The verb of a mark: what the marked element does for its record.</summary>
public enum MarkVerb
{
    /// <summary>
    ///     The element implements the record: the verb of every code mark.
    /// </summary>
    Implements = 0,

    /// <summary>
    ///     The element proves the record: the verb of every test mark.
    /// </summary>
    Proves = 1,

    /// <summary>
    ///     The element follows the choice: the verb of a mark naming the
    ///     provision, decision, or exception that shapes the code.
    /// </summary>
    Follows = 2,
}
