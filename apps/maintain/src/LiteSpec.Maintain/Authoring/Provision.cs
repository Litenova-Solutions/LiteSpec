namespace LiteSpec.Authoring;

/// <summary>One provision heading and its body lines. <see cref="Provision.Claim"/> is set once the labels parse.</summary>
internal sealed record Provision(string Id, string Title, string Section, int Line, List<LineItem> Body)
{
    /// <summary>Gets or sets the comparable claim text parsed from the provision assertion.</summary>
    public string Claim { get; set; } = "";
}
