using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the value-style check: each broken value reported, each clean value passed.</summary>
public sealed class LanguageCheckTests
{
    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Statements_Without_Period_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"kind": "functional", "statement": "A room the traveller starts to book is held for them", "rationale": "partners share the no-show risk"}"""),
            TestModel.Record(
                "rule",
                "rule/orders.hold-lasts",
                Path.Combine(temp.Path, "hold.md"),
                """{"kind": "invariant", "statement": "A hold lasts for ten minutes", "governs": []}"""),
            TestModel.Record(
                "goal",
                "goal/more-completed-bookings",
                Path.Combine(temp.Path, "goal.md"),
                """{"statement": "More started bookings complete"}"""),
            TestModel.Record(
                "problem",
                "problem/rooms-sell-out",
                Path.Combine(temp.Path, "problem.md"),
                """{"statement": "Rooms sell out at checkout"}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();

        Assert.Equal(4, findings.Count);
        Assert.All(findings, finding => Assert.Contains("does not end in a period", finding.Message, StringComparison.Ordinal));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Theory]
    [InlineData("should", "A room should stay held for ten minutes.")]
    [InlineData("must", "A hold must last for ten minutes.")]
    [InlineData("may", "The traveller may extend the hold.")]
    public void Statement_With_Lower_Case_Modal_Reported(string modal, string statement)
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"kind": "functional", "statement": "__STATEMENT__", "rationale": "partners share the no-show risk"}""".Replace("__STATEMENT__", statement)));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();

        Assert.Contains(findings, finding => finding.Message.Contains($"'{modal}'", StringComparison.Ordinal));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Statement_Modal_Inside_Backticks_Skipped()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "rule",
                "rule/orders.hold-lasts",
                Path.Combine(temp.Path, "hold.md"),
                """{"kind": "invariant", "statement": "The `should` flag stays stored.", "governs": []}"""));

        Assert.DoesNotContain("value-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Step_Statement_Without_Period_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "journey",
                "journey/book-a-room",
                Path.Combine(temp.Path, "booking.md"),
                """{"finishedWhen": "the traveller holds a booking", "steps": [{"id": "s1", "statement": "The traveller pays for the booking"}]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("Journey step 'journey/book-a-room#s1'", finding.Message, StringComparison.Ordinal);
        Assert.Contains("does not end in a period", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Step_Statement_Without_Actor_Opening_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "journey",
                "journey/book-a-room",
                Path.Combine(temp.Path, "booking.md"),
                """{"finishedWhen": "the traveller holds a booking", "steps": [{"id": "s1", "statement": "Pays for the booking."}]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("starts with neither 'The' nor an actor", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Step_Statements_With_Actor_Openings_Pass()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "actor",
                "actor/concierge",
                Path.Combine(temp.Path, "concierge.md"),
                """{"kind": "person", "title": "Concierge"}"""),
            TestModel.Record(
                "journey",
                "journey/book-a-room",
                Path.Combine(temp.Path, "booking.md"),
                """{"finishedWhen": "the traveller holds a booking", "steps": [{"id": "s1", "statement": "The traveller pays for the booking."}, {"id": "s2", "statement": "Concierge hands over the key.", "failure": "The traveller tries again later."}]}"""));

        Assert.DoesNotContain("value-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Meaning_Without_Article_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "term",
                "term/booking",
                Path.Combine(temp.Path, "booking.md"),
                """{"meaning": "Confirmed reservation of one room for one stay."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("does not start with 'A', 'An' or 'The'", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Meaning_Over_25_Words_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "term",
                "term/booking",
                Path.Combine(temp.Path, "booking.md"),
                """{"meaning": "A confirmed reservation of one room for one stay that the traveller booked through the webshop during checkout yesterday with breakfast included every morning for two guests."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("over 25 words", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Clean_Meaning_Passes()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "term",
                "term/booking",
                Path.Combine(temp.Path, "booking.md"),
                """{"meaning": "A confirmed reservation of one room for one stay."}"""));

        Assert.DoesNotContain("value-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Rationale_Starting_With_Because_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"kind": "functional", "statement": "A room the traveller starts to book is held for them for 10 minutes.", "rationale": "Because partners share the no-show risk."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("has a rationale that starts with 'because'", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Because_Field_Starting_With_Because_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "constraint",
                "constraint/total-price-display",
                Path.Combine(temp.Path, "price.md"),
                """{"kind": "legal", "applies": true, "appliesBecause": "because the law demands a total."}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("has an appliesBecause that starts with 'because'", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Theory]
    [InlineData("when the hold window changes", "when")]
    [InlineData("if the hold lapses", "if")]
    public void When_Field_Starting_With_When_Or_If_Reported(string condition, string word)
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "decision",
                "decision/0001-room-hold-length",
                Path.Combine(temp.Path, "hold-length.md"),
                """{"options": [{"name": "short", "pros": "Fewer stale holds.", "cons": "More hurried travellers."}], "reopenedWhen": "__CONDITION__"}""".Replace("__CONDITION__", condition)));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();

        Assert.Contains(findings, finding => finding.Message.Contains($"starts with '{word}'", StringComparison.Ordinal));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Except_Condition_Starting_With_When_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"kind": "functional", "statement": "A room the traveller starts to book is held for them for 10 minutes.", "rationale": "partners share the no-show risk", "except": [{"condition": "when the room is held", "fallback": "requirement/checkout.room-held"}]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("has an except condition that starts with 'when'", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Failure_With_Refusal_Code_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "journey",
                "journey/book-a-room",
                Path.Combine(temp.Path, "booking.md"),
                """{"finishedWhen": "the traveller holds a booking", "steps": [{"id": "s1", "statement": "The traveller pays for the booking.", "failure": "The traveller sees orders.not-found."}]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("has a failure that contains a refusal code", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Top_Level_Failure_With_Refusal_Code_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "external-system",
                "external-system/payout-bank",
                Path.Combine(temp.Path, "bank.md"),
                """{"failure": "The bank answers payouts.declined.", "operations": [{"name": "create-charge", "direction": "out"}]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("has a failure that contains a refusal code", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Questions_Without_Mark_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/partner-hold-limits",
                Path.Combine(temp.Path, "limits.md"),
                """{"question": "How long should a room stay held", "sources": [{"readOn": "2026-01-01", "url": "https://example.com"}], "conclusion": "Ten minutes.", "questions": [{"question": "Who approves the window", "decider": "lending-team", "decideBy": "2027-01-01", "ballot": "holds"}]}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();

        Assert.Equal(2, findings.Count);
        Assert.All(findings, finding => Assert.Contains("does not end in '?'", finding.Message, StringComparison.Ordinal));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Clean_Question_Passes()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/partner-hold-limits",
                Path.Combine(temp.Path, "limits.md"),
                """{"question": "How long should a room stay held during checkout?", "sources": [{"readOn": "2026-01-01", "url": "https://example.com"}], "conclusion": "Ten minutes."}"""));

        Assert.DoesNotContain("value-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Title_Outside_Title_Case_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"title": "room held during checkout", "kind": "functional", "statement": "A room is held for ten minutes.", "rationale": "partners share the risk"}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("has a title that is not Title Case", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Title_Ending_With_Period_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"title": "Room Held.", "kind": "functional", "statement": "A room is held for ten minutes.", "rationale": "partners share the risk"}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();
        var finding = Assert.Single(findings);

        Assert.Contains("has a title that ends in a period", finding.Message, StringComparison.Ordinal);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Clean_Titles_Pass()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/checkout.room-held",
                Path.Combine(temp.Path, "checkout.md"),
                """{"title": "Room Held During Checkout", "kind": "functional", "statement": "A room is held for ten minutes.", "rationale": "partners share the risk"}"""),
            TestModel.Record(
                "goal",
                "goal/more-completed-bookings",
                Path.Combine(temp.Path, "goal.md"),
                """{"title": "More Completed Bookings", "statement": "More started bookings complete."}"""),
            TestModel.Record(
                "problem",
                "problem/rooms-lost",
                Path.Combine(temp.Path, "problem.md"),
                """{"title": "Rooms Lost at Checkout", "statement": "Rooms sell out at checkout."}"""));

        Assert.DoesNotContain("value-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Nested_Meanings_And_Conditions_Reported()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "aggregate",
                "aggregate/lending.order",
                Path.Combine(temp.Path, "order.md"),
                """{"states": [{"name": "Held", "meaning": "Room is held."}], "data": [{"name": "contact", "meaning": "Contact details."}]}"""),
            TestModel.Record(
                "use-case",
                "use-case/lending.list-loans",
                Path.Combine(temp.Path, "loans.md"),
                """{"kind": "query", "input": [{"name": "room", "meaning": "Room to hold."}], "output": {"include": {"lines": {"fields": [{"name": "total", "meaning": "Total price."}], "visibleWhen": "when lines exist"}}}}"""));

        var findings = Checker.Check(model).Findings.Where(finding => finding.Code == "value-invalid").ToList();

        Assert.Equal(5, findings.Count);
    }

    /// <proves>litespec/core/language#write-each-value-in-the-value-style</proves>
    [Fact]
    public void Clean_Nested_Meaning_Passes()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "aggregate",
                "aggregate/lending.order",
                Path.Combine(temp.Path, "order.md"),
                """{"states": [{"name": "Held", "meaning": "A room held for the traveller."}]}"""));

        Assert.DoesNotContain("value-invalid", TestModel.Codes(model));
    }
}
