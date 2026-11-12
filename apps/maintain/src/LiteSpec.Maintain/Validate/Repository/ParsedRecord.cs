using System.Text.Json.Nodes;
using LiteSpec.Maintain.Validate.Pages;

namespace LiteSpec.Maintain.Validate.Repository;

/// <summary>One LiteSpec record: its record file, its parsed value, and its explanation page.</summary>
/// <param name="Relative">The record file path, relative to the repository root.</param>
/// <param name="Kind">The record kind: research or decision.</param>
/// <param name="Raw">The record file text.</param>
/// <param name="Node">The parsed record value, or null when the file does not parse.</param>
/// <param name="Error">The parse failure message, or empty when the file parses.</param>
/// <param name="Explanation">The explanation page, or null when the file is missing.</param>
internal sealed record ParsedRecord(string Relative, string Kind, string Raw, JsonNode? Node, string Error, ParsedPage? Explanation);
