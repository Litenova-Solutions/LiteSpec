using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     One check finding: a code, a message, a path and a line.
/// </summary>
/// <param name="Code">The stable finding code.</param>
/// <param name="Message">What is wrong, in one sentence.</param>
/// <param name="Path">The absolute path.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Subject">The record id the finding is about, when there is one.</param>
public sealed record Finding(string Code, string Message, string Path, int Line, string? Subject)
{
    /// <summary>Gets the reader that produced this finding's facts: <c>roslyn</c>, <c>typescript</c>, or null for a record-only finding.</summary>
    public string? Reader { get; init; }

    /// <summary>Gets a value indicating whether whether the reader named by <see cref="Reader" /> is qualified, so its S findings block.</summary>
    public bool ReaderIsQualified { get; init; } = true;

    /// <summary>The .NET (Roslyn) reader name.</summary>
    public const string RoslynReader = "roslyn";

    /// <summary>Gets the tier this one finding carries instead of its code's tier, when the check grades it apart from its code.</summary>
    public string? TierOverride { get; init; }

    /// <summary>The TypeScript reader name.</summary>
    public const string TypeScriptReader = "typescript";
    /// <summary>
    ///     Every finding code with its tier: G reads records only and always blocks,
    ///     S reads code through a stack reader and blocks only where the reader is
    ///     qualified, and A is advisory and never blocks.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Tiers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["record-invalid"] = "G",
            ["folder-invalid"] = "G",
            ["module-undeclared"] = "G",
            ["record-placement-invalid"] = "G",
            ["identifier-folder-drift"] = "G",
            ["name-duplicate"] = "G",
            ["decision-number-duplicate"] = "G",
            ["link-locality-invalid"] = "G",
            ["module-dependency-invalid"] = "G",
            ["event-publisher-invalid"] = "G",
            ["intent-unreached"] = "G",
            ["role-orphaned"] = "G",
            ["permission-orphaned"] = "G",
            ["decision-decides-duplicate"] = "G",
            ["code-placement-drift"] = "S",
            ["field-invalid"] = "G",
            ["identifier-invalid"] = "G",
            ["identifier-duplicate"] = "G",
            ["reference-unknown"] = "G",
            ["link-unknown"] = "G",
            ["product-count-invalid"] = "G",
            ["size-type-missing"] = "G",
            ["requirement-fulfillment-missing"] = "G",
            ["requirement-measure-missing"] = "G",
            ["requirement-cites-missing"] = "A",
            ["constraint-unreached"] = "G",
            ["decision-decides-missing"] = "G",
            ["use-case-journey-missing"] = "G",
            ["state-unreached"] = "G",
            ["transition-unknown"] = "G",
            ["use-case-success-missing"] = "G",
            ["scenario-result-unknown"] = "G",
            ["calculation-table-missing"] = "G",
            ["explanation-orphaned"] = "A",
            ["explanation-missing"] = "A",
            ["format-drift"] = "A",
            ["source-link-bare"] = "A",
            ["source-key-unknown"] = "A",
            ["source-unused"] = "A",
            ["rule-name-invalid"] = "G",
            ["exception-expired"] = "G",
            ["exception-finding-unknown"] = "G",
            ["template-key-invalid"] = "A",
            ["setting-unit-missing"] = "A",
            ["integration-event-name-invalid"] = "G",
            ["job-name-invalid"] = "G",
            ["reaction-trigger-unknown"] = "G",
            ["erasure-undeclared"] = "G",
            ["record-link-missing"] = "G",
            ["record-parent-missing"] = "G",
            ["result-scenario-missing"] = "A",
            ["step-use-case-missing"] = "G",
            ["effect-loop-invalid"] = "G",
            ["consistency-cites-missing"] = "G",
            ["query-output-missing"] = "G",
            ["paging-undeclared"] = "G",
            ["widening-cites-missing"] = "G",
            ["retention-enforcement-missing"] = "G",
            ["setting-requirement-missing"] = "G",
            ["reservation-release-missing"] = "G",
            ["notification-sender-missing"] = "G",
            ["except-fallback-missing"] = "G",
            ["job-guard-missing"] = "G",
            ["schedule-invalid"] = "G",
            ["behavior-unrecorded"] = "S",
            ["use-case-unimplemented"] = "S",
            ["refusal-unimplemented"] = "S",
            ["proves-unreached"] = "S",
            ["use-case-scenario-missing"] = "S",
            ["write-undeclared"] = "S",
            ["result-undeclared"] = "S",
            ["effect-undeclared"] = "S",
            ["prose-missing"] = "A",
            ["atomic-group-duplicate"] = "A",
            ["atomic-group-drift"] = "G",
            ["atomic-group-invalid"] = "A",
            ["operation-id-duplicate"] = "S",
            ["name-reason-missing"] = "S",
            ["collaborator-repository-invalid"] = "A",
            ["repository-read-invalid"] = "A",
            ["query-filter-invalid"] = "A",
            ["effect-timing-invalid"] = "S",
            ["mark-missing"] = "S",
            ["mark-unknown"] = "S",
            ["mark-count-invalid"] = "S",
            ["mark-style-invalid"] = "S",
            ["mark-folder-drift"] = "S",
            ["runtime-value-drift"] = "S",
            ["partial-missing"] = "S",
            ["act-event-undeclared"] = "S",
            ["act-event-unimplemented"] = "S",
            ["use-case-act-missing"] = "S",
            ["aggregate-unimplemented"] = "S",
            ["repository-unimplemented"] = "S",
            ["reaction-unimplemented"] = "S",
            ["job-unimplemented"] = "S",
            ["external-system-unimplemented"] = "S",
            ["use-case-endpoint-missing"] = "S",
            ["effect-unimplemented"] = "A",
            ["use-case-unreached"] = "A",
            ["decision-unimplemented"] = "S",
            ["decision-basis-missing"] = "A",
            ["operation-use-case-drift"] = "S",
            ["constituent-parent-missing"] = "A",
            ["alert-link-missing"] = "S",
            ["quality-alert-missing"] = "G",
            ["alert-threshold-drift"] = "A",
            ["setting-key-missing"] = "G",
            ["setting-key-drift"] = "S",
            ["setting-unimplemented"] = "S",
            ["template-unimplemented"] = "S",
            ["template-unrecorded"] = "S",
            ["template-param-drift"] = "S",
            ["dependency-undeclared"] = "S",
            ["dependency-version-drift"] = "S",
            ["dependency-license-unknown"] = "S",
            ["admission-invalid"] = "G",
            ["screen-unimplemented"] = "S",
            ["screen-offers-drift"] = "S",
            ["screen-serves-drift"] = "G",
            ["step-screen-missing"] = "G",
            ["refusal-sentence-missing"] = "A",
            ["reader-missing"] = "S",
            ["proves-duplicate"] = "A",
            ["problem-sufferer-invalid"] = "G",
            ["problem-evidence-missing"] = "G",
            ["actor-unused"] = "A",
            ["goal-mover-missing"] = "G",
            ["term-missing"] = "A",
            ["runbook-unused"] = "A",
            ["use-case-unverified"] = "A",
            ["proof-invalid"] = "A",
            ["frontend-invalid"] = "G",
            ["glossary-invalid"] = "G",
            ["page-heading-invalid"] = "G",
            ["page-invalid"] = "G",
            ["page-section-missing"] = "G",
            ["project-invalid"] = "G",
            ["project-path-invalid"] = "G",
            ["contract-break"] = "S",
            ["analyzer-lowering-unrecorded"] = "S",
        };

    /// <summary>Gets the finding tier: G blocks, S blocks where qualified, A reports.</summary>
    public string Tier => TierOverride ?? (Tiers.TryGetValue(Code, out var tier) ? tier : "G");
}
