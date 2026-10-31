using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the project file settings read their default, both values, and every error.</summary>
public sealed class ProjectSettingsTests
{
    /// <summary>A file naming no setting reads the xml style.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact]
    public void Missing_Settings_Read_Defaults()
    {
        Assert.True(ProjectSettings.TryParse("{}", out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Null(error);
    }

    /// <summary>Both mark styles read.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory]
    [InlineData("xml", MarkStyle.Xml)]
    [InlineData("attribute", MarkStyle.Attribute)]
    public void Declared_Settings_Read(string marks, MarkStyle style)
    {
        var text = "{\"marks\": \"" + marks + "\"}";

        Assert.True(ProjectSettings.TryParse(text, out var settings, out var error));

        Assert.Equal(style, settings.Marks);
        Assert.Null(error);
    }

    /// <summary>An unknown mark style is an error naming the value, never a silent default.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact]
    public void Unknown_Marks_Value_Errors()
    {
        Assert.False(ProjectSettings.TryParse("{\"marks\": \"yaml\"}", out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Equal("litespec.project.json: unknown marks \"yaml\"; expected \"xml\" or \"attribute\".", error);
    }

    /// <summary>A wrong shape is an error naming the field, never a silent default.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory]
    [InlineData("{\"marks\": 5}", "litespec.project.json: marks must be \"xml\" or \"attribute\".")]
    public void Wrong_Shape_Errors(string text, string message)
    {
        Assert.False(ProjectSettings.TryParse(text, out _, out var error));

        Assert.Equal(message, error);
    }

    /// <summary>Text that is no JSON and JSON that is no object are errors naming the file.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory]
    [InlineData("{\"marks\": ", "litespec.project.json: invalid JSON on line 1: expected a value.")]
    [InlineData("[\"xml\"]", "litespec.project.json: expected an object.")]
    public void Broken_Text_Errors(string text, string message)
    {
        Assert.False(ProjectSettings.TryParse(text, out _, out var error));

        Assert.Equal(message, error);
    }
}
