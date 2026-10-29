using System;
using System.Collections.Generic;
using System.Linq;

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
    /// <summary>
    ///     Every finding code with its tier: G reads records only and always blocks,
    ///     S reads code through a stack reader and blocks only where the reader is
    ///     qualified, and A is advisory and never blocks.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Tiers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["record-shape"] = "G",
            ["invalid-field"] = "G",
            ["identifier-form"] = "G",
            ["identifier-duplicate"] = "G",
            ["reference-unresolved"] = "G",
            ["link-unresolved"] = "G",
            ["owner-unknown"] = "G",
            ["draft-without-decider"] = "G",
            ["draft-gate"] = "G",
            ["product-not-singleton"] = "G",
            ["size-type-missing"] = "G",
            ["requirement-unfulfilled"] = "G",
            ["requirement-unmeasured"] = "G",
            ["decision-decides-nothing"] = "G",
            ["journey-reach"] = "G",
            ["state-unreachable"] = "G",
            ["transition-unresolved"] = "G",
            ["use-case-without-success"] = "G",
            ["scenario-result-unresolved"] = "G",
            ["calculation-without-table"] = "G",
            ["rule-name-negation"] = "G",
            ["known-gap-expired"] = "G",
            ["gap-unknown-check"] = "G",
            ["aggregate-name-clash"] = "G",
            ["use-case-name-clash"] = "G",
            ["use-case-verb-unknown"] = "G",
            ["term-lookalike"] = "A",
            ["statement-vague-word"] = "A",
            ["use-case-noun-unknown"] = "A",
            ["refusal-code-form"] = "A",
            ["template-key-form"] = "A",
            ["setting-unit-missing"] = "A",
            ["integration-event-wording"] = "G",
            ["use-case-verb-kind"] = "G",
            ["reaction-name-form"] = "G",
            ["job-name-form"] = "G",
            ["reaction-trigger-unresolved"] = "G",
            ["erasure-undeclared"] = "G",
            ["record-orphaned"] = "G",
            ["step-unserved"] = "G",
            ["effect-loop"] = "G",
            ["consistency-unexplained"] = "G",
            ["query-output-missing"] = "G",
            ["paging-undeclared"] = "G",
            ["widening-unjustified"] = "G",
            ["retention-without-enforcement"] = "G",
            ["risk-without-acceptance"] = "G",
            ["exemption-expired"] = "G",
            ["setting-default-without-requirement"] = "G",
            ["reservation-without-release"] = "G",
            ["notification-without-sender"] = "G",
            ["except-without-fallback"] = "G",
            ["approval-reopened"] = "G",
            ["transcribed-reviewed"] = "G",
            ["transcribed-after-adoption"] = "G",
            ["operator-job-without-guard"] = "G",
            ["schedule-unstructured"] = "G",
            ["behavior-unrecorded"] = "S",
            ["use-case-unimplemented"] = "S",
            ["refusal-not-thrown"] = "S",
            ["proves-unconfirmed"] = "S",
            ["use-case-without-scenario"] = "S",
            ["write-undeclared"] = "S",
            ["result-undeclared"] = "S",
            ["effect-undeclared"] = "S",
            ["prose-missing"] = "A",
            ["many-aggregates"] = "A",
            ["question-aged"] = "A",
            ["use-case-verb-method"] = "A",
            ["event-tense-mismatch"] = "A",
            ["name-drift-across-layers"] = "A",
            ["operation-id-duplicate"] = "S",
            ["route-shape"] = "A",
            ["reasonless-name"] = "S",
            ["collaborator-holds-repository"] = "A",
            ["repository-read-in-loop"] = "A",
            ["query-filters-in-memory"] = "A",
            ["dependency-count"] = "A",
            ["effect-before-commit"] = "S",
            ["mark-missing"] = "S",
            ["mark-unresolved"] = "S",
            ["mark-count"] = "S",
            ["mark-style-mixed"] = "S",
            ["runtime-value-drift"] = "S",
            ["partial-missing"] = "S",
            ["act-event-undeclared"] = "S",
            ["act-event-unraised"] = "S",
            ["use-case-act-uncalled"] = "S",
            ["aggregate-unimplemented"] = "S",
            ["reaction-unimplemented"] = "S",
            ["job-unimplemented"] = "S",
            ["external-system-unimplemented"] = "S",
            ["use-case-unexposed"] = "S",
            ["effect-unimplemented"] = "A",
            ["use-case-unreached"] = "A",
            ["decision-unfollowed"] = "S",
            ["decision-without-basis"] = "A",
        };

    /// <summary>The finding tier: G blocks, S blocks where qualified, A reports.</summary>
    public string Tier => Tiers.TryGetValue(Code, out var tier) ? tier : "G";

    /// <summary>
    ///     Whether the project's stack reader is qualified, so S findings block:
    ///     the .NET reader is qualified, and every other stack only reports.
    /// </summary>
    public static bool ReaderQualified(IEnumerable<string> stacks) =>
        stacks.Contains("dotnet", StringComparer.Ordinal)
        || stacks.Contains("blazor", StringComparer.Ordinal)
        || stacks.Contains("android", StringComparer.Ordinal);

    /// <summary>Whether the finding blocks the check under a qualified or unqualified reader.</summary>
    public bool Blocks(bool readerQualified) => Tier == "G" || (Tier == "S" && readerQualified);
}
