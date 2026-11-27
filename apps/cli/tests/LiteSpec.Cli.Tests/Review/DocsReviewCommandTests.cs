using LiteSpec.Cli.Review;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Review;

/// <summary>Proves <c>litespec docs review</c> lints the files changed since a revision and writes the review packet.</summary>
public sealed class DocsReviewCommandTests
{
    /// <summary>A changed file with a banned phrase exits 1 and the packet holds every section with the finding.</summary>
    /// <proves>litespec/core/writing#write-human-docs-for-the-cold-reader</proves>
    [Fact]
    public void ChangedFileWithBannedPhraseExits1AndPacketHoldsEverySection()
    {
        using var folder = TestCli.Temp("docs-review-lint");
        ReviewTestGit.Init(folder.Path);
        Directory.CreateDirectory(Path.Combine(folder.Path, "docs"));
        File.WriteAllText(Path.Combine(folder.Path, "docs", "guide.md"), "# Guide\n\nAn approver clears each expense within five business days.\n");
        ReviewTestGit.CommitAll(folder.Path, "Add guide");
        File.AppendAllText(
            Path.Combine(folder.Path, "docs", "guide.md"),
            "This cutting-edge solution changes everything.\n");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = DocsReviewCommand.Execute(folder.Path, "HEAD", output, error, out var packet);

        Assert.Equal(1, exit);
        Assert.NotEqual("", packet);
        var text = File.ReadAllText(packet);
        Assert.Contains("## Changed Files", text);
        Assert.Contains("docs/guide.md", text);
        Assert.Contains("## Diffs", text);
        Assert.Contains("cutting-edge", text);
        Assert.Contains("## Style Card Rules", text);
        Assert.Contains("## Review Checklist", text);
        Assert.Contains("## Lint Findings", text);
        Assert.Contains("[promotional-language]", text);
        Assert.Contains("[promotional-language]", output.ToString());
    }

    /// <summary>A changed file in plain voice exits 0 and the packet reports no findings.</summary>
    /// <proves>litespec/core/writing#write-human-docs-for-the-cold-reader</proves>
    [Fact]
    public void CleanChangeExits0AndPacketReportsNoFindings()
    {
        using var folder = TestCli.Temp("docs-review-clean");
        ReviewTestGit.Init(folder.Path);
        File.WriteAllText(Path.Combine(folder.Path, "notes.md"), "# Notes\n\nNothing to review yet.\n");
        ReviewTestGit.CommitAll(folder.Path, "Add notes");
        File.AppendAllText(Path.Combine(folder.Path, "notes.md"), "An approver clears each expense within five business days.\n");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = DocsReviewCommand.Execute(folder.Path, "HEAD", output, error, out var packet);

        Assert.Equal(0, exit);
        Assert.Contains("No findings", File.ReadAllText(packet));
    }

    /// <summary>A long paragraph and a raw refusal code each exit 1 with their own lint code.</summary>
    /// <proves>litespec/core/writing#write-human-docs-for-the-cold-reader</proves>
    [Fact]
    public void LongParagraphAndRawCodeExit1WithTheirOwnCodes()
    {
        using var folder = TestCli.Temp("docs-review-paragraph");
        ReviewTestGit.Init(folder.Path);
        File.WriteAllText(Path.Combine(folder.Path, "guide.md"), "# Guide\n\nStart.\n");
        ReviewTestGit.CommitAll(folder.Path, "Add guide");
        File.WriteAllText(
            Path.Combine(folder.Path, "guide.md"),
            "# Guide\n\nStart.\n\nFirst the approver opens the queue. Then the approver reads the claim. Then the approver checks the receipt. Then the approver clears the claim. Then the approver closes the queue.\n\nA claim without a receipt is refused with receipt-missing.\n");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = DocsReviewCommand.Execute(folder.Path, "HEAD", output, error, out var packet);

        Assert.Equal(1, exit);
        var text = output.ToString();
        Assert.Contains("[paragraph-too-long]", text);
        Assert.Contains("[refusal-code-raw]", text);
    }

    /// <summary>Backticked refusal codes pass: only prose outside code spans counts as raw.</summary>
    /// <proves>litespec/core/writing#write-human-docs-for-the-cold-reader</proves>
    [Fact]
    public void BacktickedRefusalCodePasses()
    {
        var file = WriteTempFile("A claim without a receipt is refused with `receipt-missing`.\n");

        try
        {
            Assert.Empty(DocsProseLints.LintFile(file, "guide.md"));
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>An unknown revision exits 2 and writes no packet.</summary>
    /// <proves>litespec/core/writing#write-human-docs-for-the-cold-reader</proves>
    [Fact]
    public void UnknownRevisionExits2()
    {
        using var folder = TestCli.Temp("docs-review-unknown");
        ReviewTestGit.Init(folder.Path);
        File.WriteAllText(Path.Combine(folder.Path, "guide.md"), "# Guide\n");
        ReviewTestGit.CommitAll(folder.Path, "Add guide");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = DocsReviewCommand.Execute(folder.Path, "no-such-ref", output, error, out var packet);

        Assert.Equal(2, exit);
        Assert.Equal("", packet);
    }

    /// <summary>Writes one Markdown file under the system temporary folder for a direct lint call.</summary>
    /// <param name="text">The file text.</param>
    /// <returns>The absolute file path.</returns>
    static string WriteTempFile(string text)
    {
        var path = Path.Combine(Path.GetTempPath(), $"litespec-lint-{Guid.NewGuid():N}.md");
        File.WriteAllText(path, text);

        return path;
    }
}
