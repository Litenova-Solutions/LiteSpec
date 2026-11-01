namespace LiteSpec.Authoring;

/// <summary>
///     One provision with its heading, owning page and inline verification mark.
/// </summary>
/// <param name="Id">The provision identifier.</param>
/// <param name="Heading">The provision heading.</param>
/// <param name="Page">The owning page, relative to the repository root.</param>
/// <param name="Anchor">The heading slug the index links to.</param>
/// <param name="Kind">The assertion class.</param>
/// <param name="Mark">The inline verification mark, or empty when the provision carries none.</param>
/// <param name="Evidence">The inline verification evidence, or empty when the provision carries none.</param>
public sealed record ProvisionRow(string Id, string Heading, string Page, string Anchor, string Kind, string Mark, string Evidence);
