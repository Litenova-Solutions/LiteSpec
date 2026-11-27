using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Intent;
using LiteSpec.Cli.Model.Code;
using Record = LiteSpec.Cli.Model.Records.Record;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the cast checks: a person's email sits on a reserved test domain, each cast entry is cited by a
///     record or a seeds mark, and each seeds mark names a cast entry.
/// </summary>
/// <follows>litespec/quality/development#seed-only-development-and-test-environments</follows>
/// <follows>litespec/quality/development#add-a-missing-example-fact-to-the-cast-not-to-the-seed</follows>
public sealed class CastCheckTests
{
    /// <summary>The cast record file the fixture cast sits in.</summary>
    const string CastFile = "/project/.litespec/cast.yaml";

    /// <summary>The reserved-domain rule recognizes the RFC 2606 and RFC 6761 test domains and no other.</summary>
    /// <param name="domain">The domain after the at sign.</param>
    /// <param name="reserved">Whether the domain is reserved.</param>
    [Theory(DisplayName = "reserved test domains are recognized and public domains are not")]
    [InlineData("example.com", true)]
    [InlineData("example.net", true)]
    [InlineData("example.org", true)]
    [InlineData("harbor-and-pine.example", true)]
    [InlineData("shop.test", true)]
    [InlineData("kiosk.invalid", true)]
    [InlineData("desk.localhost", true)]
    [InlineData("gmail.com", false)]
    [InlineData("example.com.au", false)]
    [InlineData("exampled.com", false)]
    [InlineData("testing.example.org.uk", false)]
    public void ReservedDomainsAreRecognized(string domain, bool reserved) =>
        Assert.Equal(reserved, CastChecks.IsReservedDomain(domain));

    /// <summary>A person whose email sits on a public domain is flagged at cast-identity-unreserved.</summary>
    [Fact(DisplayName = "a person email on a public domain is flagged")]
    public void PublicEmailDomainIsFlagged()
    {
        using var temp = TestCli.Temp("cast-public-email");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@gmail.com"));

        Assert.Contains("cast-identity-unreserved", TestModel.Codes(model));
    }

    /// <summary>A person whose email sits on a reserved test domain is not flagged.</summary>
    [Fact(DisplayName = "a person email on a reserved test domain is not flagged")]
    public void ReservedEmailDomainIsNotFlagged()
    {
        using var temp = TestCli.Temp("cast-reserved-email");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"));

        Assert.DoesNotContain("cast-identity-unreserved", TestModel.Codes(model));
    }

    /// <summary>A cast entry that no record or seeds mark cites is flagged at cast-entry-unused.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact(DisplayName = "a cast entry that nothing cites is flagged as unused")]
    public void UncitedEntryIsFlagged()
    {
        using var temp = TestCli.Temp("cast-unused");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"));

        Assert.Contains("Cast entry 'person/ava' is cited by no record or code mark.", Messages(model));
    }

    /// <summary>A cast entry that a record field cites by its identifier is used.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact(DisplayName = "a cast entry a record cites by its identifier is used")]
    public void EntryCitedByIdentifierIsUsed()
    {
        using var temp = TestCli.Temp("cast-cited-id");
        var occasion = TestModel.Record(
            "occasion",
            "occasion/client-visit",
            CastFile,
            """{"title":"A visit","involves":["person/ava"],"scene":"A visit."}""");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"), occasion);

        Assert.DoesNotContain("Cast entry 'person/ava'", string.Join("\n", Messages(model)));
    }

    /// <summary>A person named by its exact name in another record's field is used.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact(DisplayName = "a person named by its exact name in a record is used")]
    public void PersonNamedInARecordIsUsed()
    {
        using var temp = TestCli.Temp("cast-named");
        var rule = TestModel.Record("rule", "rule/spending.submit", "/project/.litespec/rule.yaml", """{"title":"Submit","statement":"Ava submits with the receipt attached."}""");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"), rule);

        Assert.DoesNotContain("Cast entry 'person/ava'", string.Join("\n", Messages(model)));
    }

    /// <summary>A name inside a longer word is no exact name: Ava does not name Avalon.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact(DisplayName = "a name inside a longer word names no person")]
    public void NameInsideALongerWordIsNotUse()
    {
        using var temp = TestCli.Temp("cast-longer-word");
        var rule = TestModel.Record("rule", "rule/spending.submit", "/project/.litespec/rule.yaml", """{"title":"Submit","statement":"Avalon hosts the review."}""");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"), rule);

        Assert.Contains("Cast entry 'person/ava' is cited by no record or code mark.", Messages(model));
    }

    /// <summary>A person named only in an explanation file is unused: no check reads the explanation.</summary>
    /// <proves>litespec/core/model#split-record-fields-from-explanation</proves>
    [Fact(DisplayName = "a person named only in an explanation file is unused")]
    public void PersonNamedOnlyInAnExplanationIsUnused()
    {
        using var temp = TestCli.Temp("cast-explanation");
        var explanation = TestModel.Body("/project/.litespec/intent/submit.md", "Ava files the expense.");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"));
        model.Bodies.Add(explanation);

        Assert.Contains("Cast entry 'person/ava' is cited by no record or code mark.", Messages(model));
    }

