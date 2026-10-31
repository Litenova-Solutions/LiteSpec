namespace LiteSpec.Book;

/// <summary>
///     The field labels by field name, seeded from the book's field-label table.
/// </summary>
/// <follows>litespec/core/model#name-book-labels-with-litespec-terms</follows>
static class BookLexiconFieldLabels
{
    /// <summary>Gets the field labels by field name.</summary>
    public static IReadOnlyDictionary<string, string> Labels { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["boundary"] = "Boundary", ["exclusions"] = "Exclusions", ["addresses"] = "Addresses",
            ["source"] = "Source", ["provenance"] = "Provenance", ["authority"] = "Authority", ["applies"] = "Applies", ["appliesBecause"] = "Applies because",
            ["question"] = "Question", ["sources"] = "Sources", ["readOn"] = "Read on", ["conclusion"] = "Conclusion",
            ["options"] = "Options", ["choice"] = "Choice", ["pros"] = "Pros", ["cons"] = "Cons", ["reopenedWhen"] = "Reopened when",
            ["kind"] = "Kind", ["statement"] = "Statement", ["rationale"] = "Rationale",
            ["measure"] = "Measure", ["target"] = "Target", ["dueBy"] = "Due by", ["plannedFor"] = "Planned for", ["values"] = "Values", ["effectiveFrom"] = "Effective from",
            ["conditions"] = "Conditions", ["check"] = "Check", ["runsOn"] = "Runs on", ["evidence"] = "Evidence",
            ["except"] = "Except", ["condition"] = "Condition", ["fallback"] = "Fallback", ["setBy"] = "Set by", ["risk"] = "Risk",
            ["lifecycle"] = "Lifecycle", ["refusedBecause"] = "Refused because",
            ["scope"] = "Scope", ["selection"] = "Selection", ["code"] = "Code", ["codes"] = "Codes", ["meaning"] = "Meaning", ["recorded"] = "Recorded",
            ["finishedWhen"] = "Finished when", ["actor"] = "Actor", ["dependsOn"] = "Depends on",
            ["summary"] = "Summary", ["states"] = "States", ["transitions"] = "Transitions", ["data"] = "Data",
            ["dataClasses"] = "Data classes", ["steps"] = "Steps", ["exits"] = "Exits", ["drivenBy"] = "Driven by", ["cursor"] = "Cursor",
            ["reaction"] = "Reaction", ["job"] = "Job", ["person"] = "Person", ["dueWithin"] = "Due within", ["binds"] = "Binds", ["compensation"] = "Compensation",
            ["refusal"] = "Refusal", ["questions"] = "Questions", ["recommendation"] = "Recommendation",
            ["input"] = "Input", ["output"] = "Output", ["origin"] = "Origin", ["paging"] = "Paging", ["builtAsynchronously"] = "Built asynchronously",
            ["changes"] = "Changes", ["reads"] = "Reads", ["consistency"] = "Consistency", ["effects"] = "Effects",
            ["idempotency"] = "Idempotency", ["audit"] = "Audit", ["level"] = "Level", ["access"] = "Access", ["computedBy"] = "Computed by",
            ["triggeredBy"] = "Triggered by", ["skippedWhen"] = "Skipped when", ["runs"] = "Runs", ["sends"] = "Sends", ["emits"] = "Emits",
            ["calls"] = "Calls", ["forEach"] = "For each", ["onFailure"] = "On failure", ["onExhausted"] = "On exhausted",
            ["mode"] = "Mode", ["within"] = "Within", ["useCase"] = "Use case", ["operation"] = "Operation", ["event"] = "Event",
            ["retry"] = "Retry", ["maxAttempts"] = "Max attempts", ["initialDelay"] = "Initial delay", ["maxDelay"] = "Max delay",
            ["backoff"] = "Backoff", ["deadLetter"] = "Dead letter", ["after"] = "After", ["action"] = "Action",
            ["surfacesIn"] = "Surfaces in", ["runbook"] = "Runbook", ["every"] = "Every", ["cron"] = "Cron", ["manual"] = "Manual",
            ["outcome"] = "Outcome",
            ["given"] = "Given", ["when"] = "When", ["then"] = "Then", ["examples"] = "Examples",
            ["carries"] = "Carries", ["path"] = "Path", ["contentType"] = "Content type", ["cacheControl"] = "Cache control", ["versioning"] = "Versioning",
            ["address"] = "Address", ["entryPoint"] = "Entry point", ["floorplan"] = "Floorplan", ["offers"] = "Offers", ["implements"] = "Implements",
            ["receives"] = "Receives", ["failure"] = "Failure", ["operations"] = "Operations", ["timeout"] = "Timeout", ["onTimeout"] = "On timeout",
            ["erasure"] = "Erasure", ["acceptedBy"] = "Accepted by",
            ["direction"] = "Direction", ["eventType"] = "Event type", ["subject"] = "Subject", ["payload"] = "Payload",
            ["dataschema"] = "Dataschema", ["type"] = "Type", ["required"] = "Required", ["of"] = "Of", ["name"] = "Name", ["label"] = "Label",
            ["amount"] = "Amount", ["currency"] = "Currency", ["purpose"] = "Purpose", ["period"] = "Period", ["classification"] = "Classification",
            ["exercises"] = "Exercises", ["background"] = "Background", ["auth"] = "Auth", ["grant"] = "Grant", ["tenant"] = "Tenant",
            ["trust"] = "Trust", ["correlation"] = "Correlation", ["translates"] = "Translates",
            ["template"] = "Template", ["channel"] = "Channel", ["consent"] = "Consent", ["frequency"] = "Frequency",
            ["dedupKey"] = "Dedup key", ["params"] = "Params", ["locale"] = "Locale", ["suppressedWhen"] = "Suppressed when", ["claims"] = "Claims",
            ["schedule"] = "Schedule", ["batchSize"] = "Batch size", ["lease"] = "Lease", ["serviceLevel"] = "Service level",
            ["runsWhere"] = "Runs where", ["guard"] = "Guard",
            ["valueType"] = "Value type", ["default"] = "Default", ["range"] = "Range", ["unit"] = "Unit", ["atBound"] = "At bound", ["secret"] = "Secret", ["restart"] = "Restart",
            ["releasedBy"] = "Released by", ["frozenWhen"] = "Frozen when", ["history"] = "History",
            ["value"] = "Value", ["trigger"] = "Trigger", ["severity"] = "Severity",
            ["reason"] = "Reason", ["reviewBy"] = "Review by",
            ["covers"] = "Covers", ["expiresOn"] = "Expires on", ["paths"] = "Paths",
            ["term"] = "Term", ["labels"] = "Labels", ["rejected"] = "Rejected", ["aliases"] = "Aliases", ["layer"] = "Layer",
            ["broader"] = "Broader", ["narrower"] = "Narrower", ["mannered"] = "Mannered",
            ["decider"] = "Decider", ["decideBy"] = "Decide by", ["owner"] = "Owner",
            ["approvedBy"] = "Approved by", ["approvedOn"] = "Approved on", ["approvedHash"] = "Approval hash",
        };
}
