using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model check</c> against the fixtures, one test per broken overlay.</summary>
public sealed class ModelCheckTests
{
    /// <summary>Every finding code the builder emits, one entry per broken overlay.</summary>
    static readonly string[] FindingCodeList =
    [
        "behavior-unrecorded",
        "calculation-table-missing",
        "consistency-cites-missing",
        "decision-decides-missing",
        "effect-loop-invalid",
        "effect-undeclared",
        "erasure-undeclared",
        "except-fallback-missing",
        "waiver-invalid",
        "identifier-duplicate",
        "identifier-invalid",
        "use-case-flow-missing",
        "waiver-expired",
        "link-unknown",
        "notification-effect-missing",
        "paging-undeclared",
        "product-count-invalid",
        "proves-unreached",
        "query-output-missing",
        "record-link-missing",
        "record-invalid",
        "record-placement-invalid",
        "link-locality-invalid",
        "module-dependency-invalid",
        "event-publisher-invalid",
        "intent-unreached",
        "role-unused",
        "permission-unused",
        "reference-unknown",
        "refusal-unimplemented",
        "requirement-fulfillment-missing",
        "requirement-measure-missing",
        "reservation-release-missing",
        "result-undeclared",
        "retention-enforcement-missing",
        "schedule-invalid",
        "setting-requirement-missing",
        "size-type-missing",
        "state-unreached",
        "step-use-case-missing",
        "use-case-unimplemented",
        "use-case-scenario-missing",
        "use-case-success-missing",
        "widening-cites-missing",
        "write-undeclared",
    ];

    /// <summary>Gets the finding codes as theory rows, one row per broken overlay.</summary>
    public static TheoryData<string> FindingCodeData
    {
        get
        {
            TheoryData<string> data = new();
            foreach (var code in FindingCodeList)
            {
                data.Add(code);
            }

            return data;
        }
    }

    /// <summary>Clean small fixture exits 0.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CleanSmallFixtureExits0()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.True(result.Status == 0, result.Output);
    }

    /// <summary>Clean large fixture exits 0.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void CleanLargeFixtureExits0()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.True(result.Status == 0, result.Output);
    }

    /// <summary>Broken overlay '&lt;code&gt;' exits 1 with its finding code in the output.</summary>
    /// <param name="code">The finding code the broken-overlay case expects in the output.</param>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Theory]
    [MemberData(nameof(FindingCodeData))]
    // The MemberData values name each broken-overlay finding code case.
    public void Broken_Overlay_Exits_1(string code)
    {
        using var fixture = ModelFixture.Prepare("large", code);

        var result = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);
        Assert.Equal(1, result.Status);
        Assert.Contains(code, result.Output);
    }

    /// <summary>Every listed code has a row in the finding code table, which states the layer, trace and extension its claim gate reads.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void FindingCodesHaveAClaimRow()
    {
        foreach (var code in FindingCodeList)
        {
            Assert.True(
                FindingCodes.IsKnown(code),
                $"Finding code '{code}' has no row in the finding code table.");
        }
    }

    /// <summary>Every listed code names a broken overlay folder, so a renamed code without its folder fails.</summary>
    /// <proves>litespec/core/release#check-intent-and-behavior-changes-with-the-book</proves>
    [Fact]
    public void FindingCodesNameBrokenOverlayFolders()
    {
        var overlays = Directory.GetDirectories(TestCli.Fixture("Model/broken"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var code in FindingCodeList)
        {
            Assert.Contains(code, overlays);
        }
    }

    /// <summary>An open waiver turns its findings into exit 0.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void OpenWaiverTurnsItsFindingsIntoExit0()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        WriteWaiver(
            fixture.Path,
            "waiver/list-loans-unimplemented",
            "List Loans Unimplemented",
            ["use-case-unimplemented", "use-case-endpoint-missing", "screen-offers-drift", "behavior-unrecorded", "step-implementation-missing"],
            ["use-case/lending.list-loans", "screen/library-app.loans", "step/borrow-and-return.review-loans"],
            "2035-06-01");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.True(result.Status == 0, result.Output);
    }

    /// <summary>An expired waiver exits 1.</summary>
    /// <proves>litespec/core/waivers#record-accepted-gaps-as-waivers</proves>
    [Fact]
    public void ExpiredWaiverExits1()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        WriteWaiver(
            fixture.Path,
            "waiver/list-loans-unimplemented",
            "List Loans Unimplemented",
            ["use-case-unimplemented"],
            ["use-case/lending.list-loans"],
            "2020-01-01");
        var result = TestCli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(1, result.Status);
    }

    /// <summary>Writes a waiver covering the codes on the subjects.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="id">The waiver record id.</param>
    /// <param name="title">The waiver page title.</param>
    /// <param name="covers">The finding codes the waiver covers.</param>
    /// <param name="subject">The record ids the waiver applies to.</param>
    /// <param name="expiresOn">The date the waiver expires, in ISO form.</param>
    static void WriteWaiver(string root, string id, string title, string[] covers, string[] subject, string expiresOn)
    {
        var name = id.Split('/')[1];
        TestRecords.Write(root, $".litespec/governance/waivers/{name}/{name}.yaml", WaiverJson(id, title, covers, subject, expiresOn));
        TestRecords.Explain(root, $".litespec/governance/waivers/{name}/{name}.md", $"# {title}\n\nListing loans has no handler yet.\n");
    }

    // Builds the waiver fields as JSON. A waiver is live when present and unexpired.
    static string WaiverJson(string id, string title, string[] covers, string[] subject, string expiresOn)
    {
        return "{\n"
            + "  \"type\": \"waiver\",\n"
            + $"  \"id\": \"{id}\",\n"
            + $"  \"title\": \"{title}\",\n"
            + "  \"kind\": \"gap\",\n"
            + "  \"covers\": [\n"
            + string.Join(",\n", covers.Select(code => $"    \"{code}\""))
            + "\n  ],\n"
            + "  \"subject\": [\n"
            + string.Join(",\n", subject.Select(entry => $"    \"{entry}\""))
            + "\n  ],\n"
            + "  \"closedBy\": [\n"
            + $"    \"{subject[0]}\"\n"
            + "  ],\n"
            + $"  \"expiresOn\": \"{expiresOn}\",\n"
            + "  \"reason\": \"Listing waits for the search index.\",\n"
            + "  \"exitWhen\": \"The search index ships and listing loans gains its handler.\"\n"
            + "}\n";
    }

}
