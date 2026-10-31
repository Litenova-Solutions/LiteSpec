namespace LiteSpec.Book;

/// <summary>One incoming link: the record it comes from and the field that holds it.</summary>
/// <param name="From">The linking record's id.</param>
/// <param name="Field">The link field on that record.</param>
sealed record Backlink(string From, string Field);
