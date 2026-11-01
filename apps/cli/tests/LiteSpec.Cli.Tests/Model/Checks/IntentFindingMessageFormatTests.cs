using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves every finding message from the intent-side checkers starts with a subject title and quoted identifier.</summary>
public sealed class IntentFindingMessageFormatTests
{
    /// <summary>Every message starts with a title and quoted identifier: "Requirement '...'", "Use case '...'", and the rest.</summary>
    static readonly Regex Format = new(@"^[A-Z][A-Za-z ]* '[^']+'", RegexOptions.Compiled);

    /// <summary>Phrases a B8 message never carries: instructions, past-tense facts, and pre-B8 subject shapes.</summary>
    static readonly string[] Banned =
    [
        "names no parent:",
        "Question on '",
        "Draft '",
        "Query '",
        "Operator job '",
        " on aggregate '",
        " of aggregate '",
        "Transition on ",
        "Terms '",
        "needs a ",
        "consider splitting",
        "declare its edges",
        "declare retry",
        "; use '",
        "use '",
        "changed after",
        "ended ",
        "cannot resolve",
        "exactly one of",
        "names serves;",
        "carries replaces;",
        "; declare",
        "as prose;",
        "delete the code",
        "write its parent record",
    ];

    /// <summary>Intent Messages Start With A Subject And Identifier.</summary>
    /// <proves>litespec/core/language#write-each-finding-message-as-a-fact</proves>
    [Fact]
    public void Intent_Messages_Start_With_A_Subject_And_Identifier()
    {
        using var temp = Cli.Temp("checks");
        var model = IntentModel(temp.Path);

        var findings = Checker.Check(model).Findings.ToList();

        Assert.NotEmpty(findings);
        Assert.All(findings, AssertFormat);
    }

    static void AssertFormat(Finding finding)
    {
        Assert.Matches(Format, finding.Message);
        Assert.EndsWith(".", finding.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("  ", finding.Message, StringComparison.Ordinal);

        foreach (var banned in Banned)
        {
            Assert.DoesNotContain(banned, finding.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>A model triggering the intent-side findings: uncited requirements, unparented records, unknown verbs, vague words, and lookalikes.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the triggering records.</returns>
    static ProjectModel IntentModel(string root)
    {
        return TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("requirement", "requirement/borrowing.max-5-loans", Path.Combine(root, "borrowing.md"), """{"kind": "functional", "statement": "A reader holds at most 5 active loans."}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(root, "charter.md"), """{"authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(root, "holds.md"), """{"status": "approved", "lifecycle": "planned", "plannedFor": "Pilot launch", "solves": ["problem/hold-delays"], "serves": ["goal/faster-checkout"]}"""),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(root, "borrow.md"), """{"actor": ["actor/reader"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("use-case", "use-case/lending.fetch-volume", Path.Combine(root, "fetch.md"), """{"kind": "command"}"""),
            TestModel.Record("reaction", "reaction/lending.notify-borrower-on-loan-borrowed", Path.Combine(root, "notify.md"), """{"triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}]}"""),
            TestModel.Record("rule", "rule/lending.handle-renewal", Path.Combine(root, "rules.md"), """{"kind": "calculation", "statement": "The clerk handles the renewal.", "governs": []}"""),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(root, "soldout.md"), """{"template": "soldout"}"""),
            TestModel.Record("reaction", "reaction/test.send-sold-out-notice-on-sale-ended", Path.Combine(root, "sold-out.md"), """{"triggeredBy": ["event/sale.sale-ended"]}"""),
            TestModel.Record("actor", "actor/reader", Path.Combine(root, "reader.md"), """{"status": "approved", "kind": "person", "questions": [{"question": "May a junior reader place holds?", "decideBy": "2020-01-01", "ballot": "junior-holds"}]}"""));
    }
}
