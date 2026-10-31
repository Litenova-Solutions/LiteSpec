using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The book's model words and addresses: the navigation layers, the type and
///     link labels, the precedence order, and the URL of every page, plus the
///     entry points the index builder and the review queries keep under this name.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookModel
{
    /// <summary>The navigation layers, in display order.</summary>
    public static readonly IReadOnlyList<BookLayer> Layers =
    [
        new("Why", ["product", "goal", "constraint", "research", "decision", "topic", "requirement"]),
        new("Problems and Features", ["problem", "feature"]),
        new("People", ["actor", "role", "permission"]),
        new("Journeys", ["journey"]),
        new("How the Business Works", ["module", "aggregate", "rule", "use-case", "result", "scenario", "event", "integration-event", "reaction", "job", "notification", "setting"]),
        new("Entry Points", ["entry-point", "external-system"]),
        new("Running It", ["runbook"]),
        new("How We Build", ["exception"]),
        new("Terms", ["term"]),
    ];

    /// <summary>Gets the display label of every record type, singular and plural.</summary>
    static Dictionary<string, (string One, string Many)> TypeLabels { get; } =
        new Dictionary<string, (string One, string Many)>(StringComparer.Ordinal)
        {
            ["product"] = ("Product", "Product"),
            ["goal"] = ("Goal", "Goals"),
            ["constraint"] = ("Constraint", "Constraints"),
            ["research"] = ("Research", "Research"),
            ["decision"] = ("Decision", "Decisions"),
            ["topic"] = ("Topic", "Topics"),
            ["requirement"] = ("Requirement", "Requirements"),
            ["problem"] = ("Problem", "Problems"),
            ["feature"] = ("Feature", "Features"),
            ["actor"] = ("Actor", "Actors"),
            ["role"] = ("Role", "Roles"),
            ["permission"] = ("Permission", "Permissions"),
            ["journey"] = ("Journey", "Journeys"),
            ["module"] = ("Module", "Modules"),
            ["aggregate"] = ("Aggregate", "Aggregates"),
            ["rule"] = ("Rule", "Rules"),
            ["use-case"] = ("Use case", "Use cases"),
            ["result"] = ("Result", "Results"),
            ["scenario"] = ("Scenario", "Scenarios"),
            ["event"] = ("Event", "Events"),
            ["integration-event"] = ("Integration event", "Integration events"),
            ["reaction"] = ("Reaction", "Reactions"),
            ["job"] = ("Job", "Jobs"),
            ["notification"] = ("Notification", "Notifications"),
            ["setting"] = ("Setting", "Settings"),
            ["entry-point"] = ("Entry point", "Entry points"),
            ["external-system"] = ("External system", "External systems"),
            ["runbook"] = ("Runbook", "Runbooks"),
            ["exception"] = ("Exception", "Exceptions"),
            ["term"] = ("Term", "Terms"),
        };

    /// <summary>
    ///     The outgoing link fields, in display order with their headings. Refusal
    ///     renders as rule sentences instead, so it stays out of this list.
    /// </summary>
    public static readonly IReadOnlyList<(string Field, string Label)> LinkLabels =
    [
        ("decides", "Decides"),
        ("basedOn", "Based On"),
        ("affects", "Affects"),
        ("solves", "Solves"),
        ("serves", "Serves"),
        ("cites", "Cites"),
        ("fulfills", "Fulfills"),
        ("governs", "Governs"),
        ("computedBy", "Computed By"),
        ("raises", "Raises"),
        ("triggeredBy", "Triggered By"),
        ("runs", "Runs"),
        ("sends", "Sends"),
        ("emits", "Emits"),
        ("calls", "Calls"),
        ("binds", "Binds"),
        ("exercises", "Exercises"),
        ("changes", "Changes"),
        ("reads", "Reads"),
        ("translates", "Translates"),
        ("enforcedBy", "Enforced By"),
        ("acceptedBy", "Accepted By"),
        ("allows", "Allows"),
        ("reachedThrough", "Reached Through"),
        ("dependsOn", "Depends On"),
        ("terms", "Terms"),
        ("result", "Illustrates"),
        ("actor", "Actor"),
        ("runbook", "Runbook"),
        ("subject", "About"),
        ("replaces", "Replaces"),
        ("covers", "Covers"),
    ];

    /// <summary>The incoming link headings by field.</summary>
    public static readonly IReadOnlyDictionary<string, string> LinkInverseLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["decides"] = "Decided By",
            ["basedOn"] = "Basis For",
            ["affects"] = "Problems Affecting This",
            ["solves"] = "Solved By",
            ["serves"] = "Served By",
            ["cites"] = "Cited By",
            ["fulfills"] = "Fulfilled By",
            ["except"] = "Excepted By",
            ["fallback"] = "Fallback For",
            ["governs"] = "Governed By",
            ["computedBy"] = "Computes",
            ["raises"] = "Raised By",
            ["triggeredBy"] = "Triggered By",
            ["runs"] = "Run By",
            ["sends"] = "Sent By",
            ["emits"] = "Emitted By",
            ["calls"] = "Called By",
            ["binds"] = "Bound By",
            ["exercises"] = "Exercised By",
            ["effects"] = "Effect For",
            ["changes"] = "Changed By",
            ["reads"] = "Read By",
            ["translates"] = "Translated By",
            ["releasedBy"] = "Releases",
            ["enforcedBy"] = "Enforces",
            ["acceptedBy"] = "Accepts",
            ["dataClasses"] = "Data Class For",
            ["access"] = "Access For",
            ["allows"] = "Allowed By",
            ["reachedThrough"] = "Entry For",
            ["dependsOn"] = "Depended On By",
            ["terms"] = "Used By",
            ["result"] = "Illustrated By",
            ["actor"] = "Systems For",
            ["runbook"] = "Runbook For",
            ["subject"] = "Exceptions",
            ["replaces"] = "Replaced By",
            ["refusal"] = "Refusal For",
            ["covers"] = "Covered By",
        };

    /// <summary>The record types in precedence order.</summary>
    public static readonly IReadOnlyList<string> Precedence = ["constraint", "product", "requirement", "rule", "use-case", "result", "scenario"];

    /// <summary>Whether the model holds a test run a proof state can read.</summary>
    /// <param name="index">The model index.</param>
    /// <returns>Whether the model holds a test run.</returns>
    public static bool HasTestRun(BookIndex index) => index.TestRun is not null;

    /// <summary>Builds the index over a parsed model.json object.</summary>
    /// <param name="model">The parsed model.</param>
    /// <returns>The built index.</returns>
    public static BookIndex BuildIndex(JsonObject model) => BookIndexBuilder.BuildIndex(model);

    /// <summary>One record's tier: the tier the model states, or 1 when its record carries risk, else 3.</summary>
    /// <param name="record">The record.</param>
    /// <returns>The record's tier.</returns>
    public static int TierOf(JsonObject record) => BookReviewQueries.TierOf(record);

    /// <summary>The display label of a record type, singular or plural.</summary>
    /// <param name="type">The record type, or null.</param>
    /// <param name="plural">Whether the plural label is wanted.</param>
    /// <returns>The display label.</returns>
    public static string TypeLabel(string? type, bool plural = false) =>
        type is not null && TypeLabels.TryGetValue(type, out var labels)
            ? (plural ? labels.Many : labels.One)
            : (plural ? "Records" : "Record");

    /// <summary>The record URL: the id with "/" and "." mapped to path segments, keeping any "#step" fragment.</summary>
    /// <param name="id">The record id.</param>
    /// <returns>The record URL.</returns>
    public static string RecordUrl(string id)
    {
        var fragment = "";
        var baseId = id;
        var hash = id.IndexOf('#');
        if (hash >= 0)
        {
            baseId = id[..hash];
            fragment = id[hash..];
        }

        var slash = baseId.IndexOf('/');
        if (slash < 0)
        {
            return "index.html";
        }

        return $"{baseId[..slash]}/{baseId[(slash + 1)..].Replace(".", "/")}.html{fragment}";
    }

    /// <summary>The URL of a record type's index page.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>The type index URL.</returns>
    public static string TypeIndexUrl(string type) => $"{type}/index.html";

    /// <summary>The URL of the ballots page.</summary>
    /// <returns>The ballots URL.</returns>
    public static string BallotsUrl() => "ballots/index.html";

    /// <summary>The URL of an actor's lens page.</summary>
    /// <param name="actorId">The actor id.</param>
    /// <returns>The lens URL.</returns>
    public static string LensUrl(string actorId)
    {
        var slash = actorId.IndexOf('/');
        var name = slash < 0 ? actorId : actorId[(slash + 1)..];
        return $"lens/{name}.html";
    }

    /// <summary>The URL of a goal's lens page: goal, features, requirements, verified state.</summary>
    /// <param name="goalId">The goal id.</param>
    /// <returns>The goal URL.</returns>
    public static string GoalUrl(string goalId)
    {
        var slash = goalId.IndexOf('/');
        var name = slash < 0 ? goalId : goalId[(slash + 1)..];
        return $"goals/{name}.html";
    }

    /// <summary>The relative prefix from a page back to the site root.</summary>
    /// <param name="url">The page URL.</param>
    /// <returns>The relative prefix.</returns>
    public static string RelFor(string url)
    {
        var depth = url.Split('/').Length - 1;
        return depth <= 0 ? "" : string.Concat(Enumerable.Repeat("../", depth));
    }

    /// <summary>Splits "use-case/lending.borrow-copy" into type, home and name.</summary>
    /// <param name="id">The record id.</param>
    /// <returns>The split id.</returns>
    public static SplitId SplitId(string id)
    {
        var slash = id.IndexOf('/');
        if (slash < 0)
        {
            return new SplitId(id, null, "");
        }

        var rest = id[(slash + 1)..];
        var dot = rest.IndexOf('.');
        return dot < 0
            ? new SplitId(id[..slash], null, rest)
            : new SplitId(id[..slash], rest[..dot], rest[(dot + 1)..]);
    }

    /// <summary>The title a record sorts and links under: its title, or its id when empty.</summary>
    /// <param name="record">The record.</param>
    /// <returns>The sort title.</returns>
    public static string SortTitle(JsonObject record) =>
        BookRender.IsTruthy(record["title"]) ? BookRender.Str(record["title"]) : BookRender.PropText(record, "id");

    /// <summary>One rule kind as a predicate: this rule is always true, holds across records, and so on.</summary>
    /// <param name="kind">The rule kind, or null.</param>
    /// <returns>The rule kind in words.</returns>
    public static string RuleKindWords(string? kind) =>
        kind switch
        {
            "invariant" => "is always true",
            "precondition" => "is checked before the use case runs",
            "calculation" => "is computed as stated",
            "cross-aggregate" => "holds across records",
            "atomicity" => "explains why one write covers several records",
            "reservation" => "holds something until its release",
            "commercial-data" => "freezes commercial facts as stated",
            "default" => "sets the starting value",
            { Length: > 0 } other => $"is {other}",
            _ => "",
        };

    /// <summary>One data classification in plain handling words for reviewers.</summary>
    /// <param name="classification">The data classification, or null.</param>
    /// <returns>The classification in words.</returns>
    public static string HandlingWords(string? classification) =>
        classification switch
        {
            "public" => "Visible to everyone",
            "internal" => "Stays inside the product",
            "personal" => "Exported or deleted on request",
            "sensitive" => "Exported or deleted on request",
            { Length: > 0 } other => other,
            _ => "",
        };
}
