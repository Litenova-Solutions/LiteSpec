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
        "calculation-without-table",
        "consistency-unexplained",
        "decision-decides-nothing",
        "draft-gate",
        "draft-without-decider",
        "effect-loop",
        "effect-undeclared",
        "erasure-undeclared",
        "except-without-fallback",
        "exemption-expired",
        "gap-unknown-check",
        "identifier-duplicate",
        "identifier-form",
        "invalid-field",
        "journey-reach",
        "known-gap-expired",
        "link-unresolved",
        "notification-without-sender",
        "operator-job-without-guard",
        "owner-unknown",
        "paging-undeclared",
        "product-not-singleton",
        "proves-unconfirmed",
        "query-output-missing",
        "reaction-trigger-unresolved",
        "record-orphaned",
        "record-shape",
        "reference-unresolved",
        "refusal-not-thrown",
        "requirement-unfulfilled",
        "requirement-unmeasured",
        "reservation-without-release",
        "result-undeclared",
        "retention-without-enforcement",
        "risk-without-acceptance",
        "rule-name-negation",
        "scenario-result-unresolved",
        "schedule-unstructured",
        "setting-default-without-requirement",
        "size-type-missing",
        "state-unreachable",
        "step-unserved",
        "transcribed-after-adoption",
        "transcribed-reviewed",
        "transition-unresolved",
        "use-case-name-clash",
        "use-case-unimplemented",
        "use-case-verb-unknown",
        "use-case-without-scenario",
        "use-case-without-success",
        "widening-unjustified",
        "write-undeclared",
    ];

    /// <summary>The finding codes as theory rows, one row per broken overlay.</summary>
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
    [Fact]
    public void Clean_Small_Fixture_Exits_0()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(0, result.Status);
    }

    /// <summary>Clean large fixture exits 0.</summary>
    [Fact]
    public void Clean_Large_Fixture_Exits_0()
    {
        using var fixture = ModelFixture.Prepare("large");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(0, result.Status);
    }

    /// <summary>Broken overlay '&lt;code&gt;' exits 1 with its finding code in the output.</summary>
    [Theory]
    [MemberData(nameof(FindingCodeData))]
    public void Broken_Overlay_Exits_1(string code)
    {
        using var fixture = ModelFixture.Prepare("large", code);
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(1, result.Status);
        Assert.Contains(code, result.Output);
    }

    /// <summary>The theory's code list matches the overlay folders, so a new overlay without a case fails.</summary>
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

    /// <summary>Open known gaps turn their findings into exit 0.</summary>
    [Fact]
    public void Open_Known_Gap_Turns_Its_Finding_Into_Exit_0()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        ModelFixture.WriteKnownGap(fixture.Path, "2035-06-01");
        WriteKnownGap(fixture.Path, "known-gap/list-loans-unexposed", "List loans unexposed", "use-case-unexposed", "2035-06-01");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(0, result.Status);
    }

    /// <summary>Expired known gap exits 1.</summary>
    [Fact]
    public void Expired_Known_Gap_Exits_1()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        ModelFixture.WriteKnownGap(fixture.Path, "2020-01-01");
        var result = Cli.Run("model", "check", "--root", fixture.Path, "--format", "json");
        Assert.Equal(1, result.Status);
    }

    /// <summary>Writes an open known gap covering the check on the list-loans use case.</summary>
    static void WriteKnownGap(string root, string id, string title, string check, string endsBy)
    {
        var folder = Path.Combine(root, "docs", "known-gaps");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, $"{id.Split('/')[1]}.md"), GapFile(id, title, check, endsBy));
    }

    // Writes JSON frontmatter, a heading, and one line of body, joined with line feeds.
    // The approval hash is stamped over the written fields, so the gap itself reports nothing.
    static string GapFile(string id, string title, string check, string endsBy)
    {
        var frontmatter =
            "{\n"
            + "  \"type\": \"known-gap\",\n"
            + $"  \"id\": \"{id}\",\n"
            + $"  \"title\": \"{title}\",\n"
            + "  \"owner\": \"lending-team\",\n"
            + "  \"status\": \"approved\",\n"
            + $"  \"check\": \"{check}\",\n"
            + "  \"subject\": [\n"
            + "    \"use-case/lending.list-loans\"\n"
            + "  ],\n"
            + $"  \"endsBy\": \"{endsBy}\",\n"
            + "  \"reason\": \"Listing waits for the search index.\"\n"
            + "}\n";

        using var document = JsonDocument.Parse(frontmatter);
        var record = new LiteSpec.Model.Record
        {
            Type = "known-gap",
            Id = id,
            Raw = document.RootElement.Clone(),
        };
        var hash = Checker.SemanticHash(record);
        var stamped = frontmatter.Replace(
            "\n}",
            ",\n  \"approvedBy\": \"lending-team\",\n  \"approvedOn\": \"2026-10-01\",\n  \"approvedHash\": \"" + hash + "\"\n}");

        return "---\n" + stamped + "---\n" + "\n" + $"# {title}\n" + "\n" + "Listing loans has no endpoint yet.\n";
    }

}
