namespace LiteSpec.Cli.Book.Lexicon;

/// <summary>
///     The field labels by field name, seeded from the book's field-label table.
///     A name that means different things on different record types carries a
///     typed entry of the form record-type dot field that wins over the plain
///     entry of the same field, so each record type reads in its own context.
/// </summary>
/// <follows>litespec/core/model#name-book-labels-with-litespec-terms</follows>
static class BookLexiconFieldLabels
{
    /// <summary>Gets the field labels by field name.</summary>
    public static IReadOnlyDictionary<string, string> Labels { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["boundary"] = "Boundary", ["excludes"] = "Excludes", ["users"] = "Users", ["audience"] = "Audience", ["requires"] = "Requires", ["movedBy"] = "Moved by", ["ifExhausted"] = "If exhausted", ["exemptBecause"] = "Exempt because", ["boundBy"] = "Bound by", ["widenedBy"] = "Widened by", ["parts"] = "Parts", ["solves"] = "Solves", ["observations"] = "Observations", ["outcomes"] = "Outcomes",
            ["source"] = "Source", ["authority"] = "Authority", ["applies"] = "Applies", ["appliesBecause"] = "Applies because", ["basedOn"] = "Based on",
            ["question"] = "Question", ["sources"] = "Sources", ["readOn"] = "Read on", ["conclusion"] = "Conclusion",
            ["options"] = "Options", ["choice"] = "Choice", ["pros"] = "Pros", ["cons"] = "Cons", ["reopenedWhen"] = "Reopened when", ["supersedes"] = "Supersedes",
            ["kind"] = "Kind", ["statement"] = "Statement", ["rationale"] = "Rationale", ["problemType"] = "Problem type",
            ["measure"] = "Measure", ["target"] = "Target", ["targetKind"] = "Target kind", ["dueBy"] = "Due by", ["values"] = "Values", ["effectiveFrom"] = "Effective from",
            ["conditions"] = "Conditions", ["check"] = "Check", ["runsOn"] = "Runs on", ["runsAs"] = "Runs as", ["fieldSources"] = "Field sources", ["tracksState"] = "Tracks state", ["ignored"] = "Ignored", ["evidence"] = "Evidence",
            ["except"] = "Except", ["condition"] = "Condition", ["fallback"] = "Fallback", ["setBy"] = "Set by", ["risk"] = "Risk",
            ["scope"] = "Scope", ["selection"] = "Selection", ["code"] = "Code", ["codes"] = "Codes", ["meaning"] = "Meaning", ["recorded"] = "Recorded",
            ["startedWhen"] = "Started when", ["finishedWhen"] = "Finished when", ["actor"] = "Actor", ["dependsOn"] = "Depends on", ["branches"] = "Branches",
            ["summary"] = "Summary", ["states"] = "States", ["transitions"] = "Transitions", ["data"] = "Data",
            ["dataClasses"] = "Data classes", ["retentionHolds"] = "Retention holds", ["steps"] = "Steps", ["cursor"] = "Cursor",
            ["governs"] = "Governs",
            ["refusal"] = "Refusal",
            ["inputs"] = "Inputs", ["outputs"] = "Outputs", ["origin"] = "Origin", ["paging"] = "Paging",
            ["writes"] = "Writes", ["reads"] = "Reads", ["consistency"] = "Consistency", ["effects"] = "Effects",
            ["idempotency"] = "Idempotency", ["audit"] = "Audit", ["level"] = "Level", ["access"] = "Access", ["computedBy"] = "Computed by",
            ["triggeredBy"] = "Triggered by", ["skippedWhen"] = "Skipped when", ["compensates"] = "Compensates", ["runs"] = "Runs", ["sends"] = "Sends", ["publishes"] = "Publishes",
            ["calls"] = "Calls", ["queries"] = "Queries", ["forEach"] = "For each", ["onFailure"] = "On failure", ["onExhausted"] = "On exhausted",
            ["mode"] = "Mode", ["within"] = "Within", ["useCase"] = "Use case", ["operation"] = "Operation", ["event"] = "Event",
            ["retry"] = "Retry", ["maxAttempts"] = "Max attempts", ["initialDelay"] = "Initial delay", ["maxDelay"] = "Max delay",
            ["backoff"] = "Backoff", ["deadLetter"] = "Dead letter", ["after"] = "After", ["action"] = "Action",
            ["surfacesIn"] = "Surfaces in", ["runbook"] = "Runbook", ["every"] = "Every", ["cron"] = "Cron", ["manual"] = "Manual",
            ["outcome"] = "Outcome",
            ["given"] = "Given", ["when"] = "When", ["then"] = "Then", ["ends"] = "Ends", ["instead"] = "Instead", ["examples"] = "Examples",
            ["carries"] = "Carries", ["fields"] = "Fields", ["path"] = "Path", ["contentType"] = "Content type", ["cacheControl"] = "Cache control", ["versioning"] = "Versioning", ["servedContent"] = "Served content",
            ["route"] = "Route", ["entryPoint"] = "Entry point", ["offers"] = "Offers", ["implements"] = "Implements",
            ["receives"] = "Receives", ["failure"] = "Failure", ["operations"] = "Operations", ["timeout"] = "Timeout", ["onTimeout"] = "On timeout",
            ["erasure"] = "Erasure", ["acceptedBy"] = "Accepted by",
            ["keyedBy"] = "Keyed by", ["from"] = "From", ["accumulate"] = "Accumulate", ["erasedBy"] = "Erased by", ["removedBy"] = "Removed by", ["freshness"] = "Freshness", ["decidedBy"] = "Decided by",
            ["eventType"] = "Event type", ["subject"] = "Subject", ["direction"] = "Direction",
            ["dataschema"] = "Dataschema", ["type"] = "Type", ["required"] = "Required", ["identifies"] = "Identifies", ["items"] = "Items", ["name"] = "Name", ["label"] = "Label",
            ["amount"] = "Amount", ["currency"] = "Currency", ["purpose"] = "Purpose", ["period"] = "Period", ["classification"] = "Classification",
            ["exercises"] = "Exercises", ["authentication"] = "Authentication", ["grant"] = "Grant", ["grantedTo"] = "Granted to", ["tenant"] = "Tenant", ["tenantReason"] = "Tenant reason", ["represents"] = "Represents", ["vendor"] = "Vendor",
            ["trust"] = "Trust", ["correlation"] = "Correlation", ["translates"] = "Translates",
            ["template"] = "Template", ["channels"] = "Channels", ["via"] = "Via", ["consent"] = "Consent", ["frequency"] = "Frequency",
            ["deduplicationKey"] = "Deduplication key", ["parameters"] = "Parameters", ["locale"] = "Locale", ["suppressedWhen"] = "Suppressed when", ["claims"] = "Claims",
            ["schedule"] = "Schedule", ["batchSize"] = "Batch size", ["lease"] = "Lease", ["serviceLevel"] = "Service level",
            ["runsWhen"] = "Runs when", ["guard"] = "Guard",
            ["default"] = "Default", ["range"] = "Range", ["unit"] = "Unit", ["outOfRange"] = "Out of range", ["secret"] = "Secret", ["restart"] = "Restart",
            ["releasedBy"] = "Released by", ["frozenWhen"] = "Frozen when", ["history"] = "History",
            ["value"] = "Value", ["trigger"] = "Trigger", ["severity"] = "Severity",
            ["reason"] = "Reason",
            ["covers"] = "Covers", ["waives"] = "Waives", ["appliesTo"] = "Applies to", ["expiresOn"] = "Expires on", ["paths"] = "Paths", ["closedBy"] = "Closed by",
            ["term"] = "Term", ["labels"] = "Labels", ["rejected"] = "Rejected", ["aliases"] = "Aliases", ["layer"] = "Layer",
            ["broader"] = "Broader", ["narrower"] = "Narrower", ["mannered"] = "Mannered",
            ["affects"] = "Affects", ["affectsSegment"] = "Affects segment", ["costToday"] = "Cost today", ["workaroundToday"] = "Workaround today",
            ["evidenceStrength"] = "Evidence strength", ["claim"] = "Claim", ["captures"] = "Captures", ["captureState"] = "Capture state", ["completion"] = "Completion",
            ["offering"] = "Offering", ["owner"] = "Owner",
            ["consequences"] = "Consequences",
            ["refusals"] = "Refusals", ["delivery"] = "Delivery",
            ["impact"] = "Impact", ["prerequisites"] = "Prerequisites", ["procedure"] = "Procedure", ["verification"] = "Verification",
            ["recovery"] = "Recovery", ["escalation"] = "Escalation", ["exitWhen"] = "Exit when", ["key"] = "Key", ["enforcement"] = "Enforcement", ["configurationPath"] = "Configuration path",
            ["brand"] = "Brand", ["density"] = "Density", ["voice"] = "Voice",
            ["leadsTo"] = "Leads to", ["message"] = "Message", ["translations"] = "Translations",
            ["failureCases"] = "Failure cases", ["workaround"] = "Workaround", ["cost"] = "Cost", ["resolutionCriteria"] = "Resolution criteria",
            ["obligations"] = "Obligations", ["productEffect"] = "Effect on the product",
            ["findings"] = "Findings", ["openQuestions"] = "Open questions", ["note"] = "Note", ["successCriteria"] = "Success criteria",
            ["responsibilities"] = "Responsibilities", ["knowledge"] = "Knowledge", ["context"] = "Context",
            ["edgeCases"] = "Edge cases", ["compatibility"] = "Compatibility", ["session"] = "Session", ["reauthenticateWhen"] = "Reauthenticate when",
            ["job"] = "Job", ["archetype"] = "Archetype", ["consentRule"] = "Consent rule", ["recentSignInWindow"] = "Recent sign-in window",
            ["timeZone"] = "Time zone", ["variesBy"] = "Varies by", ["party"] = "Party", ["observedOn"] = "Observed on", ["occursOn"] = "Occurs on",
            ["referenceData"] = "Reference data", ["section"] = "Section", ["mcp"] = "MCP", ["navigation"] = "Navigation", ["sections"] = "Sections", ["properties"] = "Properties", ["concurrency"] = "Concurrency",
            ["constraint.scope"] = "Scope", ["role.scope"] = "Scope", ["screen.states"] = "States", ["research.findings"] = "Research findings",
            ["aggregate.states"] = "States", ["flow.carries"] = "Carries", ["domain-event.fields"] = "Fields",
        };

    /// <summary>
    ///     Reads one field under its label, trying the typed entry of the form
    ///     record-type dot field before the plain entry of the plain field name.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="recordType">The record type reading the field, or null for the plain entry.</param>
    /// <returns>The label for the field on that record type, or the field name when no entry names it.</returns>
    public static string FieldLabel(string field, string? recordType)
    {
        if (!string.IsNullOrEmpty(recordType) && !field.Contains('.', StringComparison.Ordinal)
            && Labels.TryGetValue(recordType + "." + field, out var typed))
        {
            return typed;
        }

        return Labels.TryGetValue(field, out var label) ? label : field;
    }
}
