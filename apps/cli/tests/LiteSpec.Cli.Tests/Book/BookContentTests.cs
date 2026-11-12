using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>
///     Proves the book client the CLI writes carries LiteSpec's words and none of the
///     words it replaced. The client is the built Svelte app, so these tests read the
///     strings in its bundle; its behavior is tested in apps/cli/book.
/// </summary>
public sealed class BookContentTests
{
    /// <summary>The client holds no project, path or sample string: everything it shows comes from the shards.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void ClientHoldsNoProjectOrSampleStrings()
    {
        var script = Client();
        foreach (var purged in new[]
        {
            "nighthall",
            "NightHall",
            "Personal-Projects",
            "vscode",
            "Outside this sample",
            "two modules in full",
            "Admission and Orders",
        })
        {
            Assert.DoesNotContain(purged, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client renders value objects, automation shapes and exceptions in LiteSpec words.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void ClientRendersRecordShapesWithLiteSpecTerms()
    {
        var script = Client();
        foreach (var words in new[]
        {
            "authoritative value in",
            "Forwards through",
            "skips and logs",
            "fresh within ",
            ", because ",
            "Skipped when",
            "Runs by hand",
            "Not yet met",
            "Not yet tested",
            "Covered",
            "Because ",
        })
        {
            Assert.Contains(words, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client holds none of the replaced renderings: gap and deviation words, the retries column and the automation handler label.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void ClientHoldsNoneOfTheReplacedWords()
    {
        var script = Client();
        foreach (var removed in new[]
        {
            "known-gap",
            "deviation",
            "Known gap",
            "Accepted gap",
            "Not yet verified",
            "Retries",
            "Reaction handler",
        })
        {
            Assert.DoesNotContain(removed, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The shortcuts dialog names the two views the keys 1 to 2 switch, Security last.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void ShortcutsNameTheTwoViews()
    {
        var script = Client();
        Assert.Contains("Outline, Security", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Review queue", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Ballots", script, StringComparison.Ordinal);
    }

    /// <summary>Completion and proof labels use LiteSpec words: no verdict, approval or proof phrase outside the model survives in the client.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void ClientLabelsUseLiteSpecTerms()
    {
        var script = Client();
        foreach (var purged in new[]
        {
            "changes requested",
            "question asked",
            "approved by you",
            "reviewed by you",
            "test written, not run",
            "test not traced",
            "proved indirectly",
            "scenarios without a test",
            "Has open questions",
            "Download review",
            "Next draft",
            "Auto-approves",
            "Needs an explicit answer",
            "Request changes",
            "Ask a question",
            "approved by",
            "decide by",
            "Approved or reviewed",
            "Held by a draft",
            "Counts as draft",
        })
        {
            Assert.DoesNotContain(purged, script, StringComparison.Ordinal);
        }

        // The writer words each missing mark in lacks ("scenarios not tested" among them),
        // which BookContractTests checks; the client only shows them.
        foreach (var words in new[]
        {
            "not coded",
            "not tested",
            "Not implemented or not tested",
            "Has code",
            "Has tests",
            "scenarios pass",
            "No record with this identifier is in the book",
        })
        {
            Assert.Contains(words, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client script one generated book carries.</summary>
    /// <returns>The text of assets/book.js.</returns>
    static string Client()
    {
        using var temp = TestCli.Temp("book-content");
        BookTestBooks.Write(BookTestBooks.LoadFixture("Book/review.json"), temp.Path, Path.Combine(temp.Path, "out"));
        return File.ReadAllText(Path.Combine(temp.Path, "out", "assets", "book.js"));
    }
}
