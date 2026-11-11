using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Project;

/// <summary>Proves the explanation claims read their layers, drop unknown names, and default to none.</summary>
public sealed class ExplanationClaimsTests
{
    /// <summary>A file without explanations claims no layer.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void MissingExplanationsClaimNone()
    {
        Assert.True(ProjectSettings.TryParse("schemaVersion: 1\n", out var settings, out var error));

        Assert.Empty(settings.Explanations.Layers);
        Assert.Null(error);
    }

    /// <summary>Declared layers read in declaration order.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void DeclaredLayersReadInOrder()
    {
        Assert.True(ProjectSettings.TryParse("explanations:\n  layers:\n  - intent\n  - behavior\n", out var settings, out var error));

        Assert.Equal(["intent", "behavior"], settings.Explanations.Layers);
        Assert.Null(error);
    }

    /// <summary>Unknown layer names are dropped; the project schema reports them.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void UnknownLayersAreDropped()
    {
        Assert.True(ProjectSettings.TryParse("explanations:\n  layers:\n  - intent\n  - governance\n", out var settings, out var error));

        Assert.Equal(["intent"], settings.Explanations.Layers);
        Assert.Null(error);
    }

    /// <summary>A value of the wrong shape reads as none; the project schema reports it, so the reader never does.</summary>
    /// <param name="text">The project file text under test.</param>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Theory]
    [InlineData("explanations: intent\n")]
    [InlineData("explanations:\n  layers: intent\n")]
    public void WrongShapeReadsNone(string text)
    {
        Assert.True(ProjectSettings.TryParse(text, out var settings, out var error));

        Assert.Empty(settings.Explanations.Layers);
        Assert.Null(error);
    }
}
