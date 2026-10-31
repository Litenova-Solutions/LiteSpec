using LiteSpec.Book;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book lexicon reads LiteSpec's own words: link phrases, type questions, layers, fields, glossary, sections and findings, and fails naming the file.</summary>
public sealed class BookLexiconTests
{
    /// <summary>Each link reads in both directions, in the Links table's own phrases.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Links_Read_In_Both_Directions()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var links = lexicon.ToJson()["links"]!.AsArray();
        var solves = links.Single(node => node!.AsObject()["field"]!.GetValue<string>() == "solves")!.AsObject();
        Assert.Equal("solves", solves["forward"]!.GetValue<string>());
        Assert.Equal("is solved by", solves["inverse"]!.GetValue<string>());
        Assert.Equal(
            new List<string> { "feature" },
            solves["carriers"]!.AsArray().Select(node => node!.GetValue<string>()).ToList());
    }

    /// <summary>An aggregate transition mover reads as its transitions row, with or without the leading record type.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Transitions_By_Resolves_To_Its_Row()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));

        Assert.True(lexicon.TryPhrase("transitions.by", "aggregate", out var forward, out var inverse));
        Assert.Equal("is moved by", forward);
        Assert.Equal("moves", inverse);

        Assert.True(lexicon.TryPhrase("aggregate.transitions.by", "aggregate", out forward, out inverse));
        Assert.Equal("is moved by", forward);
        Assert.Equal("moves", inverse);
    }

    /// <summary>A nested effects path reads as its effects row; a leading record type reads the same; an unknown nested path reads nothing.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Nested_Effects_Calls_Resolves_To_Effects_Row()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));

        Assert.True(lexicon.TryPhrase("effects.calls", "use-case", out var forward, out var inverse));
        Assert.Equal("effects", forward);
        Assert.Equal("is effected by", inverse);

        Assert.True(lexicon.TryPhrase("use-case.effects.sends", "use-case", out forward, out inverse));
        Assert.Equal("effects", forward);
        Assert.Equal("is effected by", inverse);

        Assert.True(lexicon.TryPhrase("access.grant", "use-case", out forward, out inverse));
        Assert.Equal("is granted through", forward);
        Assert.Equal("grants", inverse);

        Assert.False(lexicon.TryPhrase("except.fallback", "requirement", out _, out _));
    }

    /// <summary>The actor row's two readings split by carrier: a journey is walked by, an external system acts as.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Actor_Row_Splits_By_Carrier()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));

        Assert.True(lexicon.TryPhrase("actor", "journey", out var forward, out var inverse));
        Assert.Equal("is walked by", forward);
        Assert.Equal("walks", inverse);

        Assert.True(lexicon.TryPhrase("actor", "external-system", out forward, out inverse));
        Assert.Equal("acts as", forward);
        Assert.Equal("is acted as by", inverse);
    }

    /// <summary>Rows sharing one field stay apart by carrier: each serves row keeps its own carriers.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Serves_Rows_Stay_Apart_By_Carrier()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var serves = lexicon.Links.Where(link => link.Field == "serves").ToList();
        Assert.Equal(2, serves.Count);
        Assert.Equal(new List<string> { "use-case", "reaction", "job" }, serves[0].Carriers.ToList());
        Assert.Equal(new List<string> { "journey" }, serves[1].Carriers.ToList());

        Assert.True(lexicon.TryPhrase("serves", "journey", out var forward, out var inverse));
        Assert.Equal("serves", forward);
        Assert.Equal("is served by", inverse);
    }

    /// <summary>The terms row widens to every record type, since any record may use a term.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Terms_Row_Covers_Every_Record_Type()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var terms = lexicon.Links.Single(link => link.Field == "terms");
        Assert.Equal(RecordTypes.All.Count, terms.Carriers.Count);
        Assert.Contains("product", terms.Carriers);
        Assert.Contains("use-case", terms.Carriers);
        Assert.Contains("term", terms.Carriers);
        Assert.Equal(
            terms.Carriers.OrderBy(carrier => carrier, StringComparer.Ordinal).ToList(),
            terms.Carriers.ToList());
    }

    /// <summary>Each type carries its label, plural, layer and question; research stays whole in the plural.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Types_Carry_Label_Plural_Layer_And_Question()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var types = lexicon.ToJson()["types"]!.AsObject();

        var useCase = types["use-case"]!.AsObject();
        Assert.Equal("Use case", useCase["label"]!.GetValue<string>());
        Assert.Equal("Use cases", useCase["plural"]!.GetValue<string>());
        Assert.Equal("behavior", useCase["layer"]!.GetValue<string>());
        Assert.Equal("Which use cases exist?", useCase["question"]!.GetValue<string>());

        Assert.Equal("Research", types["research"]!.AsObject()["plural"]!.GetValue<string>());
        Assert.Equal("Journeys", types["journey"]!.AsObject()["plural"]!.GetValue<string>());
        Assert.Equal("External systems", types["external-system"]!.AsObject()["plural"]!.GetValue<string>());
    }

    /// <summary>Each layer carries its writers and reviewers; governance and glossary read beside the four rows with none.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Layers_Carry_Writes_And_Reviews()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var layers = lexicon.ToJson()["layers"]!.AsObject();

        var intent = layers["intent"]!.AsObject();
        Assert.Equal("Intent", intent["label"]!.GetValue<string>());
        Assert.Equal("agents draft", intent["writes"]!.GetValue<string>());
        Assert.Equal("the founder", intent["reviews"]!.GetValue<string>());

        var governance = layers["governance"]!.AsObject();
        Assert.Equal("Governance", governance["label"]!.GetValue<string>());
        Assert.Equal("", governance["writes"]!.GetValue<string>());
        Assert.Equal("", governance["reviews"]!.GetValue<string>());
        Assert.Equal("Glossary", layers["glossary"]!.AsObject()["label"]!.GetValue<string>());
    }

    /// <summary>Each field reads under its label, with the renamed fields under their new names.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Fields_Label_By_Field_Name()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));

        Assert.Equal("Finished when", lexicon.Fields["finishedWhen"]);
        Assert.Equal("Lifecycle", lexicon.Fields["lifecycle"]);
        Assert.Equal("Due by", lexicon.Fields["dueBy"]);
        Assert.Equal("Planned for", lexicon.Fields["plannedFor"]);
        Assert.Equal("Choice", lexicon.Fields["choice"]);
        Assert.Equal("Reopened when", lexicon.Fields["reopenedWhen"]);
        Assert.Equal("Pros", lexicon.Fields["pros"]);
        Assert.Equal("Cons", lexicon.Fields["cons"]);
        Assert.Equal("Exclusions", lexicon.Fields["exclusions"]);
        Assert.Equal("Addresses", lexicon.Fields["addresses"]);
        Assert.Equal("Recommendation", lexicon.Fields["recommendation"]);
        Assert.Equal("Condition", lexicon.Fields["condition"]);
        Assert.Equal("Read on", lexicon.Fields["readOn"]);
        Assert.Equal("Authority", lexicon.Fields["authority"]);
        Assert.Equal("Conclusion", lexicon.Fields["conclusion"]);
        Assert.Equal("Options", lexicon.Fields["options"]);

        Assert.False(lexicon.Fields.ContainsKey("by"));
        Assert.False(lexicon.Fields.ContainsKey("state"));
        Assert.False(lexicon.Fields.ContainsKey("goal"));
        Assert.False(lexicon.Fields.ContainsKey("decided"));
        Assert.False(lexicon.Fields.ContainsKey("category"));
        Assert.False(lexicon.Fields.ContainsKey("decidedOn"));
        Assert.False(lexicon.Fields.ContainsKey("chosen"));
        Assert.False(lexicon.Fields.ContainsKey("changeWhen"));
        Assert.False(lexicon.Fields.ContainsKey("neverDoes"));
        Assert.False(lexicon.Fields.ContainsKey("audience"));
        Assert.False(lexicon.Fields.ContainsKey("aim"));
    }

    /// <summary>Each glossary term carries its definition, layer and source; aliases and rejected words stay out.</summary>
    /// <proves>litespec/core/model#explain-the-book-from-litespec-definitions</proves>
    [Fact]
    public void Glossary_Splits_Definition_Layer_And_Source()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var glossary = lexicon.ToJson()["glossary"]!.AsObject();
        Assert.Equal(3, glossary.Count);

        var expense = glossary["expense"]!.AsObject();
        Assert.Equal("Expense", expense["term"]!.GetValue<string>());
        Assert.Equal("One cost a person claims back.", expense["definition"]!.GetValue<string>());
        Assert.Equal("Intent", expense["layer"]!.GetValue<string>());
        Assert.Equal("plain English", expense["source"]!.GetValue<string>());

        var undo = glossary["undo"]!.AsObject();
        Assert.Equal("The per-step reversal of a process.", undo["definition"]!.GetValue<string>());
        Assert.DoesNotContain("compensation", undo["definition"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.DoesNotContain("rollback", undo["definition"]!.GetValue<string>(), StringComparison.Ordinal);

        var host = glossary["apphost"]!.AsObject();
        Assert.Null(host["layer"]);
        Assert.Equal(".NET Aspire", host["source"]!.GetValue<string>());
    }

    /// <summary>Each named section carries its first paragraph and names its LiteSpec source.</summary>
    /// <proves>litespec/core/model#explain-the-book-from-litespec-definitions</proves>
    [Fact]
    public void Sections_Carry_First_Paragraph_And_Source()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var sections = lexicon.ToJson()["sections"]!.AsObject();
        Assert.Equal(6, sections.Count);

        var statuses = sections["Statuses"]!.AsObject();
        Assert.Equal("Draft means the record awaits its decider.", statuses["text"]!.GetValue<string>());
        Assert.Equal("model.md, Statuses", statuses["source"]!.GetValue<string>());

        var ballots = sections["Questions and Ballots"]!.AsObject();
        Assert.Equal("Open questions block approval.", ballots["text"]!.GetValue<string>());
        Assert.Equal("model.md, Questions and Ballots", ballots["source"]!.GetValue<string>());
    }

    /// <summary>Each finding code carries its tier and meaning; the reason-code table is not a finding table.</summary>
    /// <proves>litespec/core/model#explain-the-book-from-litespec-definitions</proves>
    [Fact]
    public void Findings_Carry_Tier_And_Meaning()
    {
        using var temp = Cli.Temp("book-lexicon");
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), ToolsPage(), GlossaryPage()));
        var findings = lexicon.ToJson()["findings"]!.AsObject();
        Assert.Equal(2, findings.Count);

        var shape = findings["record-shape"]!.AsObject();
        Assert.Equal("G", shape["tier"]!.GetValue<string>());
        Assert.Equal("A record fails the shape its type requires.", shape["meaning"]!.GetValue<string>());
        Assert.False(findings.ContainsKey("requirement-removed"));
    }

    /// <summary>The finding table reads by column name, so an added Fix column never moves it.</summary>
    /// <proves>litespec/core/model#explain-the-book-from-litespec-definitions</proves>
    [Fact]
    public void Findings_Read_By_Column_Name_With_Extra_Columns()
    {
        using var temp = Cli.Temp("book-lexicon");
        var tools = ToolsPage()
            .Replace("| Code | Level | Meaning |", "| Code | Level | Meaning | Fix |", StringComparison.Ordinal)
            .Replace("|:---|:---|:---|", "|:---|:---|:---|:---|", StringComparison.Ordinal)
            .Replace(
                "| `record-shape` | G | A record fails the shape its type requires. |",
                "| `record-shape` | G | A record fails the shape its type requires. | Write the missing shape. |",
                StringComparison.Ordinal)
            .Replace(
                "| `term-lookalike` | A | Two terms differ only in spelling. |",
                "| `term-lookalike` | A | Two terms differ only in spelling. | Rename one term. |",
                StringComparison.Ordinal);
        var lexicon = BookLexicon.Load(WriteHome(temp, ModelPage(), tools, GlossaryPage()));
        var findings = lexicon.ToJson()["findings"]!.AsObject();
        Assert.Equal(2, findings.Count);

        var shape = findings["record-shape"]!.AsObject();
        Assert.Equal("G", shape["tier"]!.GetValue<string>());
        Assert.Equal("A record fails the shape its type requires.", shape["meaning"]!.GetValue<string>());
        Assert.False(findings.ContainsKey("requirement-removed"));
    }

    /// <summary>A page without its Links section fails, naming the page.</summary>
    /// <proves>litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it</proves>
    [Fact]
    public void Missing_Links_Table_Fails_Naming_The_File()
    {
        using var temp = Cli.Temp("book-lexicon");
        var model = ModelPage().Replace("### Links", "### Gone", StringComparison.Ordinal);
        var home = WriteHome(temp, model, ToolsPage(), GlossaryPage());
        var modelFile = Path.Combine(home, "docs", "core", "model.md");
        var exception = Assert.Throws<BookLexiconException>(() => BookLexicon.Load(home));
        Assert.Equal(modelFile, exception.File);
    }

    /// <summary>A Links table in the old three-column shape fails, naming the page.</summary>
    /// <proves>litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it</proves>
    [Fact]
    public void Old_Links_Shape_Fails_Naming_The_File()
    {
        using var temp = Cli.Temp("book-lexicon");
        var model = ModelPage()
            .Replace(
                "| Field | Written on | Points at | Reads as | Read back as |",
                "| Field | Written on | Points at |",
                StringComparison.Ordinal)
            .Replace("|:---|:---|:---|:---|:---|", "|:---|:---|:---|", StringComparison.Ordinal);
        var home = WriteHome(temp, model, ToolsPage(), GlossaryPage());
        var modelFile = Path.Combine(home, "docs", "core", "model.md");
        var exception = Assert.Throws<BookLexiconException>(() => BookLexicon.Load(home));
        Assert.Equal(modelFile, exception.File);
    }

    /// <summary>A missing carried page fails, naming the page.</summary>
    /// <proves>litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it</proves>
    [Fact]
    public void Missing_Page_Fails_Naming_The_File()
    {
        using var temp = Cli.Temp("book-lexicon");
        var home = WriteHome(temp, ModelPage(), ToolsPage(), null);
        var glossaryFile = Path.Combine(home, "docs", "reference", "glossary.md");
        var exception = Assert.Throws<BookLexiconException>(() => BookLexicon.Load(home));
        Assert.Equal(glossaryFile, exception.File);
    }

    static string WriteHome(TempFolder temp, string? model, string? tools, string? glossary)
    {
        WriteFile(temp, ["docs", "core", "model.md"], model);
        WriteFile(temp, ["docs", "tools", "model.md"], tools);
        WriteFile(temp, ["docs", "reference", "glossary.md"], glossary);
        return temp.Path;
    }

    static void WriteFile(TempFolder temp, string[] segments, string? content)
    {
        if (content is null)
        {
            return;
        }

        var path = temp.Path;
        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    static string ModelPage() =>
        """
        # Project Model

        ## Concepts

        ### The Four Record Layers

        Each record layer answers one question.

        | Record layer | Question | Writes | Reviews | Records |
        |:--|:--|:--|:--|:--|
        | Intent | Why build it? | agents draft | the founder | product, goal |
        | Behavior | How does it behave? | agents | developers | module, use-case |
        | Implementation | How is it built? | agents | checks | code only |
        | Proof | How do we know? | the builder | checks | tests |

        Governance records attach to any record layer.

        ### Record Types and Page Styles

        Each type answers one question.

        | Type | Record layer | Question |
        |:--|:--|:--|
        | `product` | intent | What is built? |
        | `goal` | intent | Which outcome is served? |
        | `feature` | intent | What is offered? |
        | `research` | intent | Which facts inform a choice? |
        | `requirement` | intent | What does the product guarantee? |
        | `journey` | intent | Which steps reach one goal? |
        | `use-case` | behavior | Which use cases exist? |
        | `reaction` | behavior | What follows an event? |
        | `job` | behavior | Which work runs on a schedule? |
        | `screen` | behavior | Which page does a person see? |
        | `external-system` | behavior | Which outside system exchanges what? |

        ### Links

        Each written link lives as a field on the record it starts from.

        | Field | Written on | Points at | Reads as | Read back as |
        |:---|:---|:---|:---|:---|
        | `solves` | feature | Problems | solves | is solved by |
        | `serves` | use case, reaction, job | Journey steps | serves | is served by |
        | `serves` | journey | The goal it walks toward | serves | is served by |
        | `effects` | use case, reaction, job | Use cases, notifications, integration events, operations | effects | is effected by |
        | `transitions.by` | aggregate | The use case that moves the transition | is moved by | moves |
        | `access.grant` | use case | A permission | is granted through | grants |
        | `actor` | journey, external system | The actor walking; the actor acted as | is walked by / acts as | walks / is acted as by |
        | `terms` | any record | Terms | uses | is used by |

        ### Statuses

        Draft means the record awaits its decider.

        Approved means it holds.

        ### Approval as Data

        An approved record carries its approval as data.

        ### Risk Tiers

        Any risk value makes the record tier 1.

        ### Check Levels

        Every check has a tier.

        ### Transcription Mode

        A Behavior record written from code carries transcribed source.

        ### Questions and Ballots

        Open questions block approval.
        """;

    static string ToolsPage() =>
        """
        # Model Command

        ## Description

        A change breaks approved intent in these ways:

        | Code | Meaning |
        |:---|:---|
        | `requirement-removed` | An approved requirement no longer exists. |

        The finding codes are:

        | Code | Level | Meaning |
        |:---|:---|:---|
        | `record-shape` | G | A record fails the shape its type requires. |
        | `term-lookalike` | A | Two terms differ only in spelling. |
        """;

    static string GlossaryPage() =>
        """
        # Glossary

        ## Product and Domain Terms

        These terms name the records a specification is written in.

        ### Expense

        One cost a person claims back.

        Layer: Intent.

        Source: plain English.

        ### Undo

        The per-step reversal of a process.

        Layer: Behavior.

        Aliases: compensation.

        Source: LiteSpec.

        Rejected: rollback, which names the mechanism.

        ### AppHost

        The local orchestrator that starts every application together.

        Source: .NET Aspire.
        """;
}
