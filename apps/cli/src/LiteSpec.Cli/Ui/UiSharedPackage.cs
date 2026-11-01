using System.Text.Json.Nodes;

namespace LiteSpec.Ui;

/// <summary>The shared package summary the human and JSON outputs print.</summary>
/// <param name="NameFound">Whether the package declares a name.</param>
/// <param name="Name">The declared name value.</param>
/// <param name="PathFound">Whether the package declares a path.</param>
/// <param name="Path">The declared path value.</param>
/// <param name="Exports">The declared public entry points.</param>
sealed record UiSharedPackage(bool NameFound, JsonNode? Name, bool PathFound, JsonNode? Path, IReadOnlyList<JsonNode?> Exports);
