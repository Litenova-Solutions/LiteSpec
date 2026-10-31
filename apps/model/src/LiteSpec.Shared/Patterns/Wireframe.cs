namespace LiteSpec.Patterns;

/// <summary>A parsed wireframe: the page node, or null when the block has errors, and every error.</summary>
/// <param name="Page">The page node, or null when the block has errors.</param>
/// <param name="Errors">Every error in the block.</param>
public sealed record Wireframe(WireframeNode? Page, IReadOnlyList<WireframeError> Errors);
