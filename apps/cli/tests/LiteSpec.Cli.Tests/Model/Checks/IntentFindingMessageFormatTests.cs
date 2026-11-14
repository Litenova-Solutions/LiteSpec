using System.Text.RegularExpressions;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

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
    public void IntentMessagesStartWithASubjectAndIdentifier()
    {
        using var temp = TestCli.Temp("checks");
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

    /// <summary>A model triggering the intent-side findings: uncited requirements, unparented records, uncovered steps, unknown triggers, empty governs, template keys and missing effects.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the triggering records.</returns>
    static ProjectModel IntentModel(string root)
    {
        return TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(root, "max-active-loans.yaml"), """{"title": "Max active loans", "kind": "functional", "statement": "A reader holds at most 5 active loans."}"""),
            TestModel.Record("constraint", "constraint/library-keeps-records", Path.Combine(root, "library-keeps-records.yaml"), """{"title": "Library keeps records", "authority": "Library charter.", "kind": "organizational", "applies": true, "appliesBecause": "The library keeps records."}"""),
            TestModel.Record("feature", "feature/hold-requests", Path.Combine(root, "hold-requests.yaml"), """{"title": "Hold requests", "serves": ["goal/faster-checkout"]}"""),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(root, "borrow-and-return.yaml"), """{"title": "Borrow and return", "actor": ["actor/reader"], "steps": [{"id": "s1", "statement": "The reader borrows the copy."}]}"""),
            TestModel.Record("use-case", "use-case/lending.fetch-volume", Path.Combine(root, "fetch-volume.yaml"), """{"title": "Fetch volume", "kind": "command"}"""),
            TestModel.Record("reaction", "reaction/lending.notify-borrower-on-loan-borrowed", Path.Combine(root, "notify-borrower-on-loan-borrowed.yaml"), """{"title": "Notify borrower on loan borrowed", "triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due"}]}"""),
            TestModel.Record("rule", "rule/lending.handle-renewal", Path.Combine(root, "handle-renewal.yaml"), """{"title": "Handle renewal", "kind": "calculation", "statement": "The clerk handles the renewal.", "governs": []}"""),
            TestModel.Record("notification", "notification/test.send-soldout-notice", Path.Combine(root, "send-soldout-notice.yaml"), """{"title": "Send soldout notice", "template": "soldout"}"""),
            TestModel.Record("reaction", "reaction/test.send-sold-out-notice-on-sale-ended", Path.Combine(root, "send-sold-out-notice-on-sale-ended.yaml"), """{"title": "Send sold out notice on sale ended", "triggeredBy": ["event/sale.sale-ended"]}"""),
            TestModel.Record("actor", "actor/reader", Path.Combine(root, "reader.yaml"), """{"title": "Reader", "kind": "person"}"""));
    }
}
