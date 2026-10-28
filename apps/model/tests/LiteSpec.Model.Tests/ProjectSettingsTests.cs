using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the project file settings read their defaults, both values, and every error.</summary>
public sealed class ProjectSettingsTests
{
    /// <summary>A file naming neither setting reads the xml style and the working phase.</summary>
    [Fact]
    public void Missing_Settings_Read_Defaults()
    {
        Assert.True(ProjectSettings.TryParse("{}", out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Equal(ProjectPhase.Working, settings.Phase);
        Assert.Null(error);
    }

    /// <summary>Both mark styles and both phases read.</summary>
    [Theory]
    [InlineData("xml", "working", MarkStyle.Xml, ProjectPhase.Working)]
    [InlineData("attribute", "frozen", MarkStyle.Attribute, ProjectPhase.Frozen)]
    public void Declared_Settings_Read(string marks, string phase, MarkStyle style, ProjectPhase expected)
    {
        var text = "{\"marks\": \"" + marks + "\", \"check\": {\"phase\": \"" + phase + "\"}}";

        Assert.True(ProjectSettings.TryParse(text, out var settings, out var error));

        Assert.Equal(style, settings.Marks);
        Assert.Equal(expected, settings.Phase);
        Assert.Null(error);
    }

    /// <summary>An unknown mark style is an error naming the value, never a silent default.</summary>
    [Fact]
    public void Unknown_Marks_Value_Errors()
    {
        Assert.False(ProjectSettings.TryParse("{\"marks\": \"yaml\"}", out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Equal(ProjectPhase.Working, settings.Phase);
        Assert.Equal("litespec.project.json: unknown marks \"yaml\"; expected \"xml\" or \"attribute\".", error);
    }

    /// <summary>An unknown phase is an error naming the value, never a silent default.</summary>
    [Fact]
    public void Unknown_Phase_Value_Errors()
    {
        Assert.False(ProjectSettings.TryParse("{\"check\": {\"phase\": \"paused\"}}", out var settings, out var error));

        Assert.Equal(MarkStyle.Xml, settings.Marks);
        Assert.Equal(ProjectPhase.Working, settings.Phase);
        Assert.Equal("litespec.project.json: unknown check.phase \"paused\"; expected \"working\" or \"frozen\".", error);
    }

    /// <summary>A wrong shape is an error naming the field, never a silent default.</summary>
    [Theory]
    [InlineData("{\"marks\": 5}", "litespec.project.json: marks must be \"xml\" or \"attribute\".")]
    [InlineData("{\"check\": \"frozen\"}", "litespec.project.json: check must be an object with phase \"working\" or \"frozen\".")]
    [InlineData("{\"check\": {\"phase\": 5}}", "litespec.project.json: check.phase must be \"working\" or \"frozen\".")]
    public void Wrong_Shape_Errors(string text, string message)
    {
        Assert.False(ProjectSettings.TryParse(text, out _, out var error));

        Assert.Equal(message, error);
    }

    /// <summary>Text that is no JSON and JSON that is no object are errors naming the file.</summary>
    [Theory]
    [InlineData("{\"marks\": ", "litespec.project.json: invalid JSON on line 1: expected a value.")]
    [InlineData("[\"xml\"]", "litespec.project.json: expected an object.")]
    public void Broken_Text_Errors(string text, string message)
    {
        Assert.False(ProjectSettings.TryParse(text, out _, out var error));

        Assert.Equal(message, error);
    }
}
