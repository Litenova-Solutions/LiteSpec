using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec rename</c> inside free-text fields.</summary>
public sealed class RenameFreeTextTests
{
    /// <summary>Rename rewrites the identifier inside free-text fields that end a sentence on it.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename rewrites the identifier inside free-text fields that end a sentence on it")]
    public void RenameRewritesFreeTextCitations()
    {
        using var fixture = ModelFixture.Prepare("small");
        var useCase = Path.Combine(fixture.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.AppendAllText(useCase, "summary: Readers holding permission/borrow-copy.\n");
        File.AppendAllText(useCase, "exposure: Only readers with permission/borrow-copy.\n");
        var explanation = Path.ChangeExtension(useCase, ".md");
        File.AppendAllText(explanation, "\nBorrowing needs permission/borrow-copy.\n");

        var result = TestCli.Run("rename", "permission/borrow-copy", "permission/lend-copy", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        var rewritten = File.ReadAllText(useCase);
        Assert.Contains("Readers holding permission/lend-copy.", rewritten);
        Assert.Contains("Only readers with permission/lend-copy.", rewritten);
        Assert.DoesNotContain("permission/borrow-copy", rewritten);
        var notes = File.ReadAllText(explanation);
        Assert.Contains("Borrowing needs permission/lend-copy.", notes);
        Assert.DoesNotContain("permission/borrow-copy", notes);
    }

    /// <summary>Rename leaves an identifier that merely starts with the old one alone.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename leaves an identifier that merely starts with the old one alone")]
    public void RenameLeavesLongerIdentifierAlone()
    {
        using var fixture = ModelFixture.Prepare("small");
        var useCase = Path.Combine(fixture.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.AppendAllText(useCase, "summary: The permission/borrow-copy-audit trail stays.\n");
        var explanation = Path.ChangeExtension(useCase, ".md");
        File.AppendAllText(explanation, "\nThe permission/borrow-copy-audit trail stays.\n");

        var result = TestCli.Run("rename", "permission/borrow-copy", "permission/lend-copy", "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        var rewritten = File.ReadAllText(useCase);
        Assert.Contains("permission/borrow-copy-audit", rewritten);
        Assert.Contains("grant: permission/lend-copy", rewritten);
        var notes = File.ReadAllText(explanation);
        Assert.Contains("permission/borrow-copy-audit", notes);
    }
}
