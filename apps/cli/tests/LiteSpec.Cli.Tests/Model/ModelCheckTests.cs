using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

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
        "exception-finding-unknown",
        "identifier-duplicate",
        "identifier-invalid",
        "use-case-journey-missing",
        "exception-expired",
        "link-unknown",
        "notification-sender-missing",
        "job-guard-missing",
        "paging-undeclared",
        "product-count-invalid",
        "proves-unreached",
        "query-output-missing",
        "reaction-trigger-unknown",
        "record-link-missing",
        "record-invalid",
        "folder-invalid",
        "module-undeclared",
        "record-placement-invalid",
        "identifier-folder-drift",
        "name-duplicate",
        "decision-number-duplicate",
        "link-locality-invalid",
        "module-dependency-invalid",
        "event-publisher-invalid",
        "intent-unreached",
        "role-orphaned",
        "permission-orphaned",
        "decision-decides-duplicate",
        "reference-unknown",
        "refusal-unimplemented",
        "requirement-fulfillment-missing",
        "requirement-measure-missing",
        "reservation-release-missing",
        "result-undeclared",
        "retention-enforcement-missing",
        "rule-name-invalid",
        "scenario-result-unknown",
        "schedule-invalid",
        "setting-requirement-missing",
        "size-type-missing",
        "state-unreached",
        "step-use-case-missing",
        "transition-unknown",
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
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void Clean_Small_Fixture_Exits_0()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.True(result.Status == 0, result.Output);
    }

    /// <summary>Clean large fixture exits 0.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void Clean_Large_Fixture_Exits_0()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.True(result.Status == 0, result.Output);
    }

    /// <summary>Broken overlay '&lt;code&gt;' exits 1 with its finding code in the output.</summary>
    /// <param name="code">The finding code the broken-overlay case expects in the output.</param>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Theory]
    [MemberData(nameof(FindingCodeData))]
    // The MemberData values name each broken-overlay finding code case.
    public void Broken_Overlay_Exits_1(string code)
    {
        using var fixture = ModelFixture.Prepare("large", code);

        // Planned Intent blocks only in the pull-request check, so its cases run there.
        var environment = Severity.PlannedIntent.Contains(code)
            ? new Dictionary<string, string> { ["CI"] = "true" }
            : null;
        var result = Cli.Run(["model", "check", "--root", fixture.Path, "--format", "json"], environment: environment);
        Assert.Equal(1, result.Status);
        Assert.Contains(code, result.Output);
    }

    /// <summary>Every listed code names a broken overlay folder, so a renamed code without its folder fails.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void Finding_Codes_Name_Broken_Overlay_Folders()
    {
        var overlays = Directory.GetDirectories(Cli.Fixture("Model/broken"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var code in FindingCodeList)
        {
            Assert.Contains(code, overlays);
        }
    }

    /// <summary>An open exception turns its findings into exit 0.</summary>
    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Open_Exception_Turns_Its_Findings_Into_Exit_0()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        WriteException(
            fixture.Path,
            "exception/list-loans-unimplemented",
            "List Loans Unimplemented",
            ["use-case-unimplemented", "use-case-endpoint-missing", "screen-offers-drift", "behavior-unrecorded"],
            ["use-case/lending.list-loans", "screen/library-app.loans"],
            "2035-06-01");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.True(result.Status == 0, result.Output);
    }

    /// <summary>An expired exception exits 1.</summary>
    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Expired_Exception_Exits_1()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        WriteException(
            fixture.Path,
            "exception/list-loans-unimplemented",
            "List Loans Unimplemented",
            ["use-case-unimplemented"],
            ["use-case/lending.list-loans"],
            "2020-01-01");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(1, result.Status);
    }

    /// <summary>Writes an exception covering the codes on the subjects.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="id">The exception record id.</param>
    /// <param name="title">The exception page title.</param>
    /// <param name="covers">The finding codes the exception covers.</param>
    /// <param name="subject">The record ids the exception applies to.</param>
    /// <param name="expiresOn">The date the exception expires, in ISO form.</param>
    static void WriteException(string root, string id, string title, string[] covers, string[] subject, string expiresOn)
    {
        var name = id.Split('/')[1];
        TestRecords.Write(root, $".litespec/governance/exceptions/{name}.yaml", ExceptionJson(id, title, covers, subject, expiresOn));
        TestRecords.Explain(root, $".litespec/governance/exceptions/{name}.md", $"# {title}\n\nListing loans has no handler yet.\n");
    }

    // Builds the exception fields as JSON. An exception is live when present and unexpired.
    static string ExceptionJson(string id, string title, string[] covers, string[] subject, string expiresOn)
    {
        return "{\n"
            + "  \"type\": \"exception\",\n"
            + $"  \"id\": \"{id}\",\n"
            + $"  \"title\": \"{title}\",\n"
            + "  \"kind\": \"gap\",\n"
            + "  \"covers\": [\n"
            + string.Join(",\n", covers.Select(code => $"    \"{code}\""))
            + "\n  ],\n"
            + "  \"subject\": [\n"
            + string.Join(",\n", subject.Select(entry => $"    \"{entry}\""))
            + "\n  ],\n"
            + $"  \"expiresOn\": \"{expiresOn}\",\n"
            + "  \"reason\": \"Listing waits for the search index.\",\n"
            + "  \"exit\": \"The search index ships and listing loans gains its handler.\"\n"
            + "}\n";
    }

}
