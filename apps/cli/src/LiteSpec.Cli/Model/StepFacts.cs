namespace LiteSpec.Model;

/// <summary>
///     What one steps method does through the helpers of its class: the request routes it names and the HTTP
///     statuses and refusal codes it asserts, so a shared then step such as <c>nothing_is_found</c> counts as the
///     assertion it makes.
/// </summary>
sealed record StepFacts(IReadOnlySet<string> Routes, IReadOnlySet<int> Statuses, IReadOnlySet<string> Codes);
