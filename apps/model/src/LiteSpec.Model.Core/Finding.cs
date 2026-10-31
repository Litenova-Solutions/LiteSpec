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
    /// <summary>The reader that produced this finding's facts: <c>roslyn</c>, <c>typescript</c>, or null for a record-only finding.</summary>
    public string? Reader { get; init; }

    /// <summary>Whether the reader named by <see cref="Reader" /> is qualified, so its S findings block.</summary>
    public bool ReaderIsQualified { get; init; } = true;

    /// <summary>The .NET (Roslyn) reader name.</summary>
    public const string RoslynReader = "roslyn";

    /// <summary>The tier this one finding carries instead of its code's tier, when the check grades it by risk.</summary>
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
            ["field-invalid"] = "G",
            ["identifier-invalid"] = "G",
            ["identifier-duplicate"] = "G",
            ["reference-unknown"] = "G",
            ["link-unknown"] = "G",
            ["owner-unknown"] = "G",
            ["decider-missing"] = "G",
            ["approval-invalid"] = "G",
            ["product-count-invalid"] = "G",
            ["size-type-missing"] = "G",
            ["requirement-fulfillment-missing"] = "G",
            ["requirement-measure-missing"] = "G",
            ["requirement-cites-missing"] = "A",
            ["constraint-unreached"] = "G",
            ["parent-reopened"] = "A",
            ["decision-decides-missing"] = "G",
            ["use-case-journey-missing"] = "G",
            ["state-unreached"] = "G",
            ["transition-unknown"] = "G",
            ["use-case-success-missing"] = "G",
            ["scenario-result-unknown"] = "G",
            ["calculation-table-missing"] = "G",
            ["table-drift"] = "A",
            ["rule-name-invalid"] = "G",
            ["known-gap-expired"] = "G",
            ["gap-finding-unknown"] = "G",
            ["aggregate-name-clash"] = "G",
            ["use-case-name-clash"] = "G",
            ["use-case-verb-unknown"] = "G",
            ["term-clash"] = "A",
            ["statement-word-invalid"] = "A",
            ["value-invalid"] = "A",
            ["use-case-noun-unknown"] = "A",
            ["refusal-code-invalid"] = "A",
            ["template-key-invalid"] = "A",
            ["setting-unit-missing"] = "A",
            ["integration-event-name-invalid"] = "G",
            ["use-case-verb-invalid"] = "G",
            ["automation-name-invalid"] = "G",
            ["automation-trigger-unknown"] = "G",
            ["erasure-undeclared"] = "G",
            ["record-link-missing"] = "G",
            ["record-parent-missing"] = "G",
            ["feature-goal-missing"] = "A",
            ["result-scenario-missing"] = "A",
            ["step-use-case-missing"] = "G",
            ["effect-loop-invalid"] = "G",
            ["consistency-cites-missing"] = "G",
            ["query-output-missing"] = "G",
            ["paging-undeclared"] = "G",
            ["widening-cites-missing"] = "G",
            ["retention-enforcement-missing"] = "G",
            ["risk-acceptance-missing"] = "G",
            ["exemption-expired"] = "G",
            ["setting-requirement-missing"] = "G",
            ["reservation-release-missing"] = "G",
            ["notification-sender-missing"] = "G",
            ["except-fallback-missing"] = "G",
            ["approval-reopened"] = "G",
            ["transcription-status-invalid"] = "G",
            ["transcription-expired"] = "G",
            ["automation-guard-missing"] = "G",
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
            ["question-expired"] = "A",
            ["use-case-method-drift"] = "A",
            ["event-tense-invalid"] = "A",
            ["name-drift"] = "A",
            ["operation-id-duplicate"] = "S",
            ["route-invalid"] = "A",
            ["name-reason-missing"] = "S",
            ["collaborator-repository-invalid"] = "A",
            ["repository-read-invalid"] = "A",
            ["query-filter-invalid"] = "A",
            ["dependency-count-invalid"] = "A",
            ["effect-timing-invalid"] = "S",
            ["mark-missing"] = "S",
            ["mark-unknown"] = "S",
            ["mark-count-invalid"] = "S",
            ["mark-style-invalid"] = "S",
            ["runtime-value-drift"] = "S",
            ["partial-missing"] = "S",
            ["act-event-undeclared"] = "S",
            ["act-event-unimplemented"] = "S",
            ["use-case-act-missing"] = "S",
            ["aggregate-unimplemented"] = "S",
            ["repository-unimplemented"] = "S",
            ["automation-unimplemented"] = "S",
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
        };

    /// <summary>The finding tier: G blocks, S blocks where qualified, A reports.</summary>
    public string Tier => TierOverride ?? (Tiers.TryGetValue(Code, out var tier) ? tier : "G");
}
