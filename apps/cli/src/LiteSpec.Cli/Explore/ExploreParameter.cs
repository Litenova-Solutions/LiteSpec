using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Explore;

/// <summary>One query parameter of an operation the walk can call: its name, whether the contract requires it, and its schema.</summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Required">Whether the contract requires the parameter.</param>
/// <param name="Schema">The resolved JSON schema, or null when it has none.</param>
sealed record ExploreParameter(string Name, bool Required, JsonNode? Schema);
