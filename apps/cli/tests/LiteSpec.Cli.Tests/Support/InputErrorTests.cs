using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Model;

namespace LiteSpec.Cli.Tests.Support;

/// <summary>
///     Proves that ordinary user error reaches a message and a documented exit
///     code, never an unhandled exception.
/// </summary>
public sealed class InputErrorTests
{
    /// <summary>Model Reports A Malformed Project File As A Usage Error.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ModelReportsAMalformedProjectFileAsAUsageError()
    {
        using var folder = TestCli.Temp("model-json");
        File.WriteAllText(Path.Combine(folder.Path, ".litespec/project.yaml"), "{ not json");

        var result = TestCli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains(".litespec/project.yaml is not valid YAML or JSON", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Model Refuses A Root Without A Project File.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ModelRefusesARootWithoutAProjectFile()
    {
        using var folder = TestCli.Temp("model-empty");

        var result = TestCli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.yaml", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Model Finds The Consumer By Walking Up To The LiteSpec Folder.</summary>
    /// <proves>litespec/core/scope#keep-every-litespec-file-under-one-folder</proves>
    [Fact]
    public void ModelFindsTheConsumerByWalkingUpToTheLiteSpecFolder()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run("model", "check", "--root", Path.Combine(fixture.Path, ".litespec", "behavior"));

        Assert.NotEqual(2, result.Status);
        Assert.DoesNotContain("holds no .litespec/project.yaml", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Book Accepts An Option Written With An Equals Sign.</summary>
    [Fact]
    public void BookAcceptsAnOptionWrittenWithAnEqualsSign()
    {
        using var consumer = ModelFixture.Prepare("small");
        using var folder = TestCli.Temp("book");

        var result = TestCli.Run("book", $"--root={consumer.Path}", $"--out={folder.Path}");

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(folder.Path, "index.html")));
    }
}
