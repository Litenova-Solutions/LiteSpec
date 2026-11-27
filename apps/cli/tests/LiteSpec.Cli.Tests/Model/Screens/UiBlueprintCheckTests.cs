using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Screens;

/// <summary>
///     The UI blueprint checks: primary actions, screen reachability,
///     wording aliases, refusal sentences, sentence languages and the token limits,
///     each proved on a small model that breaks one rule.
/// </summary>
public sealed class UiBlueprintCheckTests
{
    /// <summary>A screen offering five commands reports no offer-count finding: the four-command cap is reviewed, not checked.</summary>
    /// <proves>litespec/core/ui-ia#cap-each-non-hub-screen-at-four-offered-commands</proves>
    [Fact]
    public void ScreenOfferingFiveCommandsReportsNoOfferCountFinding()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        AddUseCases(model, temp.Path, "borrow-copy", "extend-loan", "return-copy", "place-hold");
        model.Records.Add(Screen(
            "screen/app.loans", "/loans",
            ["use-case/lending.list-loans", "use-case/lending.borrow-copy", "use-case/lending.extend-loan", "use-case/lending.return-copy", "use-case/lending.place-hold"],
            []));

        var findings = Checker.Check(model).Findings;

        // The screen is checked: with no route marking it, screen-unimplemented still fires.
        Assert.Contains(findings, finding => finding.Code == "screen-unimplemented");
        // But the offer count is reviewed, not checked: no finding caps the five offers.
        Assert.DoesNotContain(findings, finding => finding.Message.Contains("4 commands", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, finding => finding.Message.Contains("offer limit", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A primary action naming no offered use case reports screen-primary-action-unknown.</summary>
    /// <proves>litespec/core/ui-ia#declare-each-screen-job-and-archetype</proves>
    [Fact]
    public void PrimaryActionOutsideOffersReportsUnknown()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        AddUseCases(model, temp.Path, "borrow-copy");
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"], [],
            primaryAction: "use-case/lending.borrow-copy"));

        var findings = WithCode(model, "screen-primary-action-unknown");

        Assert.Single(findings);
        Assert.Contains("use-case/lending.borrow-copy", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A primary action naming an offered use case reports no screen-primary-action-unknown.</summary>
    /// <proves>litespec/core/ui-ia#declare-each-screen-job-and-archetype</proves>
    [Fact]
    public void PrimaryActionInOffersReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"], [],
            primaryAction: "use-case/lending.list-loans"));

