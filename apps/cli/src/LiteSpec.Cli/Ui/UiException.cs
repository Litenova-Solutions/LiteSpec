using System.Text.Json.Nodes;

namespace LiteSpec.Ui;

/// <summary>One exception record the UI gates read their own departures from.</summary>
/// <param name="File">The exception file, relative to the consumer root.</param>
/// <param name="Provision">The provision the exception departs from.</param>
/// <param name="ExpiresOn">The recorded expiry date value.</param>
sealed record UiException(string File, string Provision, JsonNode? ExpiresOn);
