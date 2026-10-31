using System.Text.Json.Nodes;

namespace LiteSpec.Ui;

/// <summary>One declared frontend, with the raw values the outputs print.</summary>
/// <param name="NameFound">Whether the frontend declares a name.</param>
/// <param name="Name">The declared name value.</param>
/// <param name="PlatformFound">Whether the frontend declares a platform.</param>
/// <param name="Platform">The declared platform value.</param>
sealed record UiFrontend(bool NameFound, JsonNode? Name, bool PlatformFound, JsonNode? Platform);