        Assert.Empty(WithCode(model, "screen-primary-action-unknown"));
    }

    /// <summary>A layer matching the surface it sits on reports design-token-limit-failed naming the layer.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void LayerMatchingItsSurfaceReportsTokenLimitFailed()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "layer-1": {"$value": "#ffffff"},
                "text": {"$value": "#111111"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.Contains(findings, finding => finding.Message.Contains("'layer-1'", StringComparison.Ordinal) && finding.Message.Contains("1.06", StringComparison.Ordinal));
    }

    /// <summary>A layer accent matching a layer reports design-token-limit-failed naming the accent and the layer.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void LayerAccentMatchingALayerReportsTokenLimitFailed()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "layer-1": {"$value": "#000000"},
                "layer-2": {"$value": "#ffffff"},
                "layer-accent": {"$value": "#ffffff"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.Contains(findings, finding => finding.Message.Contains("'layer-accent'", StringComparison.Ordinal) && finding.Message.Contains("'layer-2'", StringComparison.Ordinal));
    }

    /// <summary>Separated layers report no layer separation finding.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void SeparatedLayersReportNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "layer-1": {"$value": "#000000"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.DoesNotContain(findings, finding => finding.Message.Contains("'layer-1' reaches", StringComparison.Ordinal));
    }

    /// <summary>A screen no other screen leads to, that is not its entry point's start, reports screen-unreachable.</summary>
    /// <proves>litespec/core/ui-ia#declare-each-screen-job-and-archetype</proves>
    [Fact]
    public void ScreenLedToByNoOtherScreenReportsUnreachable()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.home", "/", ["use-case/lending.list-loans"], []));
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));

        var findings = WithCode(model, "screen-unreachable");

        Assert.Single(findings);
        Assert.Contains("screen/app.loans", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A screen another screen leads to reports no unreachable finding.</summary>
    /// <proves>litespec/core/ui-ia#declare-each-screen-job-and-archetype</proves>
    [Fact]
    public void ScreenLedToByAnotherScreenReportsNoUnreachable()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.home", "/", ["use-case/lending.list-loans"], [], ["screen/app.loans"]));
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));

        Assert.Empty(WithCode(model, "screen-unreachable"));
    }

    /// <summary>A glossary alias in a result sentence reports wording-alias and names the term to use.</summary>
    /// <proves>litespec/core/domain-model#mark-each-term-with-its-layer</proves>
    [Fact]
    public void AliasInAResultSentenceReportsWordingAlias()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(new Record
        {
            Type = "term",
            Id = "term/borrowing",
            Title = "Borrowing",
            File = Path.Combine(temp.Path, "term.md"),
            Line = 1,
            Raw = JsonDocument.Parse("""{"meaning":"A copy held by one member.","aliases":["Checkout"]}""").RootElement.Clone(),
        });
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","message":"The checkout is refused."}""",
            temp.Path));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("Checkout", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Borrowing", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>An alias inside a longer glossary term in a result sentence reports no wording-alias.</summary>
    /// <proves>litespec/core/domain-model#mark-each-term-with-its-layer</proves>
    [Fact]
    public void AliasInsideALongerTermInASentenceReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","message":"The sales invoice is refused."}""",
            temp.Path));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>A bare alias in a result sentence still reports wording-alias when a longer term holds it.</summary>
    /// <proves>litespec/core/domain-model#mark-each-term-with-its-layer</proves>
    [Fact]
    public void BareAliasInASentenceStillReportsWordingAlias()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","message":"The invoice is refused."}""",
            temp.Path));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("invoice", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Source Document", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>An alias inside a longer glossary label in a result sentence reports no wording-alias.</summary>
    /// <proves>litespec/core/domain-model#mark-each-term-with-its-layer</proves>
    [Fact]
    public void AliasInsideALongerLabelInASentenceReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-ledger", "Sales Ledger", """{"meaning":"A record of sales.","labels":{"en":"sales invoice"}}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","message":"The sales invoice is refused."}""",
            temp.Path));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>A bare alias in a result sentence still reports wording-alias when a longer label holds it.</summary>
    /// <proves>litespec/core/domain-model#mark-each-term-with-its-layer</proves>
    [Fact]
    public void BareAliasInASentenceWithALongerLabelStillReportsWordingAlias()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.Add(Term("term/sales-ledger", "Sales Ledger", """{"meaning":"A record of sales.","labels":{"en":"sales invoice"}}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","message":"The invoice is refused."}""",
            temp.Path));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("invoice", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Source Document", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A token role below its contrast limit reports design-token-limit-failed naming the role.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void TokenRoleBelowItsLimitReportsTokenLimitFailed()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "text": {"$value": "#c8c8c8"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.NotEmpty(findings);
        Assert.Contains(findings, finding => finding.Message.Contains("'text'", StringComparison.Ordinal));
    }

    /// <summary>A written refusal missing a declared language reports sentence-language-missing.</summary>
    /// <proves>litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal</proves>
    [Fact]
    public void RefusalMissingADeclaredLanguageReportsSentenceLanguageMissing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));

        var findings = WithCode(model, "sentence-language-missing");

        Assert.Single(findings);
        Assert.Contains("result/list-loans.refused", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("'nl'", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A written refusal carrying every declared language reports no sentence-language-missing.</summary>
    /// <proves>litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal</proves>
    [Fact]
    public void RefusalCarryingEveryDeclaredLanguageReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","translations":{"nl":"De lijst is geweigerd."}}""",
            temp.Path));

        Assert.Empty(WithCode(model, "sentence-language-missing"));
    }

    /// <summary>The default language needs no sentences entry.</summary>
    /// <proves>litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal</proves>
    [Fact]
    public void DefaultLanguageNeedsNoSentencesEntry()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en"]}""", temp.Path));

        Assert.Empty(WithCode(model, "sentence-language-missing"));
    }

    /// <summary>A derived refusal without sentences reports no sentence-language-missing.</summary>
    /// <proves>litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal</proves>
    [Fact]
    public void DerivedRefusalWithoutSentencesReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        using var document = JsonDocument.Parse("""{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused"}""");
        model.Records.Add(new Record
        {
            Type = "result",
            Id = "result/list-loans.refused",
            Title = "result/list-loans.refused",
            File = Path.Combine(temp.Path, "result.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
            Derived = true,
        });

        Assert.Empty(WithCode(model, "sentence-language-missing"));
    }

    /// <summary>A use case reached through entry points declaring two languages reports one finding naming each missing sentence.</summary>
    /// <proves>litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal</proves>
    [Fact]
    public void TwoEntryPointsRequireEachDeclaredLanguage()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.Add(Rec("entry-point", "entry-point/kiosk", """{"kind":"browser","languages":["en","fr"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "use-case/lending.list-loans");
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","via":["entry-point/app","entry-point/kiosk"]}""", temp.Path));

        var finding = Assert.Single(WithCode(model, "sentence-language-missing"));

        Assert.Contains("'nl'", finding.Message, StringComparison.Ordinal);
        Assert.Contains("'fr'", finding.Message, StringComparison.Ordinal);
    }

    static List<Finding> WithCode(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(finding => finding.Code == code).ToList();

    /// <summary>Adds command use cases of the lending module reached through the app entry point.</summary>
    /// <param name="model">The model holding the records.</param>
    /// <param name="root">The consumer root the record files sit under.</param>
    /// <param name="names">The use-case names to add.</param>
    static void AddUseCases(ProjectModel model, string root, params string[] names)
    {
        foreach (var name in names)
        {
            model.Records.Add(Rec("use-case", $"use-case/lending.{name}", """{"kind":"command","via":["entry-point/app"]}""", root));
        }
    }

    /// <summary>Writes a project whose UI package names a design contract with a token file.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="tokens">The token file text, written beside the design contract.</param>
    static void WriteDesignContract(string root, string tokens)
    {
        var package = Path.Combine(root, "packages", "ui");
        Directory.CreateDirectory(Path.Combine(package, "tokens"));
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(Path.Combine(package, "tokens", "fixture.tokens.json"), tokens);
        File.WriteAllText(
            Path.Combine(package, "DESIGN.md"),
            """
            ---
            schemaVersion: 2
            package: "@fixture/ui"
            tokens: tokens/fixture.tokens.json
            pageFrames:
              list-page: ListPage
            ---

            # Fixture design contract
            """);
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" }
                ],
              "ui": { "package": { "designContract": "packages/ui/DESIGN.md" } },
              "paths": {}
            }
            """);
    }

    static ProjectModel BaseModel(string root)
    {
        if (!File.Exists(Path.Combine(root, ".litespec", "project.yaml")))
        {
            Directory.CreateDirectory(Path.Combine(root, ".litespec"));
            File.WriteAllText(
                Path.Combine(root, ".litespec", "project.yaml"),
                """
                {
                  "owners": ["lending-team"],
                  "frontends": [
                      { "name": "web", "path": "apps/web", "platform": "svelte" }
                    ], "paths": {
                  }
                }
                """);
        }

        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser"}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","via":["entry-point/app"]}""", root));
        model.Records.Add(Rec("result", "result/list-loans.listed", """{"outcome":"success","meaning":"The loans are listed."}""", root));
        model.Records.Add(Rec("result", "result/list-loans.refused", """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused"}""", root));

        return model;
    }

    static Record Screen(string id, string route, IReadOnlyList<string> offers, IReadOnlyList<string> serves, IReadOnlyList<string>? leadsTo = null, string? primaryAction = null) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            File = "screen.md",
            Line = 1,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":\"entry-point/app\",\"route\":" + JsonSerializer.Serialize(route)
                + ",\"offers\":" + JsonSerializer.Serialize(offers)
                + ",\"serves\":" + JsonSerializer.Serialize(serves)
                + ",\"leadsTo\":" + JsonSerializer.Serialize(leadsTo ?? [])
                + ",\"job\":\"The reader lists loans and opens one to read it.\""
                + ",\"archetype\":\"ListPage\""
                + PrimaryActionJson(primaryAction) + "}").RootElement.Clone(),
        };

    static string PrimaryActionJson(string? primary) =>
        primary is null ? "" : ",\"primaryAction\":" + JsonSerializer.Serialize(primary);

    static Record Rec(string type, string id, string fields, string root)
    {
        using var document = JsonDocument.Parse(fields);

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = Path.Combine(root, $"{type}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }

    static Record Term(string id, string title, string fields, string root)
    {
        using var document = JsonDocument.Parse(fields);

        return new Record
        {
            Type = "term",
            Id = id,
            Title = title,
            File = Path.Combine(root, $"{id.Replace('/', '-')}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
