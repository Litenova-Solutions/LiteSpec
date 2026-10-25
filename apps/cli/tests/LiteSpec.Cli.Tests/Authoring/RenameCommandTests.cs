using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Tests for <c>litespec rename</c>.</summary>
public sealed class RenameCommandTests
{
    [Fact(DisplayName = "rename rewrites the identifier in source and test files as well as pages")]
    public void Rename_Rewrites_Code_Citations()
    {
        using var fixture = ModelFixture.Prepare("small");
        var source = Path.Combine(fixture.Path, "src", "App", "Lending", "Loans", "BorrowCopy", "BorrowCopyOnRequestReaction.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllText(source, "/// <implements>use-case/lending.borrow-copy</implements>\ninternal sealed class BorrowCopyOnRequestReaction;\n");

        var result = Cli.Run("rename", "use-case/lending.borrow-copy", "use-case/lending.borrow-item", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/lending.borrow-item", File.ReadAllText(source));
        Assert.DoesNotContain("use-case/lending.borrow-copy", File.ReadAllText(source));
    }
}
