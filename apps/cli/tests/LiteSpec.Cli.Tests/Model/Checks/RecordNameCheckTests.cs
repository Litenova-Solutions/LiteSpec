using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the record name-shape checks: use-case nouns, refusal codes, template keys, setting units and published event names.</summary>
public sealed class RecordNameCheckTests
{
    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void Use_Case_Naming_Its_Aggregate_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{"vocabulary": {"verbs": ["borrow"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "README.md"), """{}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("use-case-noun-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void Aggregate_Plural_Passes_The_Noun_Check()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "README.md"), """{}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "list-loans.md"), """{"kind": "query"}"""));

        Assert.DoesNotContain("use-case-noun-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void Declared_Part_Passes_The_Noun_Check()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.order", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "orders", "README.md"), """{"parts": ["line"]}"""),
            TestModel.Record("use-case", "use-case/lending.add-line", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "orders", "add-line.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("use-case-noun-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void Glossary_Term_Passes_The_Noun_Check()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "README.md"), """{}"""),
            TestModel.Record("term", "term/guest", Path.Combine(temp.Path, "docs", "glossary.json"), """{"term": "guest", "meaning": "A visitor without a card."}"""),
            TestModel.Record("use-case", "use-case/lending.add-guest", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "visits", "add-guest.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("use-case-noun-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void Plural_Of_A_Glossary_Term_Passes_The_Noun_Check()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "README.md"), """{}"""),
            TestModel.Record("term", "term/guest", Path.Combine(temp.Path, "docs", "glossary.json"), """{"term": "guest", "meaning": "A visitor without a card."}"""),
            TestModel.Record("use-case", "use-case/lending.list-guests", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "visits", "list-guests.md"), """{"kind": "query"}"""));

        Assert.DoesNotContain("use-case-noun-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void Phrasal_Verb_Strips_Before_The_Noun_Check()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{"vocabulary": {"phrasal": ["check-in"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "README.md"), """{}"""),
            TestModel.Record("use-case", "use-case/lending.check-in-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "check-in-copy.md"), """{"kind": "command"}"""));

        Assert.DoesNotContain("use-case-noun-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model</proves>
    [Fact]
    public void Free_Noun_Reports_Use_Case_Noun_Unknown()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{"vocabulary": {"verbs": ["borrow"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "README.md"), """{}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-volume", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-volume.md"), """{"kind": "command"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "use-case-noun-unknown").Message;
        Assert.Contains("borrow-volume", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</proves>
    [Fact]
    public void Module_Code_Naming_Its_State_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.copy-unavailable", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "lending.copy-unavailable", "meaning": "The copy is out."}"""),
            TestModel.Record("result", "result/borrow-copy.card-expired", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "request.card-expired", "meaning": "The card lapsed."}"""));

        Assert.DoesNotContain("refusal-code-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</proves>
    [Fact]
    public void Declared_Generic_Refusal_Prefix_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{"refusals": {"statuses": {"gateway.provider-timeout": 504}}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.provider-silent", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "gateway.provider-silent", "meaning": "The provider stayed silent."}"""),
            TestModel.Record("result", "result/borrow-copy.provider-timeout", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "gateway.provider-timeout", "meaning": "The provider timed out."}"""),
            TestModel.Record("result", "result/borrow-copy.bad-input", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "request.validation-failed", "meaning": "The input fails validation."}"""));

        Assert.DoesNotContain("refusal-code-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</proves>
    [Fact]
    public void Declared_Module_Prefix_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/catalog", Path.Combine(temp.Path, "docs", "behavior", "modules", "catalog", "catalog.md"), """{}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.title-retired", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "catalog.title-retired", "meaning": "The title is retired."}"""));

        Assert.DoesNotContain("refusal-code-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</proves>
    [Fact]
    public void Composed_Refusal_Prefix_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{"refusals": {"composed": ["uniqueness.{key}-taken"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.register-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "register-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/register-copy.barcode-taken", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "copies", "register-copy.md"), """{"outcome": "refusal", "code": "uniqueness.copy-barcode-taken", "meaning": "Another copy holds the barcode."}"""));

        Assert.DoesNotContain("refusal-code-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</proves>
    [Fact]
    public void Project_Refusal_Prefix_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{"refusals": {"prefixes": ["shared"]}}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.locale-unknown", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "shared.locale-unknown", "meaning": "The locale is unknown."}"""));

        Assert.DoesNotContain("refusal-code-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</proves>
    [Fact]
    public void Vague_Word_Reports_Refusal_Code_Form()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.copy-invalid", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "lending.copy-invalid", "meaning": "The copy is invalid."}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "refusal-code-invalid").Message;
        Assert.Contains("invalid", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses</proves>
    [Fact]
    public void Foreign_Prefix_Reports_Refusal_Code_Form()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/borrow-copy.copy-on-loan", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """{"outcome": "refusal", "code": "billing.copy-on-loan", "meaning": "The copy is out."}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "refusal-code-invalid").Message;
        Assert.Contains("billing.copy-on-loan", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</proves>
    [Fact]
    public void Template_Key_Ending_In_Its_Audience_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "notifications", "loan-due.md"), """{"template": "loan-due-reader", "addresses": ["actor/reader"]}"""));

        Assert.DoesNotContain("template-key-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</proves>
    [Fact]
    public void Comma_Separated_Template_Keys_Pass_With_None_Skipped()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "notifications", "loan-due.md"), """{"template": "loan-due-reader, loan-due-staff, none (no message)", "addresses": ["actor/reader"]}"""));

        Assert.DoesNotContain("template-key-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</proves>
    [Fact]
    public void Template_Key_Missing_Its_Audience_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("notification", "notification/lending.loan-due", Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "notifications", "loan-due.md"), """{"template": "loan-due", "addresses": ["actor/reader"]}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "template-key-invalid").Message;
        Assert.Contains("loan-due", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/use-cases#state-the-unit-of-every-quantity-setting</proves>
    [Fact]
    public void Setting_With_A_Unit_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("setting", "setting/lending.loan-period", Path.Combine(temp.Path, "docs", "operations", "limits.md"), """{"valueType": "integer", "unit": "days"}"""),
            TestModel.Record("setting", "setting/lending.late-fee", Path.Combine(temp.Path, "docs", "operations", "limits.md"), """{"valueType": "money"}"""));

        Assert.DoesNotContain("setting-unit-missing", TestModel.Codes(model));
    }

    /// <proves>litespec/core/use-cases#state-the-unit-of-every-quantity-setting</proves>
    [Fact]
    public void Quantity_Setting_Without_A_Unit_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("setting", "setting/lending.loan-period", Path.Combine(temp.Path, "docs", "operations", "limits.md"), """{"valueType": "integer"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "setting-unit-missing").Message;
        Assert.Contains("setting/lending.loan-period", message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/edges#name-published-events-in-the-domains-words</proves>
    [Fact]
    public void Outbound_Event_Named_For_Its_Fact_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", Path.Combine(temp.Path, "docs", "behavior", "events", "loan-borrowed.md"), """{"direction": "out"}"""),
            TestModel.Record("integration-event", "integration-event/claim-staged", Path.Combine(temp.Path, "docs", "behavior", "events", "claim-staged.md"), """{"direction": "in"}"""));

        Assert.DoesNotContain("integration-event-name-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/edges#name-published-events-in-the-domains-words</proves>
    [Fact]
    public void Outbound_Event_Naming_Machinery_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path, """{}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/refund-staged", Path.Combine(temp.Path, "docs", "behavior", "events", "refund-staged.md"), """{"direction": "out"}"""));

        var message = Checker.Check(model).Findings.Single(finding => finding.Code == "integration-event-name-invalid").Message;
        Assert.Contains("staged", message, StringComparison.Ordinal);
    }

    static void WriteProject(string root, string json) =>
        File.WriteAllText(Path.Combine(root, "litespec.project.json"), json);
}
