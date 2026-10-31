using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves <c>litespec model check</c> against the fixtures, one test per broken overlay.</summary>
public sealed class ModelCheckTests
{
    /// <summary>Every finding code the builder emits, one entry per broken overlay.</summary>
    static readonly string[] FindingCodeList =
    [
        "aggregate-name-clash",
        "approval-reopened",
        "behavior-unrecorded",
        "calculation-table-missing",
        "consistency-cites-missing",
        "decision-decides-missing",
        "approval-invalid",
        "decider-missing",
        "effect-loop-invalid",
        "effect-undeclared",
        "erasure-undeclared",
        "except-fallback-missing",
        "exemption-expired",
        "exception-finding-unknown",
        "identifier-duplicate",
        "identifier-invalid",
        "field-invalid",
        "use-case-journey-missing",
        "exception-expired",
        "link-unknown",
        "notification-sender-missing",
        "job-guard-missing",
        "owner-unknown",
        "paging-undeclared",
        "product-count-invalid",
        "proves-unreached",
        "query-output-missing",
        "reaction-trigger-unknown",
        "record-link-missing",
        "record-invalid",
        "reference-unknown",
        "refusal-unimplemented",
        "requirement-fulfillment-missing",
        "requirement-measure-missing",
        "reservation-release-missing",
        "result-undeclared",
        "retention-enforcement-missing",
        "risk-acceptance-missing",
        "rule-name-invalid",
        "scenario-result-unknown",
        "schedule-invalid",
        "setting-requirement-missing",
        "size-type-missing",
        "state-unreached",
        "step-use-case-missing",
        "transcription-expired",
        "transcription-status-invalid",
        "transition-unknown",
        "use-case-name-clash",
        "use-case-unimplemented",
        "use-case-verb-unknown",
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
        Assert.Equal(0, result.Status);
    }

    /// <summary>Clean large fixture exits 0.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void Clean_Large_Fixture_Exits_0()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(0, result.Status);
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
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(1, result.Status);
        Assert.Contains(code, result.Output);
    }

    /// <summary>The theory's code list matches the overlay folders, so a new overlay without a case fails.</summary>
    /// <proves>litespec/core/release#run-consistency-checks</proves>
    [Fact]
    public void Finding_Codes_Match_Broken_Overlay_Folders()
    {
        var overlays = Directory.GetDirectories(Cli.Fixture("Model/broken"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        var codes = FindingCodeList.OrderBy(code => code, StringComparer.Ordinal).ToList();
        Assert.Equal(codes, overlays);
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
            "List loans unimplemented",
            ["use-case-unimplemented", "use-case-endpoint-missing", "screen-offers-drift", "behavior-unrecorded"],
            ["use-case/lending.list-loans", "screen/library-app.loans"],
            "2035-06-01");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(0, result.Status);
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
            "List loans unimplemented",
            ["use-case-unimplemented"],
            ["use-case/lending.list-loans"],
            "2020-01-01");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(1, result.Status);
    }

    /// <summary>Writes an approved exception covering the codes on the subjects.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="id">The exception record id.</param>
    /// <param name="title">The exception page title.</param>
    /// <param name="covers">The finding codes the exception covers.</param>
    /// <param name="subject">The record ids the exception applies to.</param>
    /// <param name="expiresOn">The date the exception expires, in ISO form.</param>
    static void WriteException(string root, string id, string title, string[] covers, string[] subject, string expiresOn)
    {
        var folder = Path.Combine(root, "docs", "governance", "exceptions");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, $"{id.Split('/')[1]}.md"), ExceptionFile(id, title, covers, subject, expiresOn));
    }

    // Writes JSON frontmatter, a heading, and one line of body, joined with line feeds.
    // The approval hash is stamped over the written fields, so the exception itself reports nothing.
    static string ExceptionFile(string id, string title, string[] covers, string[] subject, string expiresOn)
    {
        var frontmatter =
            "{\n"
            + "  \"type\": \"exception\",\n"
            + $"  \"id\": \"{id}\",\n"
            + $"  \"title\": \"{title}\",\n"
            + "  \"owner\": \"lending-team\",\n"
            + "  \"status\": \"approved\",\n"
            + "  \"kind\": \"gap\",\n"
            + "  \"covers\": [\n"
            + string.Join(",\n", covers.Select(code => $"    \"{code}\""))
            + "\n  ],\n"
            + "  \"subject\": [\n"
            + string.Join(",\n", subject.Select(entry => $"    \"{entry}\""))
            + "\n  ],\n"
            + $"  \"expiresOn\": \"{expiresOn}\",\n"
            + "  \"reason\": \"Listing waits for the search index.\"\n"
            + "}\n";

        using var document = JsonDocument.Parse(frontmatter);
        var record = new LiteSpec.Model.Record
        {
            Type = "exception",
            Id = id,
            Raw = document.RootElement.Clone(),
        };
        var hash = Checker.SemanticHash(record);
        var stamped = frontmatter.Replace(
            "\n}",
            ",\n  \"approvedBy\": \"lending-team\",\n  \"approvedOn\": \"2026-10-01\",\n  \"approvedHash\": \"" + hash + "\"\n}");

        return "---\n" + stamped + "---\n" + "\n" + $"# {title}\n" + "\n" + "Listing loans has no handler yet.\n";
    }

}
