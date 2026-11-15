using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the waiver checks against hand-built models: coverage by code, provision, page and path, the invalid covers items, and expiry.</summary>
public sealed class WaiverCheckTests
{
    /// <summary>The page whose Checked by line names mark-missing.</summary>
    private const string MarkMissingPage = "litespec/core/layers";

    /// <summary>The provision on that page whose Checked by line names mark-missing.</summary>
    private const string MarkMissingProvision = "litespec/core/layers#write-each-layer-from-the-layer-above-it";

    /// <summary>A model holding one marked code element with no mark, so mark-missing fires on its path, and one waiver naming the covers.</summary>
    /// <param name="temp">The temporary folder the model's files sit in.</param>
    /// <param name="covers">The covers items as a JSON array.</param>
    /// <param name="extraFields">Further JSON members of the waiver, each beginning with a comma.</param>
    /// <returns>The waiver record and the model holding it.</returns>
    private static (LiteSpec.Cli.Model.Records.Record Waiver, ProjectModel Model) ModelWithUnmarkedHandler(string temp, string covers, string extraFields)
    {
        var waiver = TestModel.Record(
            "waiver",
            "waiver/ghost-route",
            Path.Combine(temp, "ghost-route.yaml"),
            "{\"covers\": " + covers + ", \"paths\": [\"src/GhostReaction.cs\"], \"reason\": \"Infrastructure.\"" + extraFields + "}");
        var model = TestModel.Model(TestModel.Config(temp), waiver);
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp, "src", "GhostReaction.cs"), 3, [], InTests: false));

        return (waiver, model);
    }

    /// <summary>A lasting waiver with no expiry date holds the finding its path names, and reports no expiry.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void LastingWaiverWithoutExpiryHoldsItsPathFindings()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", "");

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
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"" + MarkMissingPage + "\"]", "");

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
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"" + MarkMissingProvision + "\"]", "");

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
            "{\"covers\": [\"mark-missing\"], \"paths\": [\"src/OtherReaction.cs\"], \"reason\": \"Infrastructure.\"}");
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
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"record-invalid\"]", "");

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
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"no-such-finding\"]", "");

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "waiver-invalid");

        Assert.Equal("Waiver 'waiver/ghost-route' names 'no-such-finding', which no check emits, and which is no shared provision or page.", finding.Message);
    }

    /// <summary>A waiver naming a provision the installed release does not ship reports waiver-invalid.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverNamingAnUnshippedProvisionReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"litespec/core/layers#no-such-provision\"]", "");

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "waiver-invalid");

        Assert.Contains("which the installed release does not ship as a provision or page", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A waiver past its expiry date reports waiver-expired and holds nothing.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverPastItsExpiresOnReportsExpiredAndHoldsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", ", \"expiresOn\": \"2000-01-01\", \"exit\": \"The route is marked.\"");

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
        var (_, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", ", \"expiresOn\": \"2999-01-01\", \"exit\": \"The route is marked.\"");

        Assert.DoesNotContain("waiver-expired", TestModel.Codes(model));
    }

    /// <summary>A waiver that sets an expiry date and names no exit reports record-invalid.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverWithExpiryAndNoExitReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var (waiver, model) = ModelWithUnmarkedHandler(temp.Path, "[\"mark-missing\"]", ", \"expiresOn\": \"2999-01-01\"");

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == waiver.Id);

        Assert.Contains("exit", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A waiver naming neither subject nor paths reports record-invalid.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void WaiverNamingNeitherSubjectNorPathsReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("waiver", "waiver/list-loans-unimplemented", Path.Combine(temp.Path, "list-loans-unimplemented.yaml"), "{\"covers\": [\"use-case-unimplemented\"], \"reason\": \"Listing waits for the search index.\"}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("names neither subject nor paths", StringComparison.Ordinal));
        Assert.Equal("Waiver 'waiver/list-loans-unimplemented' names neither subject nor paths.", finding.Message);
        Assert.Equal("waiver/list-loans-unimplemented", finding.Subject);
    }
}
