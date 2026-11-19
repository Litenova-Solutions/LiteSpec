using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the waiver checks against hand-built models: coverage by code, provision, page and path, the kind shapes, the overlap of two live waivers, the invalid waives items, and expiry.</summary>
public sealed class WaiverCheckTests
{
    /// <summary>The page whose Checked by line names mark-missing.</summary>
    private const string MarkMissingPage = "litespec/core/layers";

    /// <summary>The provision on that page whose Checked by line names mark-missing.</summary>
    private const string MarkMissingProvision = "litespec/core/layers#write-each-layer-from-the-layer-above-it";

    /// <summary>The gap shape every paths waiver in these tests carries: its kind, closing record, exit and expiry.</summary>
    const string GapShape = ", \"kind\": \"gap\", \"closedBy\": [\"waiver/ghost-route\"], \"exitWhen\": \"The route is marked.\", \"expiresOn\": \"2999-01-01\"";

    /// <summary>The lasting shape: its kind and consequences, with no expiry, exit or closing records.</summary>
    const string LastingShape = ", \"kind\": \"lasting\", \"consequences\": \"The route stays unmarked.\"";

    /// <summary>A model holding one marked code element with no mark, so mark-missing fires on its path, and one waiver naming the waives.</summary>
    /// <param name="temp">The temporary folder the model's files sit in.</param>
    /// <param name="covers">The waives items as a JSON array.</param>
    /// <param name="extraFields">Further JSON members of the waiver, each beginning with a comma.</param>
    /// <returns>The waiver record and the model holding it.</returns>
    private static (LiteSpec.Cli.Model.Records.Record Waiver, ProjectModel Model) ModelWithUnmarkedHandler(string temp, string covers, string extraFields)
    {
        var waiver = TestModel.Record(
            "waiver",
            "waiver/ghost-route",
            Path.Combine(temp, "ghost-route.yaml"),
            "{\"waives\": " + covers + ", \"paths\": [\"src/GhostReaction.cs\"], \"reason\": \"Infrastructure.\"" + extraFields + "}");
        var model = TestModel.Model(TestModel.Config(temp), waiver);
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp, "src", "GhostReaction.cs"), 3, [], InTests: false));

        return (waiver, model);
    }

    /// <summary>One gap waiver with the waives and appliesTo, closed by itself.</summary>
    /// <param name="temp">The temporary folder the model's files sit in.</param>
    /// <param name="id">The waiver record id.</param>
    /// <param name="covers">The waives items as a JSON array.</param>
    /// <param name="subject">The appliesTo items as a JSON array.</param>
    /// <returns>The waiver record.</returns>
    private static LiteSpec.Cli.Model.Records.Record GapWaiver(string temp, string id, string covers, string subject) =>
        TestModel.Record(
            "waiver",
            id,
            Path.Combine(temp, id.Split('/')[1] + ".yaml"),
            "{\"kind\": \"gap\", \"waives\": " + covers + ", \"appliesTo\": " + subject + ", \"reason\": \"Infrastructure.\", \"closedBy\": [\"" + id + "\"], \"exitWhen\": \"The gap closes.\", \"expiresOn\": \"2999-01-01\"}");

    /// <summary>A gap waiver holds the finding its path names until its expiry date, and reports no expiry before it.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void GapWaiverHoldsItsPathFindings()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", GapShape);

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "mark-missing");
        Assert.Contains(known, finding => finding.Code == "mark-missing");
        Assert.DoesNotContain("waiver-expired", TestModel.Codes(model));
    }

    /// <summary>A waiver naming a page holds the findings of the codes that page's provisions check.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void PageCoverageHoldsTheCodesOfItsProvisions()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"" + MarkMissingPage + "\"]", GapShape);

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "mark-missing");
        Assert.Contains(known, finding => finding.Code == "mark-missing");
    }

    /// <summary>A waiver naming a provision holds the findings of the codes its Checked by line names.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void ProvisionCoverageHoldsTheCodesItsCheckedByLineNames()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"" + MarkMissingProvision + "\"]", GapShape);

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "mark-missing");
        Assert.Contains(known, finding => finding.Code == "mark-missing");
    }

    /// <summary>A waiver holds findings only on the paths it names, so a file it does not name keeps its finding.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverHoldsNothingOnOtherPaths()
    {
        using var temp = TestCli.Temp("checks");
        var waiver = TestModel.Record(
            "waiver",
            "waiver/other-route",
            Path.Combine(temp.Path, "other-route.yaml"),
            "{\"kind\": \"gap\", \"waives\": [\"mark-missing\"], \"paths\": [\"src/OtherReaction.cs\"], \"reason\": \"Infrastructure.\", \"closedBy\": [\"waiver/other-route\"], \"exitWhen\": \"The route is marked.\", \"expiresOn\": \"2999-01-01\"}");
        var model = TestModel.Model(TestModel.Config(temp.Path), waiver);
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "src", "GhostReaction.cs"), 3, [], InTests: false));

        var (findings, known) = Checker.Check(model);

        Assert.Contains(findings, finding => finding.Code == "mark-missing");
        Assert.DoesNotContain(known, finding => finding.Code == "mark-missing");
    }

    /// <summary>A waiver naming a model-load code reports waiver-invalid and holds no finding.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverNamingAModelLoadCodeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"record-invalid\"]", GapShape);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "waiver-invalid");

        Assert.Equal("Waiver 'waiver/ghost-route' names 'record-invalid', which the model needs in order to load, so no waiver covers it.", finding.Message);
        Assert.Equal(waiver.Id, finding.Subject);
    }

    /// <summary>A waiver naming a code no check emits reports waiver-invalid.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverNamingAnUnknownCodeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"no-such-finding\"]", GapShape);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "waiver-invalid");

        Assert.Equal("Waiver 'waiver/ghost-route' names 'no-such-finding', which no check emits, and which is no shared provision or page.", finding.Message);
    }

    /// <summary>A waiver naming a provision the installed release does not ship reports waiver-invalid.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverNamingAnUnshippedProvisionReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"litespec/core/layers#no-such-provision\"]", GapShape);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "waiver-invalid");

        Assert.Contains("which the installed release does not ship as a provision or page", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A waiver past its expiry date reports waiver-expired and holds nothing.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverPastItsExpiresOnReportsExpiredAndHoldsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", ", \"kind\": \"gap\", \"closedBy\": [\"waiver/ghost-route\"], \"expiresOn\": \"2000-01-01\", \"exitWhen\": \"The route is marked.\"");

        var expired = Assert.Single(Checker.Check(model).Findings, found => found.Code == "waiver-expired");

        Assert.Equal("Waiver 'waiver/ghost-route' stands past its expiresOn 2000-01-01.", expired.Message);
        Assert.Equal(waiver.Id, expired.Subject);
        Assert.Contains("mark-missing", TestModel.Codes(model));
    }

    /// <summary>A waiver before its expiry date reports no expiry.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverBeforeItsExpiresOnReportsNoExpiry()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", GapShape);

        Assert.DoesNotContain("waiver-expired", TestModel.Codes(model));
    }

    /// <summary>A waiver that sets an expiry date and names no exit reports record-invalid.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverWithExpiryAndNoExitReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", ", \"kind\": \"gap\", \"closedBy\": [\"waiver/ghost-route\"], \"expiresOn\": \"2999-01-01\"");

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == waiver.Id);

        Assert.Contains("exit", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A waiver naming neither appliesTo nor paths reports record-invalid.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverNamingNeitherSubjectNorPathsReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("waiver", "waiver/list-loans-unimplemented", Path.Combine(temp.Path, "list-loans-unimplemented.yaml"), "{\"kind\": \"gap\", \"waives\": [\"use-case-unimplemented\"], \"reason\": \"Listing waits for the search index.\", \"closedBy\": [\"waiver/list-loans-unimplemented\"], \"exitWhen\": \"The search index ships.\", \"expiresOn\": \"2999-01-01\"}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("names neither appliesTo nor paths", StringComparison.Ordinal));
        Assert.Equal("Waiver 'waiver/list-loans-unimplemented' names neither appliesTo nor paths.", finding.Message);
        Assert.Equal("waiver/list-loans-unimplemented", finding.Subject);
    }

    /// <summary>A gap waiver naming no closing record reports record-invalid.</summary>
    /// <proves>litespec/core/waivers#name-the-records-that-close-a-gap</proves>
    [Fact]
    public void GapWithoutClosedByReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", ", \"kind\": \"gap\", \"exitWhen\": \"The route is marked.\", \"expiresOn\": \"2999-01-01\"");

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == waiver.Id);

        Assert.Equal("Record 'waiver/ghost-route' has no closedBy.", finding.Message);
    }

    /// <summary>A gap waiver naming its closing record, exit and expiry reports no shape finding.</summary>
    /// <proves>litespec/core/waivers#name-the-records-that-close-a-gap</proves>
    [Fact]
    public void GapWithClosedByReportsNoShapeFinding()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", GapShape);

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            found => found.Code is "record-invalid" or "record-invalid" && found.Subject == waiver.Id);
    }

    /// <summary>A lasting waiver carrying an expiry date reports record-invalid.</summary>
    /// <proves>litespec/core/waivers#state-the-kind-of-every-waiver</proves>
    [Fact]
    public void LastingWithExpiresOnReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", LastingShape + ", \"expiresOn\": \"2999-01-01\"");

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == waiver.Id);

        Assert.Equal("Waiver 'waiver/ghost-route' is lasting and holds expiresOn.", finding.Message);
    }

    /// <summary>A lasting waiver naming no consequences reports record-invalid.</summary>
    /// <proves>litespec/core/waivers#state-the-kind-of-every-waiver</proves>
    [Fact]
    public void LastingWithoutConsequencesReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", ", \"kind\": \"lasting\"");

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == waiver.Id);

        Assert.Equal("Record 'waiver/ghost-route' has no consequences.", finding.Message);
    }

    /// <summary>A lasting waiver naming its consequences and no gap field reports no shape finding.</summary>
    /// <proves>litespec/core/waivers#state-the-kind-of-every-waiver</proves>
    [Fact]
    public void LastingWithConsequencesReportsNoShapeFinding()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", LastingShape);

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            found => found.Code is "record-invalid" or "record-invalid" && found.Subject == waiver.Id);
    }

    /// <summary>Two live waivers covering one finding on one subject report waiver-duplicate on the later waiver.</summary>
    /// <proves>litespec/core/waivers#cover-each-finding-with-one-waiver</proves>
    [Fact]
    public void TwoWaiversCoveringOneFindingOnOneSubjectReportOverlap()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            GapWaiver(temp.Path, "waiver/early-route", "[\"mark-missing\"]", "[\"waiver/early-route\"]"),
            GapWaiver(temp.Path, "waiver/late-route", "[\"mark-missing\"]", "[\"waiver/early-route\"]"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "waiver-duplicate");

        Assert.Equal("Waiver 'waiver/late-route' overlaps waiver 'waiver/early-route': both cover 'mark-missing' on 'waiver/early-route'.", finding.Message);
        Assert.Equal("waiver/late-route", finding.Subject);
    }

    /// <summary>Two live waivers covering one finding on different subjects report no overlap.</summary>
    /// <proves>litespec/core/waivers#cover-each-finding-with-one-waiver</proves>
    [Fact]
    public void TwoWaiversOnDifferentSubjectsReportNoOverlap()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            GapWaiver(temp.Path, "waiver/early-route", "[\"mark-missing\"]", "[\"waiver/early-route\"]"),
            GapWaiver(temp.Path, "waiver/late-route", "[\"mark-missing\"]", "[\"waiver/late-route\"]"));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "waiver-duplicate");
    }

    /// <summary>Two live waivers covering different findings on one subject report no overlap.</summary>
    /// <proves>litespec/core/waivers#cover-each-finding-with-one-waiver</proves>
    [Fact]
    public void TwoWaiversCoveringDifferentFindingsReportNoOverlap()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            GapWaiver(temp.Path, "waiver/early-route", "[\"mark-missing\"]", "[\"waiver/early-route\"]"),
            GapWaiver(temp.Path, "waiver/late-route", "[\"mark-unknown\"]", "[\"waiver/early-route\"]"));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "waiver-duplicate");
    }
}
