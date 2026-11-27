namespace LiteSpec.Cli.Book.Details;

/// <summary>The Intent product-area grouping: one entry per Behavior module, with the features and flows no module reaches.</summary>
/// <param name="Modules">The product areas in identifier order.</param>
/// <param name="Unfulfilled">The features and flows no module reaches.</param>
sealed record ProductAreas(IReadOnlyList<ProductArea> Modules, UnfulfilledArea Unfulfilled);
