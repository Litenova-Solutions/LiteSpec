using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Screens;

/// <summary>
///     The UI blueprint checks: wireframe references, screen reachability, wording aliases,
///     refusal sentences, sentence languages and the token limits, each proved on a small
///     model that breaks one rule.
/// </summary>
public sealed class UiBlueprintCheckTests
{
    /// <summary>A wireframe action that names no use case the screen offers reports wireframe-reference-unknown.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeActionNamingAnUnofferedUseCaseReportsReferenceUnknown()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                action primary "Approve loan"
            ```
            """));

        var findings = WithCode(model, "wireframe-reference-unknown");

        Assert.Single(findings);
        Assert.Contains("Approve loan", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A wireframe action that names an offered use case, matched case-insensitively, reports nothing.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeActionNamingAnOfferedUseCaseReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                action primary "list loans"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-reference-unknown"));
    }

    /// <summary>A screen no other screen leads to, that is not its entry point's start, reports screen-unreachable.</summary>
    /// <proves>litespec/frontend/ui#lead-each-screen-to-the-screens-a-person-reaches-next</proves>
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
    /// <proves>litespec/frontend/ui#lead-each-screen-to-the-screens-a-person-reaches-next</proves>
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
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
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
            """{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused","sentence":"The checkout is refused."}""",
            temp.Path));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("Checkout", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Borrowing", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>An alias inside a longer glossary term in a result sentence reports no wording-alias.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
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
            """{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused","sentence":"The sales invoice is refused."}""",
            temp.Path));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>A bare alias in a result sentence still reports wording-alias when a longer term holds it.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
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
            """{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused","sentence":"The invoice is refused."}""",
            temp.Path));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("invoice", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Source Document", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>An alias inside a longer glossary label in a result sentence reports no wording-alias.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
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
            """{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused","sentence":"The sales invoice is refused."}""",
            temp.Path));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>An alias inside a longer glossary term in a wireframe reports no wording-alias.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void AliasInsideALongerTermInAWireframeReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Sales invoice"
            ```
            """));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>A bare alias in a wireframe still reports wording-alias when a longer term holds it.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void BareAliasInAWireframeStillReportsWordingAlias()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Invoice"
            ```
            """));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("invoice", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Source Document", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A token role below its contrast limit reports design-token-limit-failed naming the role.</summary>
    /// <proves>litespec/frontend/ui#meet-the-limits-of-every-token-role</proves>
    [Fact]
    public void TokenRoleBelowItsLimitReportsTokenLimitFailed()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, "{\"light\":{\"page\":{\"$value\":\"#ffffff\"},\"text\":{\"$value\":\"#c8c8c8\"}}}");
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.NotEmpty(findings);
        Assert.Contains(findings, finding => finding.Message.Contains("'text'", StringComparison.Ordinal));
    }

    /// <summary>A written refusal missing a declared language reports sentence-language-missing.</summary>
    /// <proves>litespec/frontend/ui#format-money-and-moments-from-the-readers-language-and-the-businesss-country</proves>
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
    /// <proves>litespec/frontend/ui#format-money-and-moments-from-the-readers-language-and-the-businesss-country</proves>
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
            """{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused","sentences":{"nl":"De lijst is geweigerd."}}""",
            temp.Path));

        Assert.Empty(WithCode(model, "sentence-language-missing"));
    }

    /// <summary>The default language needs no sentences entry.</summary>
    /// <proves>litespec/frontend/ui#format-money-and-moments-from-the-readers-language-and-the-businesss-country</proves>
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
    /// <proves>litespec/frontend/ui#format-money-and-moments-from-the-readers-language-and-the-businesss-country</proves>
    [Fact]
    public void DerivedRefusalWithoutSentencesReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        using var document = JsonDocument.Parse("""{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused"}""");
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

    /// <summary>A use case reached through entry points declaring two languages needs a sentence for each.</summary>
    /// <proves>litespec/frontend/ui#format-money-and-moments-from-the-readers-language-and-the-businesss-country</proves>
    [Fact]
    public void TwoEntryPointsRequireEachDeclaredLanguage()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.Add(Rec("entry-point", "entry-point/kiosk", """{"kind":"browser","languages":["en","fr"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "use-case/lending.list-loans");
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","reachedThrough":["entry-point/app","entry-point/kiosk"]}""", temp.Path));

        var findings = WithCode(model, "sentence-language-missing");

        Assert.Equal(2, findings.Count);
        Assert.Contains(findings, finding => finding.Message.Contains("'nl'", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Message.Contains("'fr'", StringComparison.Ordinal));
    }

    static List<Finding> WithCode(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(finding => finding.Code == code).ToList();

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
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","reachedThrough":["entry-point/app"]}""", root));
        model.Records.Add(Rec("result", "result/list-loans.listed", """{"outcome":"success","meaning":"The loans are listed."}""", root));
        model.Records.Add(Rec("result", "result/list-loans.refused", """{"outcome":"refusal","meaning":"The listing is refused.","code":"lending.refused"}""", root));

        return model;
    }

    static Record Screen(string id, string address, IReadOnlyList<string> offers, IReadOnlyList<string> serves, IReadOnlyList<string>? leadsTo = null) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            File = "screen.md",
            Line = 1,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":\"entry-point/app\",\"address\":" + JsonSerializer.Serialize(address)
                + ",\"offers\":" + JsonSerializer.Serialize(offers)
                + ",\"serves\":" + JsonSerializer.Serialize(serves)
                + ",\"leadsTo\":" + JsonSerializer.Serialize(leadsTo ?? []) + "}").RootElement.Clone(),
        };

    static Record ScreenWithExplanation(string root, string id, string address, IReadOnlyList<string> offers, string explanation) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            File = Path.Combine(root, "screen.yaml"),
            Line = 1,
            ExplanationFile = Path.Combine(root, "screen.md"),
            Explanation = explanation,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":\"entry-point/app\",\"address\":" + JsonSerializer.Serialize(address)
                + ",\"offers\":" + JsonSerializer.Serialize(offers)
                + ",\"serves\":[],\"leadsTo\":[]}").RootElement.Clone(),
        };

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
