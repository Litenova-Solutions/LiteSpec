using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     One scenario probe the verify command runs: the scenario that declares it
///     with the caller, the request template and the expected response.
/// </summary>
/// <param name="ScenarioId">The scenario identifier, such as scenario/spending.submit-expense.submitting.</param>
/// <param name="OwnerId">The carrying page identifier, such as use-case/spending.submit-expense.</param>
/// <param name="File">The absolute record file path holding the scenario.</param>
/// <param name="Line">The 1-based line of the scenario entry in its file.</param>
/// <param name="As">The cast person id that calls, or null for anonymous.</param>
/// <param name="Method">The HTTP method, such as POST.</param>
/// <param name="Path">The request path template, with {name} placeholders.</param>
/// <param name="Body">The request body template, or null for none.</param>
/// <param name="Status">The expected HTTP status code.</param>
/// <param name="Refusal">The expected problem code, or null for none.</param>
/// <param name="Fields">The field checks on the response body, in declaration order.</param>
internal sealed record ProbeCase(
    string ScenarioId,
    string OwnerId,
    string File,
    int Line,
    string? As,
    string Method,
    string Path,
    JsonNode? Body,
    int Status,
    string? Refusal,
    IReadOnlyList<ProbeFieldCheck> Fields);
