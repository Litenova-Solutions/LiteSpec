namespace LiteSpec.Standards;

/// <summary>
///     One provision with its heading, owning page, inline verification mark, scope, and body.
/// </summary>
/// <param name="Id">The provision identifier.</param>
/// <param name="Heading">The provision heading.</param>
/// <param name="Page">The owning page, relative to the repository root.</param>
/// <param name="Anchor">The heading slug the index links to.</param>
/// <param name="Kind">The assertion class.</param>
/// <param name="Mark">The inline verification mark, or empty when the provision carries none.</param>
/// <param name="Evidence">The inline verification evidence, or empty when the provision carries none.</param>
/// <param name="Stack">The page stack values, comma-separated, or empty when the page declares none.</param>
/// <param name="Concern">The page concern values, comma-separated, or empty when the page declares none.</param>
/// <param name="Artifact">The narrowed artifact values, comma-separated, or empty when the provision carries none.</param>
/// <param name="Layer">The record layer the provision governs: <c>any</c>, <c>intent</c>, <c>behavior</c>, <c>implementation</c>, or <c>proof</c>.</param>
/// <param name="Trace">Whether the provision ties its layer to the records, rather than stating LiteSpec's practice for it.</param>
/// <param name="Strength">The named strength, or empty when the provision carries none.</param>
/// <param name="Statement">The assertive line: the Statement, or the Default of a convention or pattern.</param>
/// <param name="Rationale">The Rationale paragraph, or empty when the provision carries none.</param>
/// <param name="Example">The Example block, or empty when the provision carries none.</param>
public sealed record ProvisionRow(
    string Id,
    string Heading,
    string Page,
    string Anchor,
    string Kind,
    string Mark,
    string Evidence,
    string Stack,
    string Concern,
    string Artifact,
    string Layer,
    bool Trace,
    string Strength,
    string Statement,
    string Rationale,
    string Example);
