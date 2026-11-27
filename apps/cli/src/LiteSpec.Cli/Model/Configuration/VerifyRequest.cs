using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Model.Configuration;

/// <summary>One HTTP request the verify commands send: a method, a path and an optional JSON body.</summary>
/// <param name="Method">The HTTP method, such as GET or POST.</param>
/// <param name="Path">The path from the base address, with {name} placeholders.</param>
/// <param name="Body">The JSON body, or null for none.</param>
sealed record VerifyRequest(string Method, string Path, JsonNode? Body);
