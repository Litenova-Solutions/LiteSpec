using System.Text.Json.Nodes;

namespace LiteSpec.Tests.Support;

/// <summary>
///     Proves that ordinary user error reaches a message and a documented exit
///     code, never an unhandled exception.
/// </summary>
public sealed class InputErrorTests
{
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Model_Reports_A_Malformed_Project_File_As_A_Usage_Error()
    {
        using var folder = Cli.Temp("model-json");
        File.WriteAllText(Path.Combine(folder.Path, "litespec.project.json"), "{ not json");

        var result = Cli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("litespec.project.json is not valid JSON", result.Stderr, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Model_Refuses_A_Root_Without_A_Project_File()
    {
        using var folder = Cli.Temp("model-empty");

        var result = Cli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no litespec.project.json", result.Stderr, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Book_Accepts_An_Option_Written_With_An_Equals_Sign()
    {
        using var folder = Cli.Temp("book");

        var result = Cli.Run("book", $"--model={Cli.Fixture("Book/model.json")}", $"--out={folder.Path}");

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(folder.Path, "index.html")));
    }
}
