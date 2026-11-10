using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Consumer;

/// <summary>
///     Proves the one consumer-root rule: a command that reads a consumer refuses a folder
///     with no project file, and <c>init</c> writes into the folder it names.
/// </summary>
public sealed class ConsumerRootTests
{
    /// <summary>New refuses a folder with no project file and writes nothing into it.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "new refuses a folder with no project file")]
    public void NewRefusesAFolderWithNoProjectFile()
    {
        using var folder = Cli.Temp("root-new");

        var result = Cli.Run("new", "decision", "hold-notices", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.json", result.Stderr, StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(folder.Path, ".litespec")));
    }

    /// <summary>Rename refuses a folder with no project file.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "rename refuses a folder with no project file")]
    public void RenameRefusesAFolderWithNoProjectFile()
    {
        using var folder = Cli.Temp("root-rename");

        var result = Cli.Run("rename", "use-case/lending.borrow-copy", "use-case/lending.borrow-item", "--root", folder.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.json", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>Init writes into the folder it names, even when a folder above it is a consumer.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "init writes into the folder it names inside a consumer")]
    public void InitWritesIntoTheFolderItNamesInsideAConsumer()
    {
        using var consumer = ConsumerFixture.Create();
        var nested = Path.Combine(consumer.Path, "nested");
        Directory.CreateDirectory(nested);

        var result = Cli.Run("init", "--preset", "records", "--size", "small", "--root", nested);

        Assert.Equal(0, result.Status);
        Assert.True(File.Exists(Path.Combine(nested, ".litespec", "project.json")));
    }
}
