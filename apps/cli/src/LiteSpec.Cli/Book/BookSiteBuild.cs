using LiteSpec.Cli.Book.Output;

namespace LiteSpec.Cli.Book;

/// <summary>The book built in memory: the site, the record and shard counts, the records no parent rule places, and the build time.</summary>
/// <param name="Site">The whole book as named files.</param>
/// <param name="Records">The number of records in the book.</param>
/// <param name="Shards">The number of data shards in the site.</param>
/// <param name="Unplaced">The identifiers of the records no parent rule places.</param>
/// <param name="GeneratedOn">The build time in UTC, in ISO 8601 form.</param>
sealed record BookSiteBuild(BookSite Site, int Records, int Shards, IReadOnlyList<string> Unplaced, string GeneratedOn);
