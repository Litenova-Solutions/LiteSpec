namespace LiteSpec.Authoring;

/// <summary>Everything one page parse collects for the later cross-page checks.</summary>
internal sealed record ParsedBlocks(string? Kind, List<Provision> Provisions);
