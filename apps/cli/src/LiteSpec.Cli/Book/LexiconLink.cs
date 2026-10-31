namespace LiteSpec.Book;

/// <summary>One Links-table row's book words: the field, its carriers and both phrases.</summary>
/// <param name="Field">The link field, such as serves or access.grant.</param>
/// <param name="Carriers">The record types carrying the link, as kebab-case ids.</param>
/// <param name="Forward">How the link reads from carrier to target, in the table's words.</param>
/// <param name="Inverse">How the link reads from target to carrier, in the table's words.</param>
sealed record LexiconLink(string Field, IReadOnlyList<string> Carriers, string Forward, string Inverse);
