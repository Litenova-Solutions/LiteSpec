namespace LiteSpec.Authoring;

/// <summary>One parsed page: its relative path, its reader, and its blocks.</summary>
internal sealed record ParsedPage(string Relative, PageReader Reader, ParsedBlocks Parsed);
