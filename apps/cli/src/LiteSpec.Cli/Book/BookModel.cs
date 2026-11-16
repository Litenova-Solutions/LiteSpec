using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Book.Output;

namespace LiteSpec.Cli.Book;

/// <summary>
///     The book's model words and addresses: the navigation layers, the type labels and the
///     precedence order, plus the entry points the index builder and the review queries keep under
///     this name. Connection labels come from the carried schema, never from this file.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgments-in-the-book</follows>
static class BookModel
{
    /// <summary>The navigation layers, in display order.</summary>
    public static readonly IReadOnlyList<BookLayer> Layers =
    [
        new("Why", ["product", "goal", "vision", "constraint", "research", "decision", "requirement"]),
        new("Problems and Features", ["problem", "feature"]),
        new("People", ["actor", "role", "permission"]),
        new("Flows", ["flow"]),
        new("How the Business Works", ["module", "aggregate", "rule", "use-case", "result", "scenario", "domain-event", "module-event", "integration-event", "reaction", "job", "notification", "setting", "read-model"]),
        new("Entry Points", ["entry-point", "external-system"]),
        new("Running It", ["runbook"]),
        new("How We Build", ["waiver"]),
        new("Terms", ["term"]),
    ];

    /// <summary>Gets the display label of every record type, singular and plural.</summary>
    static Dictionary<string, (string One, string Many)> TypeLabels { get; } =
        new Dictionary<string, (string One, string Many)>(StringComparer.Ordinal)
        {
            ["product"] = ("Product", "Product"),
            ["goal"] = ("Goal", "Goals"),
            ["vision"] = ("Vision", "Visions"),
            ["constraint"] = ("Constraint", "Constraints"),
            ["research"] = ("Research", "Research"),
            ["decision"] = ("Decision", "Decisions"),
            ["requirement"] = ("Requirement", "Requirements"),
            ["problem"] = ("Problem", "Problems"),
            ["feature"] = ("Feature", "Features"),
            ["actor"] = ("Actor", "Actors"),
            ["role"] = ("Role", "Roles"),
            ["permission"] = ("Permission", "Permissions"),
            ["flow"] = ("Flow", "Flows"),
            ["module"] = ("Module", "Modules"),
            ["aggregate"] = ("Aggregate", "Aggregates"),
            ["rule"] = ("Rule", "Rules"),
            ["use-case"] = ("Use case", "Use cases"),
            ["result"] = ("Result", "Results"),
            ["scenario"] = ("Scenario", "Scenarios"),
            ["domain-event"] = ("Domain event", "Domain events"),
            ["module-event"] = ("Module event", "Module events"),
            ["integration-event"] = ("Integration event", "Integration events"),
            ["reaction"] = ("Reaction", "Reactions"),
            ["job"] = ("Job", "Jobs"),
            ["read-model"] = ("Read model", "Read models"),
            ["notification"] = ("Notification", "Notifications"),
            ["setting"] = ("Setting", "Settings"),
            ["entry-point"] = ("Entry point", "Entry points"),
            ["external-system"] = ("External system", "External systems"),
            ["runbook"] = ("Runbook", "Runbooks"),
            ["waiver"] = ("Waiver", "Waivers"),
            ["term"] = ("Term", "Terms"),
        };

    /// <summary>The record types in precedence order.</summary>
    public static readonly IReadOnlyList<string> Precedence = ["constraint", "product", "requirement", "rule", "use-case", "result", "scenario"];

    /// <summary>Whether the model holds a test run a proof state can read.</summary>
    /// <param name="index">The model index.</param>
    /// <returns>Whether the model holds a test run.</returns>
    public static bool HasTestRun(BookIndex index) => index.TestRun is not null;

    /// <summary>Builds the index over a parsed model object.</summary>
    /// <param name="model">The parsed model.</param>
    /// <returns>The built index.</returns>
    public static BookIndex BuildIndex(JsonObject model) => BookIndexBuilder.BuildIndex(model);

    /// <summary>The display label of a record type, singular or plural.</summary>
    /// <param name="type">The record type, or null.</param>
    /// <param name="plural">Whether the plural label is wanted.</param>
    /// <returns>The display label.</returns>
    public static string TypeLabel(string? type, bool plural = false) =>
        type is not null && TypeLabels.TryGetValue(type, out var labels)
            ? (plural ? labels.Many : labels.One)
            : (plural ? "Records" : "Record");

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
}
