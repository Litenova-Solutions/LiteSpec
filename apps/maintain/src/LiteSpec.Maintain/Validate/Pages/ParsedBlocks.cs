using LiteSpec.Standards;

namespace LiteSpec.Maintain.Validate.Pages;

/// <summary>Everything one page parse collects for the later cross-page checks.</summary>
internal sealed record ParsedBlocks(string? Kind, List<ProvisionSection> Provisions);
