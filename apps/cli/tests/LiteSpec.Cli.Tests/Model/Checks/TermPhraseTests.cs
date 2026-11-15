using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the term-missing phrase extraction skips sentence breaks, codes, paths, tables, section names, openers, months, currency codes, acronym pairs, cast names and wireframes against the large fixture.</summary>
public sealed class TermPhraseTests
{
    /// <summary>A phrase spanning a sentence stop, a colon or a semicolon is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void SentenceBreakPhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            Mara visited Berlin. The archive opens late.
            The rate includes VAT. The receipt shows it.
            The fee is 25 EUR. VAT applies to every receipt.
            filed In January. Sanne approved the refund.
            The ledger closed in January. The audit starts soon.
            Arrivals peak in November. This strains the desk.
            They flew past the US. AI tools tracked the bags.
            Note: The archive opens late.
            Paris; The desk reopens soon.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Berlin The'", result.Output);
        Assert.DoesNotContain("'VAT The'", result.Output);
        Assert.DoesNotContain("'EUR VAT'", result.Output);
        Assert.DoesNotContain("'In January Sanne'", result.Output);
        Assert.DoesNotContain("'January The'", result.Output);
        Assert.DoesNotContain("'November This'", result.Output);
        Assert.DoesNotContain("'US AI'", result.Output);
        Assert.DoesNotContain("'Note The'", result.Output);
        Assert.DoesNotContain("'Paris The'", result.Output);
    }

    /// <summary>A phrase spanning a line break inside a record field is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void LineBreakPhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");

        foreach (var name in new[] { "courier-one", "courier-two", "courier-three" })
        {
            TestRecords.Write(
                fixture.Path,
                $".litespec/intent/actors/{name}.yaml",
                $$"""
                {
                  "type": "actor",
                  "id": "actor/{{name}}",
                  "title": "Courier {{name}}",
                  "summary": "Mara visited Berlin\nThe archive opens late.",
                  "kind": "person",
                  "belongsToTeam": false,
                  "holds": ["role/librarian"]
                }
                """);
        }

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Berlin The'", result.Output);
    }

    /// <summary>A duration code split into letters is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void DurationCodeIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            The retry waits PT5S between tries and holds state PT5M, with retention P7Y for reports and P1D for drafts.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'PT S'", result.Output);
        Assert.DoesNotContain("'PT M'", result.Output);
        Assert.DoesNotContain("'P Y'", result.Output);
        Assert.DoesNotContain("'P D'", result.Output);
    }

    /// <summary>A time zone name split at its slash is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void TimeZoneNameIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            The window opens at nine Europe/Amsterdam time.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Europe Amsterdam'", result.Output);
    }

    /// <summary>A Markdown table header row is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void TableHeaderRowIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            | Option | Gives | Costs |
            | --- | --- | --- |
            | Card | Entry | Nothing |
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Option Gives Costs'", result.Output);
    }

    /// <summary>An explanation section name in prose is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void SectionNameIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            The team logs Open Questions each week.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Open Questions'", result.Output);
    }

    /// <summary>A phrase opening with a sentence opener or function word is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void SentenceOpenerPhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            When Archive Annex opens the team reviews it.
            Otherwise Archive Annex stays closed.
            With Archive Annex open the queue moves.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'When Archive Annex'", result.Output);
        Assert.DoesNotContain("'Otherwise Archive Annex'", result.Output);
        Assert.DoesNotContain("'With Archive Annex'", result.Output);
    }

    /// <summary>A phrase holding a month name is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void MonthPhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            The team reviews Archive January Review each quarter.
            The team reviews January Archive Review each quarter.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Archive January Review'", result.Output);
        Assert.DoesNotContain("'January Archive Review'", result.Output);
    }

    /// <summary>A phrase holding a currency code is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void CurrencyPhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            The team reviews Archive EUR Review each quarter.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Archive EUR Review'", result.Output);
    }

    /// <summary>A phrase holding two all-caps acronyms is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void AcronymPairPhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            The team reviews US AI Review each quarter.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'US AI Review'", result.Output);
    }

    /// <summary>A phrase holding a code-formatted cast name is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void CastCodeNamePhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        WriteCast(fixture.Path, """
            ---
            type: example-cast
            title: Example Cast
            ---

            # Example Cast

            The team uses `Marina` for expenses.
            """);
        ExplainShared(fixture.Path, """
            # Record

            The team reviews Marina Archive Review each quarter.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Marina Archive Review'", result.Output);
    }

    /// <summary>A phrase holding a table cast name is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void CastTableValuePhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        WriteCast(fixture.Path, """
            ---
            type: example-cast
            title: Example Cast
            ---

            # Example Cast

            | Person | Actor | What they do here |
            |:---|:---|:---|
            | Marina | actor/marina | Files expenses. |
            """);
        ExplainShared(fixture.Path, """
            # Record

            The team reviews Marina Archive Review each quarter.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Marina Archive Review'", result.Output);
    }

    /// <summary>A phrase holding a two-word cast name inside a longer phrase is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void CastNameInsideLongerPhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        WriteCast(fixture.Path, """
            ---
            type: example-cast
            title: Example Cast
            ---

            # Example Cast

            The team uses `Harbor Pine` for expenses.
            """);
        ExplainShared(fixture.Path, """
            # Record

            The team reviews Harbor Pine Archive each quarter.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Harbor Pine Archive'", result.Output);
    }

    /// <summary>A phrase inside a wireframe block is no recurring phrase.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void WireframePhraseIsNotReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            ```wireframe
            page
              header
                heading "Archive Annex"
            ```
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.DoesNotContain("'Archive Annex'", result.Output);
    }

    /// <summary>A real repeated multi-word term with no glossary entry is still reported.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void RepeatedTermIsStillReported()
    {
        using var fixture = ModelFixture.Prepare("large");
        ExplainShared(fixture.Path, """
            # Record

            The team reviews Archive Annex each quarter.
            """);

        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");

        Assert.Contains("term-missing", result.Output);
        Assert.Contains("'Archive Annex'", result.Output);
    }

    /// <summary>Writes the same explanation body to the three shared records.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="markdown">The explanation Markdown.</param>
    static void ExplainShared(string root, string markdown)
    {
        TestRecords.Explain(root, ".litespec/intent/actors/librarian/librarian.md", markdown);
        TestRecords.Explain(root, ".litespec/intent/actors/reader/reader.md", markdown);
        TestRecords.Explain(root, ".litespec/intent/problems/overdue-loans-pile-up/overdue-loans-pile-up.md", markdown);
    }

    /// <summary>Writes the example cast file the term check reads for cast names.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="markdown">The example cast Markdown.</param>
    static void WriteCast(string root, string markdown)
    {
        var path = Path.Combine(root, ".litespec", "example-cast.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, markdown);
    }
}
