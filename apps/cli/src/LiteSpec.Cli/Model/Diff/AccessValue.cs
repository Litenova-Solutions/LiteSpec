namespace LiteSpec.Model.Diff;

/// <summary>One parsed access grant: auth kinds, grant and tenant scope.</summary>
/// <param name="Auth">The auth kinds the grant allows.</param>
/// <param name="Grant">The grant, as written.</param>
/// <param name="Tenant">The tenant scope, as written.</param>
sealed record AccessValue(HashSet<string> Auth, string? Grant, string? Tenant);
