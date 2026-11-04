using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

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
    public void Client_Holds_No_Project_Or_Sample_Strings()
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
    public void Client_Renders_Record_Shapes_With_LiteSpec_Terms()
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
            "Excepted",
            "Because ",
        })
        {
            Assert.Contains(words, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client holds none of the replaced renderings: gap and deviation words, the retries column and the automation handler label.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Holds_None_Of_The_Replaced_Words()
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

    /// <summary>The shortcuts dialog names the four views the keys 1 to 4 switch, Ballots last.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Shortcuts_Name_The_Four_Views()
    {
        Assert.Contains("Outline, Review queue, Security, Ballots", Client(), StringComparison.Ordinal);
    }

    /// <summary>Status and proof labels use LiteSpec words: no verdict or proof phrase outside the model survives in the client.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Labels_Use_LiteSpec_Terms()
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
        })
        {
            Assert.DoesNotContain(purged, script, StringComparison.Ordinal);
        }

        foreach (var words in new[]
        {
            "not implemented",
            "not tested",
            "scenarios not tested",
            "Not implemented or not tested",
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
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Path.Combine(temp.Path, "out"));
        Assert.Equal(0, result.Status);
        return File.ReadAllText(Path.Combine(temp.Path, "out", "assets", "book.js"));
    }
}
