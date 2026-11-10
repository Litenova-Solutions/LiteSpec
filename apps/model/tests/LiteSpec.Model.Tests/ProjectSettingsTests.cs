using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the project file settings read their defaults, the declared values, and the level the layer claims map to.</summary>
public sealed class ProjectSettingsTests
{
    /// <summary>A file naming no setting reads the xml style and the full level.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact]
    public void Missing_Settings_Read_Defaults()
    {
        Assert.True(ProjectSettings.TryParse("schemaVersion: 1\n", out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Equal(ConformanceLevel.Full, settings.Conformance);
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

    /// <summary>Each layer claim set reads as the level it maps to.</summary>
    /// <param name="layers">The layers mapping the project file declares.</param>
    /// <param name="level">The level the claims must read as.</param>
    /// <proves>litespec/core/scope#declare-the-conformance-level</proves>
    [Theory]
    [InlineData("intent: litespec\n  behavior: litespec", ConformanceLevel.Records)]
    [InlineData("intent: litespec\n  behavior: litespec\n  implementation: own\n  proof: own", ConformanceLevel.Traced)]
    [InlineData("intent: litespec\n  behavior: litespec\n  implementation: litespec\n  proof: litespec", ConformanceLevel.Full)]
    [InlineData("intent: litespec\n  behavior: litespec\n  implementation: litespec\n  proof: own", ConformanceLevel.Traced)]
    public void Layer_Claims_Read_As_Their_Level(string layers, ConformanceLevel level)
    {
        var text = "layers:\n  " + layers + "\n";

        Assert.True(ProjectSettings.TryParse(text, out var settings, out var error));

        Assert.Equal(level, settings.Conformance);
        Assert.Null(error);
    }

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
