namespace LiteSpec.Cli.Explore;

/// <summary>One fault the walk found: a 5xx response, a 4xx outside problem details, or a timeout, with the request and step number that reproduce it.</summary>
/// <param name="Kind">The fault kind: server-error, client-format or timeout.</param>
/// <param name="Operation">The operation name the fault is grouped under.</param>
/// <param name="Method">The request method.</param>
/// <param name="PathTemplate">The operation path template.</param>
/// <param name="Path">The concrete request path, with parameters filled.</param>
/// <param name="RequestBody">The request body, or null when the call sent none.</param>
/// <param name="StatusCode">The response status code, or 0 for a timeout.</param>
/// <param name="ResponseExcerpt">The first characters of the response body, or empty for a timeout.</param>
/// <param name="Detail">The problem detail, or null when the body carries none.</param>
/// <param name="Step">The step number that reproduces the call from the seed.</param>
sealed record ExploreFault(
    string Kind,
    string Operation,
    string Method,
    string PathTemplate,
    string Path,
    string? RequestBody,
    int StatusCode,
    string ResponseExcerpt,
    string? Detail,
    int Step)
{
    /// <summary>A 5xx response.</summary>
    internal const string ServerError = "server-error";

    /// <summary>A 4xx response whose body is not problem details.</summary>
    internal const string ClientFormat = "client-format";

    /// <summary>A request that timed out.</summary>
    internal const string Timeout = "timeout";
}
