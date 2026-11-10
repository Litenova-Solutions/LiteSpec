using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the project file settings read their defaults, the declared values, and the layers the project claims.</summary>
public sealed class ProjectSettingsTests
{
    /// <summary>A file naming no setting reads the xml style and every layer with LiteSpec's practice.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact]
    public void Missing_Settings_Read_Defaults()
    {
        Assert.True(ProjectSettings.TryParse("schemaVersion: 1\n", out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Equal(LayerClaims.All, settings.Layers);
        Assert.Null(error);
    }

    /// <summary>Both mark styles read.</summary>
    /// <param name="marks">The mark style name the project file declares.</param>
    /// <param name="style">The mark style the declared name must read as.</param>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory]
    [InlineData("xml", MarkStyle.Xml)]
    [InlineData("attribute", MarkStyle.Attribute)]
    public void Declared_Settings_Read(string marks, MarkStyle style)
    {
        var text = "marks: " + marks + "\n";

        Assert.True(ProjectSettings.TryParse(text, out var settings, out var error));

        Assert.Equal(style, settings.Marks);
        Assert.Null(error);
    }

    /// <summary>An unknown or wrong-shaped mark style reads the default; the project schema reports it, so the reader never does.</summary>
    /// <param name="text">The project file text under test.</param>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory]
    [InlineData("marks: yaml\n")]
    [InlineData("marks: 5\n")]
    public void Invalid_Marks_Value_Reads_The_Default(string text)
    {
        Assert.True(ProjectSettings.TryParse(text, out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Null(error);
    }

    /// <summary>Each layer claim set reads as the layers and practices it declares; a layer left out is not claimed.</summary>
    /// <param name="layers">The layers mapping the project file declares.</param>
    /// <param name="implementation">The Implementation practice the claims must read: none, litespec or own.</param>
    /// <param name="proof">The Proof practice the claims must read: none, litespec or own.</param>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Theory]
    [InlineData("intent: litespec\n  behavior: litespec", "none", "none")]
    [InlineData("intent: litespec\n  behavior: litespec\n  implementation: own\n  proof: own", "own", "own")]
    [InlineData("intent: litespec\n  behavior: litespec\n  implementation: litespec\n  proof: litespec", "litespec", "litespec")]
    [InlineData("intent: litespec\n  behavior: litespec\n  implementation: litespec\n  proof: own", "litespec", "own")]
    [InlineData("intent: litespec\n  behavior: litespec\n  proof: own", "none", "own")]
    public void Layer_Claims_Read_As_Declared(string layers, string implementation, string proof)
    {
        var text = "layers:\n  " + layers + "\n";

        Assert.True(ProjectSettings.TryParse(text, out var settings, out var error));

        Assert.True(settings.Layers.Intent);
        Assert.True(settings.Layers.Behavior);
        Assert.Equal(PracticeOf(implementation), settings.Layers.Implementation);
        Assert.Equal(PracticeOf(proof), settings.Layers.Proof);
        Assert.Null(error);
    }

    /// <summary>A project file claiming only Intent claims no Behavior, Implementation or Proof.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void Intent_Only_Claim_Leaves_Every_Other_Layer_Unclaimed()
    {
        Assert.True(ProjectSettings.TryParse("layers:\n  intent: litespec\n", out var settings, out var error));

        Assert.True(settings.Layers.Intent);
        Assert.False(settings.Layers.Behavior);
        Assert.Null(settings.Layers.Implementation);
        Assert.Null(settings.Layers.Proof);
        Assert.Null(error);
    }

    /// <summary>The practice a declared name reads as, or null when the layer is not claimed.</summary>
    /// <param name="name">none, litespec or own.</param>
    /// <returns>The practice, or null.</returns>
    static LayerPractice? PracticeOf(string name) => name switch
    {
        "none" => null,
        "own" => LayerPractice.Own,
        _ => LayerPractice.LiteSpec,
    };

    /// <summary>Text that is no YAML and YAML that is no mapping are errors naming the file.</summary>
    /// <param name="text">The project file text under test.</param>
    /// <param name="message">The expected error message.</param>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory]
    [InlineData("marks: [xml\n", ".litespec/project.yaml is not valid YAML or JSON: expected valid YAML at line 2.")]
    [InlineData("- xml\n", ".litespec/project.yaml: expected a mapping.")]
    public void Broken_Text_Errors(string text, string message)
    {
        Assert.False(ProjectSettings.TryParse(text, out _, out var error));

        Assert.Equal(message, error);
    }
}
