namespace LiteSpec.Book;

/// <summary>One step of a record's why-chain: a plain label and the record it points at.</summary>
/// <param name="Label">The step label, such as Result or Goal.</param>
/// <param name="Id">The linked record's id.</param>
sealed record WhyLink(string Label, string Id);
