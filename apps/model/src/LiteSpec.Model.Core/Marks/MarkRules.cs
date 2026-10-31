using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The mark rules for every <see cref="CodeKind" />: which record types a mark
///     may name, how many marks the element takes, and which verb they use.
///     It also holds the finding codes, analyzer rule ids, and tiers of the mark
///     findings, so every host prints the same codes.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class MarkRules
{
    private static readonly string[] AggregateTypes = { "aggregate" };
    private static readonly string[] UseCaseTypes = { "use-case" };
    private static readonly string[] EventTypes = { "event" };
    private static readonly string[] RuleTypes = { "rule" };
    private static readonly string[] ReactionTypes = { "reaction" };
    private static readonly string[] JobTypes = { "job" };
    private static readonly string[] ExternalSystemTypes = { "external-system" };
    private static readonly string[] ScreenTypes = { "screen" };
    private static readonly string[] TestTypes =
    {
        "scenario", "rule", "requirement", "decision",
        "external-system", "integration-event", "litespec",
    };
    private static readonly string[] TechnicalImplementsTypes =
    {
        "aggregate", "use-case", "reaction", "job", "external-system",
        "event", "rule", "notification", "setting", "integration-event",
    };
    private static readonly string[] FollowsTypes = { "litespec", "decision", "deviation" };

    private static readonly string[] Codes =
    {
        "mark-missing",
        "mark-unknown",
        "mark-count-invalid",
        "mark-style-invalid",
        "runtime-value-drift",
        "partial-missing",
        "act-event-undeclared",
        "act-event-unimplemented",
    };

    /// <summary>
    ///     The record types an <c>implements</c> or <c>proves</c> mark on the kind
    ///     may name, as bare type names without the trailing slash: a mark matches
    ///     when its identifier starts with one of these types followed by <c>/</c>.
    ///     For a technical class these are the Behavior record types its
    ///     <c>implements</c> marks take; its <c>follows</c> marks take
    ///     <see cref="FollowsRecordTypes" /> instead.
    /// </summary>
    /// <param name="kind">The code kind.</param>
    /// <returns>The accepted record types.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    public static IReadOnlyList<string> RecordTypes(CodeKind kind) => RuleOf(kind).RecordTypes;

    /// <summary>
    ///     The record types a <c>follows</c> mark may name, as bare type names:
    ///     the provision namespace and the decision and deviation records.
    /// </summary>
    public static IReadOnlyList<string> FollowsRecordTypes => FollowsTypes;

    /// <summary>Whether the kind takes exactly one mark rather than one or more.</summary>
    /// <param name="kind">The code kind.</param>
    /// <returns>True for exactly one mark, false for one or more.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    public static bool TakesExactlyOne(CodeKind kind) => RuleOf(kind).TakesExactlyOne;

    /// <summary>
    ///     The verb of <c>implements</c> and <c>proves</c> marks on the kind:
    ///     <see cref="MarkVerb.Proves" /> for tests,
    ///     <see cref="MarkVerb.Implements" /> otherwise, including technical
    ///     classes. A technical class also takes <see cref="MarkVerb.Follows" />;
    ///     use <see cref="TakesVerb" /> to test either verb.
    /// </summary>
    /// <param name="kind">The code kind.</param>
    /// <returns>The expected verb.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    public static MarkVerb Verb(CodeKind kind) => RuleOf(kind).Verb;

    /// <summary>
    ///     Whether a mark with the verb satisfies the kind's presence: a technical
    ///     class takes <c>implements</c> or <c>follows</c>, every other kind takes
    ///     only <see cref="Verb" />.
    /// </summary>
    /// <param name="kind">The code kind.</param>
    /// <param name="verb">The mark verb.</param>
    /// <returns>True when the verb counts as the kind's mark.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    public static bool TakesVerb(CodeKind kind, MarkVerb verb)
    {
        var rule = RuleOf(kind);

        return kind == CodeKind.Technical
            ? verb == MarkVerb.Implements || verb == MarkVerb.Follows
            : verb == rule.Verb;
    }

    /// <summary>
    ///     Whether a record identifier is of a type the kind's <c>implements</c> or
    ///     <c>proves</c> marks take: the identifier starts with one of the kind's
    ///     record types followed by <c>/</c> and a non-empty name. For a technical
    ///     class this tests the Behavior types its <c>implements</c> marks take;
    ///     use <see cref="AcceptsFollows" /> for its <c>follows</c> marks.
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

    /// <summary>
    ///     Whether a record identifier is of a type a <c>follows</c> mark takes:
    ///     the identifier starts with one of <see cref="FollowsRecordTypes" />
    ///     followed by <c>/</c> and a non-empty name.
    /// </summary>
    /// <param name="recordId">The full record identifier from the mark.</param>
    /// <returns>True when a <c>follows</c> mark takes that record type.</returns>
    /// <exception cref="ArgumentNullException">The record identifier is null.</exception>
    public static bool AcceptsFollows(string recordId)
    {
        if (recordId is null)
        {
            throw new ArgumentNullException(nameof(recordId));
        }

        foreach (var type in FollowsTypes)
        {
            var prefix = type + "/";
            if (recordId.StartsWith(prefix, StringComparison.Ordinal) && recordId.Length > prefix.Length)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Decides whether an element's marks violate its kind's count rule: the
    ///     kind takes exactly one mark and the element carries more, or one record
    ///     id appears twice. An element with no marks is no violation: its missing
    ///     mark is <c>mark-missing</c>, not <c>mark-count-invalid</c>. Both hosts call this,
    ///     so the CLI and the analyzer agree on every kind.
    /// </summary>
    /// <param name="kind">The code kind.</param>
    /// <param name="recordIds">The mark record ids, one entry per mark, in source order.</param>
    /// <returns>The verdict, with the twice-named id when one id appears twice.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a known value.</exception>
    /// <exception cref="ArgumentNullException">The record ids are null.</exception>
    public static MarkCount CheckCount(CodeKind kind, IReadOnlyList<string> recordIds)
    {
        if (recordIds is null)
        {
            throw new ArgumentNullException(nameof(recordIds));
        }

        var takesExactlyOne = TakesExactlyOne(kind);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in recordIds)
        {
            if (!seen.Add(id))
            {
                return new MarkCount(true, id);
            }
        }

        return takesExactlyOne && recordIds.Count > 1
            ? new MarkCount(true, null)
            : new MarkCount(false, null);
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
            case "mark-unknown":
                return "LS5002";
            case "mark-count-invalid":
                return "LS5003";
            case "mark-style-invalid":
                return "LS5004";
            case "runtime-value-drift":
                return "LS5005";
            case "partial-missing":
                return "LS5006";
            case "act-event-undeclared":
                return "LS5007";
            case "act-event-unimplemented":
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
            case "mark-unknown":
            case "mark-count-invalid":
            case "mark-style-invalid":
            case "runtime-value-drift":
            case "partial-missing":
            case "act-event-undeclared":
            case "act-event-unimplemented":
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
                return new KindRule(UseCaseTypes, true, MarkVerb.Implements);
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
            case CodeKind.FrontendRoute:
                return new KindRule(ScreenTypes, true, MarkVerb.Implements);
            case CodeKind.BrowserTest:
                return new KindRule(TestTypes, false, MarkVerb.Proves);
            case CodeKind.DomainService:
                return new KindRule(RuleTypes, true, MarkVerb.Implements);
            case CodeKind.Test:
                return new KindRule(TestTypes, false, MarkVerb.Proves);
            case CodeKind.Technical:
                return new KindRule(TechnicalImplementsTypes, false, MarkVerb.Implements);
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
