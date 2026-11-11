namespace LiteSpec.Maintain.Validate.Pages;

/// <summary>One provision heading and its body lines. <see cref="ProvisionSection.Claim"/> is set once the labels parse.</summary>
internal sealed record ProvisionSection(string Id, string Title, string Section, int Line, List<PageLine> Body)
{
    /// <summary>Gets or sets the comparable claim text parsed from the provision assertion.</summary>
    public string Claim { get; set; } = "";
}
