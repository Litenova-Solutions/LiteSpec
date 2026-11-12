using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Model.Ui;

/// <summary>One departure record the UI gates read their own departures from.</summary>
/// <param name="File">The departure file, relative to the consumer root.</param>
/// <param name="Covers">The covers list: provision IDs, pages or finding codes the departure covers.</param>
/// <param name="ExpiresOn">The recorded expiry date value, or null for a lasting departure.</param>
sealed record UiDeparture(string File, IReadOnlyList<string> Covers, JsonNode? ExpiresOn);
