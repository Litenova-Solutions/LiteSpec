namespace LiteSpec.Cli.Audit;

/// <summary>One audited address: the route path and the subject the report names.</summary>
/// <param name="Path">The route path, such as /expenses.</param>
/// <param name="Subject">The subject the finding names: the screen id, or the routes file name.</param>
sealed record AuditRoute(string Path, string Subject);
