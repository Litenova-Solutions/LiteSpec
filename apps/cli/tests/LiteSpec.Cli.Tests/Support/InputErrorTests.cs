using System.Text.Json.Nodes;
using LiteSpec.Tests.Model;

namespace LiteSpec.Tests.Support;

/// <summary>
///     Proves that ordinary user error reaches a message and a documented exit
///     code, never an unhandled exception.
/// </summary>
public sealed class InputErrorTests
{
    /// <summary>Model Reports A Malformed Project File As A Usage Error.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Model_Reports_A_Malformed_Project_File_As_A_Usage_Error()
    {
        using var folder = Cli.Temp("model-json");
        File.WriteAllText(Path.Combine(folder.Path, ".litespec/project.json"), "{ not json");

        var result = Cli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains(".litespec/project.json is not valid JSON", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Model Refuses A Root Without A Project File.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Model_Refuses_A_Root_Without_A_Project_File()
    {
        using var folder = Cli.Temp("model-empty");

        var result = Cli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.json", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Model Finds The Consumer By Walking Up To The LiteSpec Folder.</summary>
    /// <proves>litespec/core/scope#keep-every-litespec-file-under-one-folder</proves>
    [Fact]
    public void Model_Finds_The_Consumer_By_Walking_Up_To_The_LiteSpec_Folder()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = Cli.Run("model", "check", "--root", Path.Combine(fixture.Path, ".litespec", "behavior"));

        Assert.NotEqual(2, result.Status);
        Assert.DoesNotContain("holds no .litespec/project.json", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Book Accepts An Option Written With An Equals Sign.</summary>
    [Fact]
    public void Book_Accepts_An_Option_Written_With_An_Equals_Sign()
    {
        using var folder = Cli.Temp("book");

        var result = Cli.Run("book", $"--model={Cli.Fixture("Book/model.json")}", $"--out={folder.Path}");

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(folder.Path, "index.html")));
    }
}
