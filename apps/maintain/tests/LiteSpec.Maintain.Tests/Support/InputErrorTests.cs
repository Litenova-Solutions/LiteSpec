namespace LiteSpec.Tests.Support;

/// <summary>
///     Proves that ordinary user error reaches a message and a documented exit
///     code, never an unhandled exception.
/// </summary>
public sealed class InputErrorTests
{
    /// <summary>A Malformed Percent Escape In A Link Is A Broken Link.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void A_Malformed_Percent_Escape_In_A_Link_Is_A_Broken_Link()
    {
        using var folder = Cli.Temp("escape");
        Directory.CreateDirectory(Path.Combine(folder.Path, "docs"));
        File.WriteAllText(Path.Combine(folder.Path, "docs", "page.md"), "# Page\n\nSee [the guide](guide%zz.md).\n");

        var result = Cli.Run("validate", "--root", folder.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("[LINK_BROKEN] link target 'guide%zz.md' holds a malformed percent escape", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Provisions Refuses A Root Without Docs.</summary>
    /// <proves>litespec/core/authoring#regenerate-the-provision-index</proves>
    [Fact]
    public void Provisions_Refuses_A_Root_Without_Docs()
    {
        using var folder = Cli.Temp("provisions");

        var result = Cli.Run("provisions", "--root", Path.Combine(folder.Path, "missing"));

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no docs folder", result.Stderr, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(folder.Path, "missing")));
    }
}