    /// <summary>The cast's own explanation names its people without using them, so it cannot make an entry used.</summary>
    /// <proves>litespec/core/model#split-record-fields-from-explanation</proves>
    [Fact(DisplayName = "the cast explanation does not count as a use")]
    public void CastExplanationDoesNotCountAsAUse()
    {
        using var temp = TestCli.Temp("cast-own-explanation");
        var explanation = new LiteSpec.Cli.Model.Records.PageBody("/project/.litespec/cast.md", "Ava is the employee.", 1, "cast/shop");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"));
        model.Bodies.Add(explanation);

        Assert.Contains("Cast entry 'person/ava' is cited by no record or code mark.", Messages(model));
    }

    /// <summary>A person whose party names no record is an unknown reference, found by the existing citation check.</summary>
    /// <proves>litespec/core/model#resolve-every-page-link</proves>
    [Fact(DisplayName = "a person whose party names no record is an unknown reference")]
    public void PartyNamingNoRecordIsAnUnknownReference()
    {
        using var temp = TestCli.Temp("cast-party-unknown");
        var person = TestModel.Record(
            "person",
            "person/ava",
            CastFile,
            """{"name":"Ava","party":"party/nowhere","does":"Travels.","email":"ava@harbor-and-pine.example"}""");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), person);

        Assert.Contains("Record 'person/ava' cites 'party/nowhere', which no record holds.", Messages(model));
    }

    /// <summary>The cast explanation that cites an entry by its identifier is ignored: no check reads the explanation.</summary>
    /// <proves>litespec/core/model#split-record-fields-from-explanation</proves>
    [Fact(DisplayName = "the cast explanation citing an entry by its identifier is ignored")]
    public void CastExplanationCitingAnIdentifierIsIgnored()
    {
        using var temp = TestCli.Temp("cast-explanation-identifier");
        var explanation = new LiteSpec.Cli.Model.Records.PageBody("/project/.litespec/cast.md", "The visit is person/ava's.", 1, "cast/shop");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"));
        model.Bodies.Add(explanation);

        Assert.Contains("Cast entry 'person/ava' is cited by no record or code mark.", Messages(model));
    }

    /// <summary>A seeds mark that names a cast entry uses it.</summary>
    /// <proves>litespec/quality/development#add-a-missing-example-fact-to-the-cast-not-to-the-seed</proves>
    [Fact(DisplayName = "a seeds mark naming a cast entry uses it")]
    public void SeedsMarkUsesTheEntry()
    {
        using var temp = TestCli.Temp("cast-seeds-use");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"));
        model.Code.Seeds.Add(new CodeMark("person/ava", MarkStyle.Xml, MarkVerb.Seeds, "/project/src/Seed.cs", 3));

        var messages = string.Join("\n", Messages(model));

        Assert.DoesNotContain("Cast entry 'person/ava'", messages);
        Assert.DoesNotContain("Code seeds 'person/ava'", messages);
    }

    /// <summary>A seeds mark that names no cast entry is an unknown mark.</summary>
    /// <proves>litespec/quality/development#add-a-missing-example-fact-to-the-cast-not-to-the-seed</proves>
    [Fact(DisplayName = "a seeds mark naming no cast entry is an unknown mark")]
    public void SeedsMarkNamingNothingIsUnknown()
    {
        using var temp = TestCli.Temp("cast-seeds-unknown");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast(), Person("person/ava", "ava@harbor-and-pine.example"));
        model.Code.Seeds.Add(new CodeMark("person/nobody", MarkStyle.Xml, MarkVerb.Seeds, "/project/src/Seed.cs", 3));

        Assert.Contains("Code seeds 'person/nobody', which matches no party, person, occasion or figure in the cast.", Messages(model));
    }

    /// <summary>A seeds mark naming the cast page itself is unknown: the page is no entry.</summary>
    /// <proves>litespec/quality/development#add-a-missing-example-fact-to-the-cast-not-to-the-seed</proves>
    [Fact(DisplayName = "a seeds mark naming the cast page is an unknown mark")]
    public void SeedsMarkNamingTheCastPageIsUnknown()
    {
        using var temp = TestCli.Temp("cast-seeds-page");
        var model = TestModel.Model(TestModel.Config(temp.Path), Cast());
        model.Code.Seeds.Add(new CodeMark("cast/shop", MarkStyle.Comment, MarkVerb.Seeds, "/project/src/seed.ts", 1));

        Assert.Contains("Code seeds 'cast/shop', which matches no party, person, occasion or figure in the cast.", Messages(model));
    }

    /// <summary>The cast page record of the fixture.</summary>
    /// <returns>The cast record.</returns>
    static Record Cast() =>
        TestModel.Record("cast", "cast/shop", CastFile, """{"title":"Shop Cast"}""");

    /// <summary>A person of the fixture cast.</summary>
    /// <param name="id">The person identifier.</param>
    /// <param name="email">The person's email address.</param>
    /// <returns>The person record.</returns>
    static Record Person(string id, string email) =>
        TestModel.Record(
            "person",
            id,
            CastFile,
            $$"""{"name":"Ava","actor":"actor/employee","does":"Travels to clients.","email":"{{email}}"}""");

    /// <summary>The messages of the findings the full check reports.</summary>
    /// <param name="model">The model to check.</param>
    /// <returns>Each finding message in report order.</returns>
    static List<string> Messages(ProjectModel model) =>
        Checker.Check(model).Findings.Select(finding => finding.Message).ToList();
}
