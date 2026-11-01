namespace LiteSpec.Authoring;

/// <summary>
///     A parsed provision identifier.
/// </summary>
/// <param name="Page">The owning page path relative to <c>docs/</c> without its extension.</param>
/// <param name="Slug">The heading slug.</param>
public sealed record ProvisionId(string Page, string Slug);
