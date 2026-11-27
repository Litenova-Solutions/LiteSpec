using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Explore;

/// <summary>One OpenAPI operation the walk can call: its method, path template, parameters and request schema.</summary>
/// <param name="Method">The HTTP method in upper case.</param>
/// <param name="PathTemplate">The path template, with parameters in braces.</param>
/// <param name="Name">The operation id, or the method and template when it has none.</param>
/// <param name="PathParameters">The path parameter names in template order.</param>
/// <param name="QueryParameters">The query parameters.</param>
/// <param name="RequestSchema">The resolved JSON request schema, or null when it sends no body.</param>
/// <param name="RequestContentType">The media type the request schema was read from, or null when it sends no body.</param>
sealed record ExploreOperation(
    string Method,
    string PathTemplate,
    string Name,
    IReadOnlyList<string> PathParameters,
    IReadOnlyList<ExploreParameter> QueryParameters,
    JsonNode? RequestSchema,
    string? RequestContentType)
{
    /// <summary>Gets a value indicating whether the method reads without changing state.</summary>
    public bool IsRead => Method is "GET" or "HEAD" or "OPTIONS" or "TRACE";
}
