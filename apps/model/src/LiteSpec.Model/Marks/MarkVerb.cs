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

    /// <summary>
    ///     The element seeds a cast entry: a development seed or fixture that uses the
    ///     entry's values. Only a documentation tag or a TypeScript comment carries it;
    ///     it marks no code element, so element readers leave it out.
    /// </summary>
    Seeds = 3,
}
