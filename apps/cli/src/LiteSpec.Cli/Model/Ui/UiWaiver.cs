using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Model.Ui;

/// <summary>One waiver record the UI gates read their own waivers from.</summary>
/// <param name="File">The waiver file, relative to the consumer root.</param>
/// <param name="Covers">The covers list: provision IDs, pages or finding codes the waiver covers.</param>
/// <param name="ExpiresOn">The recorded expiry date value, or null for a lasting waiver.</param>
sealed record UiWaiver(string File, IReadOnlyList<string> Covers, JsonNode? ExpiresOn);
