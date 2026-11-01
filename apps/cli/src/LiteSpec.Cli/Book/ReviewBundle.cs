namespace LiteSpec.Book;

/// <summary>Everything the review queue shows: the review batches, in review order.</summary>
/// <param name="Batches">The review batches, in review order.</param>
sealed record ReviewBundle(IReadOnlyList<BookBatch> Batches);
