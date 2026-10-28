using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The mark rules for every <see cref="CodeKind" />: which record types a mark
///     may name, how many marks the element takes, and which verb they use.
///     It also holds the finding codes, analyzer rule ids, and tiers of the mark
///     findings, so every host prints the same codes.
/// </summary>
public static class MarkRules
{
    private static readonly string[] AggregateTypes = { "aggregate" };
    private static readonly string[] UseCaseTypes = { "use-case" };
    private static readonly string[] EventTypes = { "event" };
    private static readonly string[] RuleTypes = { "rule" };
    private static readonly string[] ReactionTypes = { "reaction" };
    private static readonly string[] JobTypes = { "job" };
    private static readonly string[] ExternalSystemTypes = { "external-system" };
    private static readonly string[] TestTypes = { "scenario", "rule" };

    private static readonly string[] Codes =
    {
        "mark-missing",
        "mark-unresolved",
        "mark-count",
        "mark-style-mixed",
        "runtime-value-drift",
        "partial-missing",
        "act-event-undeclared",
        "act-event-unraised",
    };

    /// <summary>
    ///     The record types a mark on the kind may name, as bare type names
    ///     without the trailing slash: a mark matches when its identifier starts
    ///     with one of these types followed by <c>/</c>.
    /// </summary>
    /// <param name="kind">The code kind.</param>
    /// <returns>The accepted record types.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    public static IReadOnlyList<string> RecordTypes(CodeKind kind) => RuleOf(kind).RecordTypes;

    /// <summary>Whether the kind takes exactly one mark rather than one or more.</summary>
    /// <param name="kind">The code kind.</param>
    /// <returns>True for exactly one mark, false for one or more.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    public static bool TakesExactlyOne(CodeKind kind) => RuleOf(kind).TakesExactlyOne;

    /// <summary>
    ///     The verb of marks on the kind: <see cref="MarkVerb.Proves" /> for tests,
    ///     <see cref="MarkVerb.Implements" /> otherwise.
    /// </summary>
    /// <param name="kind">The code kind.</param>
    /// <returns>The expected verb.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    public static MarkVerb Verb(CodeKind kind) => RuleOf(kind).Verb;

    /// <summary>
    ///     Whether a record identifier is of a type the kind takes: the identifier
    ///     starts with one of the kind's record types followed by <c>/</c> and a
    ///     non-empty name.
    /// </summary>
    /// <param name="kind">The code kind.</param>
    /// <param name="recordId">The full record identifier from the mark.</param>
    /// <returns>True when the kind takes that record type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    /// <exception cref="ArgumentNullException">The record identifier is null.</exception>
    public static bool Accepts(CodeKind kind, string recordId)
    {
        if (recordId is null)
        {
            throw new ArgumentNullException(nameof(recordId));
        }

        foreach (var type in RecordTypes(kind))
        {
            var prefix = type + "/";
            if (recordId.StartsWith(prefix, StringComparison.Ordinal) && recordId.Length > prefix.Length)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The finding codes of the mark findings, in rule id order.</summary>
    public static IReadOnlyList<string> FindingCodes => Codes;

    /// <summary>The analyzer rule id that reports the finding code.</summary>
    /// <param name="code">The finding code, such as <c>mark-missing</c>.</param>
    /// <returns>The rule id, such as <c>LS5001</c>.</returns>
    /// <exception cref="ArgumentNullException">The code is null.</exception>
    /// <exception cref="ArgumentException">The code names no mark finding.</exception>
    public static string RuleId(string code)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        switch (code)
        {
            case "mark-missing":
                return "LS5001";
            case "mark-unresolved":
                return "LS5002";
            case "mark-count":
                return "LS5003";
            case "mark-style-mixed":
                return "LS5004";
            case "runtime-value-drift":
                return "LS5005";
            case "partial-missing":
                return "LS5006";
            case "act-event-undeclared":
                return "LS5007";
            case "act-event-unraised":
                return "LS5008";
            default:
                throw new ArgumentException($"Unknown mark finding \"{code}\".", nameof(code));
        }
    }

    /// <summary>The check tier of the finding code.</summary>
    /// <param name="code">The finding code, such as <c>mark-missing</c>.</param>
    /// <returns>The tier: <c>G</c>, <c>S</c>, or <c>A</c>.</returns>
    /// <exception cref="ArgumentNullException">The code is null.</exception>
    /// <exception cref="ArgumentException">The code names no mark finding.</exception>
    public static string Tier(string code)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        switch (code)
        {
            case "mark-missing":
            case "mark-unresolved":
            case "mark-count":
            case "mark-style-mixed":
            case "runtime-value-drift":
            case "partial-missing":
            case "act-event-undeclared":
            case "act-event-unraised":
                return "S";
            default:
                throw new ArgumentException($"Unknown mark finding \"{code}\".", nameof(code));
        }
    }

    private static KindRule RuleOf(CodeKind kind)
    {
        switch (kind)
        {
            case CodeKind.AggregateRoot:
                return new KindRule(AggregateTypes, true, MarkVerb.Implements);
            case CodeKind.AggregateMethod:
                return new KindRule(UseCaseTypes, false, MarkVerb.Implements);
            case CodeKind.DomainEvent:
                return new KindRule(EventTypes, true, MarkVerb.Implements);
            case CodeKind.RuleException:
                return new KindRule(RuleTypes, true, MarkVerb.Implements);
            case CodeKind.Handler:
                return new KindRule(UseCaseTypes, true, MarkVerb.Implements);
            case CodeKind.EventHandler:
                return new KindRule(ReactionTypes, true, MarkVerb.Implements);
            case CodeKind.Job:
                return new KindRule(JobTypes, true, MarkVerb.Implements);
            case CodeKind.Port:
                return new KindRule(ExternalSystemTypes, false, MarkVerb.Implements);
            case CodeKind.Adapter:
                return new KindRule(ExternalSystemTypes, false, MarkVerb.Implements);
            case CodeKind.Endpoint:
                return new KindRule(UseCaseTypes, true, MarkVerb.Implements);
            case CodeKind.FrontendModule:
                return new KindRule(UseCaseTypes, true, MarkVerb.Implements);
            case CodeKind.DomainService:
                return new KindRule(RuleTypes, true, MarkVerb.Implements);
            case CodeKind.Test:
                return new KindRule(TestTypes, false, MarkVerb.Proves);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown code kind.");
        }
    }

    private sealed class KindRule
    {
        public KindRule(string[] recordTypes, bool takesExactlyOne, MarkVerb verb)
        {
            RecordTypes = recordTypes;
            TakesExactlyOne = takesExactlyOne;
            Verb = verb;
        }

        public IReadOnlyList<string> RecordTypes { get; }

        public bool TakesExactlyOne { get; }

        public MarkVerb Verb { get; }
    }
}
