using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     The conformance level names and the lowest level that runs each finding code,
///     so <c>model check</c>, the book and <c>rules</c> report only what a project claims.
///     Each code takes the lowest level whose inputs its check reads: a check that
///     reads records alone runs at <see cref="ConformanceLevel.Records" />, a check
///     that reads code, screens, tests, test runs or measure results through a stack
///     reader runs at <see cref="ConformanceLevel.Traced" />, and a check that reads
///     LiteSpec's code layout, operations files, the bill of materials or the
///     controlled UI runs at <see cref="ConformanceLevel.Full" />.
/// </summary>
/// <follows>litespec/core/scope#declare-the-conformance-level</follows>
public static class ConformanceLevels
{
    /// <summary>
    ///     The finding codes about LiteSpec's own code layout. They run only at
    ///     <see cref="ConformanceLevel.Full" />, because a project at a lower level
    ///     keeps its own layout. The code folder checks report through these two
    ///     codes, so the group holds no record folder code: a record folder is
    ///     part of the record model and runs at records.
    /// </summary>
    private static readonly string[] LayoutCodes =
    [
        "code-placement-drift", "mark-folder-drift",
    ];

    /// <summary>
    ///     The finding codes that belong to the full contract beyond their tier:
    ///     the operations checks read alert and runbook files, the dependency
    ///     checks read the bill of materials, the repository and generator checks
    ///     read LiteSpec's Infrastructure layout, the contract checks read the
    ///     committed API contract, and the controlled UI checks read the shared
    ///     UI package and its baseline.
    /// </summary>
    private static readonly string[] FullCodes =
    [
        "quality-alert-missing", "alert-link-missing", "alert-threshold-drift", "runbook-unused",
        "dependency-undeclared", "dependency-version-drift", "dependency-license-unknown",
        "partial-missing", "repository-unimplemented", "collaborator-repository-invalid", "repository-read-invalid",
        "contract-break", "operation-id-duplicate", "operation-use-case-drift",
        "ui-package-invalid", "ui-baseline-missing", "ui-frontend-invalid", "ui-components-invalid",
        "ui-sourcelock-invalid", "ui-design-contract-invalid", "ui-stylesheet-invalid", "ui-class-invalid",
        "ui-import-invalid", "ui-dependency-invalid", "ui-catalog-invalid", "ui-route-suite-missing",
    ];

    /// <summary>
    ///     The finding codes below full whose check reads more than records: code
    ///     elements and their marks, screens and browser tests, scenario tests,
    ///     test runs, coverage or measure results. Each one runs from
    ///     <see cref="ConformanceLevel.Traced" /> even when its tier is G or A,
    ///     because at records no stack reader runs to produce its facts.
    /// </summary>
    private static readonly string[] TracedCodes =
    [
        "proof-invalid", "proves-duplicate", "use-case-unverified", "effect-unimplemented",
        "refusal-sentence-missing", "use-case-unreached", "query-filter-invalid",
        "screen-serves-drift", "step-screen-missing", "analyzer-lowering-unrecorded",
        "requirement-measure-missing", "constituent-parent-missing",
        "use-case-method-drift", "event-tense-invalid", "name-drift",
        "route-invalid", "dependency-count-invalid",
    ];

    /// <summary>
    ///     The finding codes that run at <see cref="ConformanceLevel.Records" />
    ///     although their tier or their absence from <see cref="Finding.Tiers" />
    ///     suggests a higher level: each check reads records, the project file or
    ///     the glossary alone. Two of them carry tier S by code yet read no code:
    ///     a use case with no scenario and an inline sends or calls timing are
    ///     both stated on the use case record itself.
    /// </summary>
    private static readonly string[] RecordsCodes =
    [
        "use-case-scenario-missing", "effect-timing-invalid",
        "use-case-verb-invalid", "reaction-name-invalid", "term-clash",
        "statement-word-invalid", "value-invalid", "use-case-noun-unknown",
        "refusal-code-invalid", "feature-goal-missing", "risk-acceptance-missing",
        "exemption-expired", "stack-invalid", "extension-invalid",
        "stage-deferral-invalid", "page-example-too-long",
    ];

    /// <summary>Gets the lowest level that runs each finding code; a code missing from the map runs at full.</summary>
    public static IReadOnlyDictionary<string, ConformanceLevel> Lowest { get; } = BuildLowest();

    /// <summary>Gets the layout group: the codes about LiteSpec's own code layout, each running only at full.</summary>
    public static IReadOnlyCollection<string> Layout { get; } = LayoutCodes;

    /// <summary>Reads a level from its name in the project file.</summary>
    /// <param name="value">The <c>conformance</c> value: records, traced or full.</param>
    /// <param name="level">The level the value names, or full when it names none.</param>
    /// <returns>True when the value names a level; otherwise false.</returns>
    public static bool TryParse(string? value, out ConformanceLevel level)
    {
        switch (value)
        {
            case "records":
                level = ConformanceLevel.Records;
                return true;
            case "traced":
                level = ConformanceLevel.Traced;
                return true;
            case "full":
                level = ConformanceLevel.Full;
                return true;
            default:
                level = ConformanceLevel.Full;
                return false;
        }
    }

    /// <summary>The name the project file writes for a level.</summary>
    /// <param name="level">The level.</param>
    /// <returns>records, traced or full.</returns>
    public static string Name(ConformanceLevel level) => level switch
    {
        ConformanceLevel.Records => "records",
        ConformanceLevel.Traced => "traced",
        ConformanceLevel.Full => "full",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Not a conformance level."),
    };

    /// <summary>The lowest level that runs a finding code.</summary>
    /// <param name="code">The finding code.</param>
    /// <returns>The lowest level that runs the code; full for a code the map does not hold.</returns>
    public static ConformanceLevel LowestOf(string code) =>
        Lowest.TryGetValue(code, out var level) ? level : ConformanceLevel.Full;

    /// <summary>Whether a project at the claimed level runs a finding code.</summary>
    /// <param name="code">The finding code.</param>
    /// <param name="claimed">The level the project claims.</param>
    /// <returns>True when the code's lowest level is at or below the claimed level.</returns>
    public static bool Runs(string code, ConformanceLevel claimed) => LowestOf(code) <= claimed;

    /// <summary>
    ///     Builds the map from every tiered finding code: a record-only code (tier G or A)
    ///     runs at records and a code read through a stack reader (tier S) at traced,
    ///     then the named groups take the level their check's inputs require. A code
    ///     the map does not hold runs at full, so a new code stays silent below full
    ///     until its check's inputs place it.
    /// </summary>
    /// <returns>The lowest level of every finding code.</returns>
    private static Dictionary<string, ConformanceLevel> BuildLowest()
    {
        var lowest = Finding.Tiers.ToDictionary(
            pair => pair.Key,
            pair => pair.Value == "S" ? ConformanceLevel.Traced : ConformanceLevel.Records,
            StringComparer.Ordinal);

        foreach (var code in TracedCodes)
        {
            lowest[code] = ConformanceLevel.Traced;
        }

        foreach (var code in FullCodes.Concat(LayoutCodes))
        {
            lowest[code] = ConformanceLevel.Full;
        }

        foreach (var code in RecordsCodes)
        {
            lowest[code] = ConformanceLevel.Records;
        }

        return lowest;
    }
}
