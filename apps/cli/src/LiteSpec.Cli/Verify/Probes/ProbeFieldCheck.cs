using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     One JSON Pointer check on a probe response body: the path with the
///     expected value or the required presence.
/// </summary>
/// <param name="Path">The JSON Pointer of the checked value, such as /state.</param>
/// <param name="Expected">The expected value at the path, or null for JSON null or when the check is presence only.</param>
/// <param name="HasExpected">True when the check compares the value, including JSON null.</param>
/// <param name="Present">True when the path must exist, false when it must be absent, or null when the check is equality only.</param>
internal sealed record ProbeFieldCheck(string Path, JsonNode? Expected, bool HasExpected, bool? Present);
