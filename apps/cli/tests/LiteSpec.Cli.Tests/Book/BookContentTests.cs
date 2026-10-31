using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the ported client kept the mockup's content contract: a detail renderer for every record type, none of the sample's hardcoded strings, the Ballots tab wired, and the style assets in mockup order.</summary>
public sealed class BookContentTests
{
    /// <summary>The client asset holds a detail renderer key for every record type, and the detail dispatch reads it.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Client_Holds_A_Detail_Renderer_For_Every_Record_Type()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var type in RecordTypes.All.OrderBy(static item => item))
        {
            var literal = type.Contains('-') ? $"  \"{type}\": function" : $"  {type}: function";
            var assigned = type.Contains('-') ? $"DETAIL[\"{type}\"] =" : $"DETAIL.{type} =";
            Assert.True(
                script.Contains(literal, StringComparison.Ordinal) || script.Contains(assigned, StringComparison.Ordinal),
                $"The client holds no detail renderer for record type '{type}'.");
        }

        Assert.Contains("DETAIL[r.type](id, r)", script, StringComparison.Ordinal);
    }

    /// <summary>The client holds none of the sample's hardcoded strings: its product, paths, callouts, label invention and phrase maps are gone, and links read the manifest.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Holds_None_Of_The_Purged_Sample_Strings()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var purged in new[]
        {
            "product/nighthall",
            "nighthall",
            "NightHall",
            "D:/Personal-Projects/LiteSpec",
            "vscode",
            "Outside this sample",
            "outside this sample",
            "two modules in full",
            "Admission and Orders",
            "function hum(",
            "([a-z])([A-Z])",
            "var PHRASE",
            "var PLAIN",
        })
        {
            Assert.DoesNotContain(purged, script, StringComparison.Ordinal);
        }

        // The port reads what the sample hardcoded: the product from the
        // manifest, source roots from the manifest, and labels from the lexicon.
        Assert.Contains("var tvOpen = {};", script, StringComparison.Ordinal);
        Assert.Contains("MAN.roots", script, StringComparison.Ordinal);
        Assert.Contains("litespecRoot()", script, StringComparison.Ordinal);
        Assert.Contains("function fieldLabel", script, StringComparison.Ordinal);
    }

    /// <summary>The client renders each wave-5 value object in LiteSpec words: freshness, paging, compensation, period, released-by, erasure and edge failure.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Renders_Wave5_Value_Objects_With_LiteSpec_Terms()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var renderer in new[]
        {
            "function freshnessNote",
            "function pagingHtml",
            "function compensationHtml",
            "function periodSuffix",
            "function releasedByHtml",
            "function erasureHtml",
            "function edgeFailureText",
        })
        {
            Assert.Contains(renderer, script, StringComparison.Ordinal);
        }

        foreach (var words in new[]
        {
            "authoritative value in",
            "Forwards through",
            "skips and logs",
            "fresh within ",
            ", because ",
        })
        {
            Assert.Contains(words, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client holds none of the wave-5 mini-language strings: every replaced rendering reads the object, and the removed retries column is gone.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Holds_None_Of_The_Wave5_Mini_Language_Strings()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var removed in new[]
        {
            "esc(g.freshness)",
            "esc(f.paging)",
            "esc(s.compensation)",
            "esc(c.period)",
            "inlineRef(f.releasedBy)",
            "esc(f.erasure)",
            "\"On failure: \" + e.onFailure",
            "esc(o.retries)",
            "\"Retries\"",
        })
        {
            Assert.DoesNotContain(removed, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client renders each wave-6 shape in LiteSpec words: automation triggers, saga steps and drivers, typed fields, binds, exercises and background.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Renders_Wave6_Shapes_With_LiteSpec_Terms()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var renderer in new[]
        {
            "function triggeredByHtml",
            "function drivenByBlock",
            "function typedFieldHtml",
            "automation: function",
        })
        {
            Assert.Contains(renderer, script, StringComparison.Ordinal);
        }

        foreach (var words in new[]
        {
            "runs: [\"Runs\"",
            "emits: [\"Emits\"",
            "Skipped when",
            "Runs by hand",
            "blk(\"exer\", \"exercises\"",
            "blk(\"bg\", \"background\"",
            "blk(\"binds\", \"binds\"",
            "c.purpose",
            "f.dataschema",
        })
        {
            Assert.Contains(words, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client holds none of the replaced wave-6 renderings: reaction and job details, old effect verbs, drivers and step whens.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Holds_None_Of_The_Replaced_Wave6_Strings()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var removed in new[]
        {
            "reaction: function",
            "job: function",
            "issues: [\"Issues\"",
            "publishes: [\"Publishes\"",
            "blk(\"on\", \"on\"",
            "blk(\"drv\", \"driver\"",
            "s.when",
            "e[\"if\"]",
            "reaction: \"Reaction handler\"",
            "\"reaction\"",
            "\"job\"",
        })
        {
            Assert.DoesNotContain(removed, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client renders the exception record in LiteSpec words: kind, covers, subject or paths, reason and expiry.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Renders_Exception_With_LiteSpec_Terms()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var words in new[]
        {
            "exception: function",
            "f.expiresOn",
            "f.covers",
            "f.paths",
            "\"Because \" + esc(f.reason)",
            "testedChip",
            "Not yet met",
            "Not yet tested",
            "indirect: \"indirect\"",
            "Excepted",
            "GL.exception",
        })
        {
            Assert.Contains(words, script, StringComparison.Ordinal);
        }
    }

    /// <summary>The client holds none of the replaced wave-7 renderings: known-gap and deviation details, ends-by dates and gap words.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Holds_None_Of_The_Replaced_Wave7_Strings()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var removed in new[]
        {
            "known-gap",
            "deviation",
            "f.endsBy",
            "f.removeWhen",
            "\"Known gap\"",
            "\"Accepted gap\"",
            "Not yet verified",
            "tile(\"verified\"",
        })
        {
            Assert.DoesNotContain(removed, script, StringComparison.Ordinal);
        }
    }

    /// <summary>Research sources with a path render as code text beside the URL rendering, so a path source never throws.</summary>
    /// <proves>litespec/core/model#explain-the-book-from-litespec-definitions</proves>
    [Fact]
    public void Research_Sources_Render_Path_As_Code()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        Assert.Contains("if (x.path)", script, StringComparison.Ordinal);
        Assert.Contains("new URL(x.url)", script, StringComparison.Ordinal);
    }

    /// <summary>The Ballots tab renders the queue's ballots view, answers to the fourth shortcut, and names the four views in the shortcuts dialog.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Ballots_Tab_Is_Wired_To_The_Queue_View()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        Assert.Contains("[\"ballots\", \"Ballots\"", script, StringComparison.Ordinal);
        Assert.Contains("if (view === \"ballots\") return ballotsHtml();", script, StringComparison.Ordinal);
        Assert.Contains("[\"outline\", \"queue\", \"security\", \"ballots\"]", script, StringComparison.Ordinal);
        Assert.Contains("(v === \"queue\" || v === \"security\" || v === \"ballots\")", script, StringComparison.Ordinal);

        var shell = File.ReadAllText(Path.Combine(Out(temp), "index.html"));
        Assert.Contains("Outline, Review queue, Security, Ballots", shell, StringComparison.Ordinal);
    }

    /// <summary>The shell links the fonts first and the two carried stylesheets after them in mockup order.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Shell_Links_The_Style_Assets_In_Mockup_Order()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var shell = File.ReadAllText(Path.Combine(Out(temp), "index.html"));
        var fonts = shell.IndexOf("assets/book-fonts.css", StringComparison.Ordinal);
        var main = shell.IndexOf("assets/book.css", StringComparison.Ordinal);
        var extra = shell.IndexOf("assets/book-extra.css", StringComparison.Ordinal);
        Assert.True(fonts >= 0, "The shell links no fonts stylesheet.");
        Assert.True(main >= 0, "The shell links no main stylesheet.");
        Assert.True(extra >= 0, "The shell links no extra stylesheet.");
        Assert.True(fonts < main, "The fonts stylesheet comes after the main stylesheet.");
        Assert.True(main < extra, "The main stylesheet comes after the extra stylesheet.");
    }

    /// <summary>Status and proof labels use LiteSpec words: no verdict or proof phrase outside the model survives in the client.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Client_Labels_Use_LiteSpec_Terms()
    {
        using var temp = Cli.Temp("book-content");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        foreach (var purged in new[]
        {
            "changes requested",
            "question asked",
            "approved by you",
            "reviewed by you",
            "test written, not run",
            "test not traced",
            "proved indirectly",
            "\"no test\"",
            "\"no code\"",
            "scenarios without a test",
            "Has open questions",
        })
        {
            Assert.DoesNotContain(purged, script, StringComparison.Ordinal);
        }

        foreach (var words in new[]
        {
            "question: \"question\"",
            "changes: \"draft\"",
            "awaiting: \"awaiting\"",
            "unconfirmed: \"unconfirmed\"",
            "unproved: \"unproved\"",
            "norun: \"norun\"",
            "\"not implemented\"",
            "\"not tested\"",
            "scenarios not tested",
            "Not implemented or not tested",
        })
        {
            Assert.Contains(words, script, StringComparison.Ordinal);
        }
    }

    static string Out(TempFolder temp) => Path.Combine(temp.Path, "out");
}
