namespace LiteSpec.Model;

/// <summary>
///     One lookalike pair: two forms of one concept, ordered so the first sorts before the second.
/// </summary>
/// <param name="Kind">The pair kind: hyphenation, spelling, or plurality.</param>
/// <param name="First">The first form and where it first occurs.</param>
/// <param name="Second">The second form and where it first occurs.</param>
sealed record LookalikePair(string Kind, LookalikeHit First, LookalikeHit Second);
