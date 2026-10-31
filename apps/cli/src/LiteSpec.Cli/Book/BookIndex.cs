using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>The index over model.json that every page renderer reads.</summary>
sealed class BookIndex
{
    /// <summary>Every entry of the records array, including entries that are not objects.</summary>
    public List<JsonNode?> Records { get; init; } = [];

    /// <summary>Records by id; the first record wins when an id repeats.</summary>
    public Dictionary<string, JsonObject> ById { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Records by type, each list sorted by title.</summary>
    public Dictionary<string, List<JsonObject>> ByType { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Incoming record links by target id.</summary>
    public Dictionary<string, List<Backlink>> Backlinks { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Results by home module name.</summary>
    public Dictionary<string, List<JsonObject>> ResultsOf { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Scenarios by home module name.</summary>
    public Dictionary<string, List<JsonObject>> ScenariosOf { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Implements links by target record id.</summary>
    public Dictionary<string, List<JsonObject>> ImplementsByTarget { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Calls links by target record id.</summary>
    public Dictionary<string, List<JsonObject>> CallsByTarget { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Proves links by claimed record id.</summary>
    public Dictionary<string, List<JsonObject>> ProvesByClaim { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Every entry of the tests array.</summary>
    public List<JsonNode?> Tests { get; set; } = [];

    /// <summary>Tests by scenario name; the first test wins when a name repeats.</summary>
    public Dictionary<string, JsonObject> TestsByName { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Every entry of the screens array.</summary>
    public List<JsonNode?> Screens { get; set; } = [];

    /// <summary>Screens by "<c>frontend route</c>"; the last screen wins when a key repeats.</summary>
    public Dictionary<string, JsonObject> ScreensByKey { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The use-case status map, or empty when the model holds none.</summary>
    public JsonObject Status { get; set; } = new();

    /// <summary>The requirement status map, or empty when the model holds none.</summary>
    public Dictionary<string, JsonObject> RequirementStatus { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The reachable states by aggregate id, or empty when the model holds none.</summary>
    public Dictionary<string, HashSet<string>> ReachableStates { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The scenarios whose passing proof went stale, or empty when the model holds none.</summary>
    public HashSet<string> StaleIds { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The latest test run, or null when the model holds none.</summary>
    public JsonNode? TestRun { get; set; }

    /// <summary>The journey steps a passing test walked, as <c>journey/&lt;name&gt;#&lt;step&gt;</c> ids.</summary>
    public HashSet<string> ExercisedSteps { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Whether the model carries the check result the model build wrote, as findings or known.</summary>
    public bool HasFindings { get; set; }

    /// <summary>Days before a tier-3 question auto-approves; 7 when the model names none.</summary>
    public int VetoWindowDays { get; set; } = 7;

    /// <summary>The tested state by record id the builder wrote, or empty when the model holds none.</summary>
    public Dictionary<string, bool> Tested { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The proof state by scenario or rule id the builder wrote, or empty when the model holds none.</summary>
    public Dictionary<string, string> ScenarioStatus { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The review batches the builder wrote, or null when the model holds none.</summary>
    public JsonArray? StoredBatches { get; set; }

    /// <summary>The security reviewer the model names, or null when it names none.</summary>
    public string? SecurityReviewer { get; set; }
}
