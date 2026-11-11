using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Model.Proof;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The project model: written records, read code, screens, tests, the test run,
///     computed links and completion.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed partial class ProjectModel
{
    /// <summary>Gets the consumer configuration the model was built from.</summary>
    public ConsumerConfig Config { get; init; } = null!;
    /// <summary>Gets the problems found while building the model, such as files that do not parse.</summary>
    public List<Finding> Problems { get; } = [];

    /// <summary>Gets or sets the check findings written into model.json, as the objects `model check --format json` writes; null when no check result was recorded for this model.</summary>
    public JsonArray? FindingsJson { get; set; }

    /// <summary>Gets or sets the covered findings written into model.json, as the objects `model check --format json` writes; null when no check result was recorded for this model.</summary>
    public JsonArray? CoveredJson { get; set; }
    /// <summary>Gets the written records read from the project pages, with derived records added.</summary>
    public List<Record> Records { get; } = [];
    /// <summary>Gets the explanation and document bodies, for the book and the model JSON.</summary>
    public List<PageBody> Bodies { get; } = [];
    /// <summary>Gets or sets the ids of the LiteSpec provisions in the carried release.</summary>
    public IReadOnlySet<string> Provisions { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    ///     Gets or sets the standard pages the project's stacks and extensions apply, as
    ///     <c>docs/...</c> relative paths. Empty when the carried release cannot
    ///     be read, so a provision check falls back to existence only.
    /// </summary>
    public IReadOnlySet<string> AppliedPages { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    /// <summary>
    ///     Gets or sets the provisions the project's stacks and extensions apply: every provision
    ///     on an applied page except the provisions a selected extension replaces. Empty when the
    ///     carried release cannot be read, so a provision check falls back to page membership only.
    /// </summary>
    public IReadOnlySet<string> AppliedProvisions { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    /// <summary>Gets or sets what the backend source says: handlers, events and marked elements.</summary>
    public CodeFacts Code { get; set; } = new();
    /// <summary>Gets the route screens the frontend readers found.</summary>
    public List<CodeScreen> Screens { get; } = [];
    /// <summary>Gets the acceptance scenarios the test readers found.</summary>
    public List<CodeScenarioTest> Tests { get; } = [];

    /// <summary>Gets the browser tests every frontend reader found.</summary>
    public List<CodeBrowserTest> BrowserTests { get; } = [];

    /// <summary>Gets or sets a value indicating whether whether the TypeScript reader's handshake matched, so it is qualified.</summary>
    public bool TypeScriptQualified { get; set; }

    /// <summary>Gets the journey steps a passing test exercised, as <c>journey/&lt;name&gt;#&lt;step&gt;</c> ids.</summary>
    public SortedSet<string> ExercisedSteps { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets the generated-client operations each steps method calls, keyed by class and method.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> StepCalls { get; set; } =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
    /// <summary>Gets or sets what each steps method requests and asserts, keyed by class and method.</summary>
    public IReadOnlyDictionary<string, StepFacts> StepFacts { get; set; } =
        new Dictionary<string, StepFacts>(StringComparer.Ordinal);

    /// <summary>Gets or sets the probe tables of steps classes: per class, each argument key mapped to the route it requests.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ProbeRoutes { get; set; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
    /// <summary>Gets the comments in the test files.</summary>
    public List<CodeComment> TestComments { get; } = [];
    /// <summary>Gets the comments in the screen source files.</summary>
    public List<CodeComment> ScreenComments { get; } = [];
    /// <summary>Gets or sets the latest test run read from the result files, or null when none was read.</summary>
    public TestRun? TestRun { get; set; }
    /// <summary>Gets the computed implements links from code elements to records.</summary>
    public List<ImplementsLink> Implements { get; } = [];
    /// <summary>Gets the computed follows links from code elements to the provisions, decisions or exceptions that shape them.</summary>
    public List<FollowsLink> Follows { get; } = [];
    /// <summary>Gets the computed calls links from screens to use cases.</summary>
    public List<CallsLink> Calls { get; } = [];
    /// <summary>Gets the computed proves links from tests to the records they claim.</summary>
    public List<ProvesLink> Proves { get; } = [];

    /// <summary>Gets refusal codes by the class whose literal carries them.</summary>
    public Dictionary<string, HashSet<string>> RefusalCodes { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets per requirement, use case, journey, feature and goal, by id: the computed completion stage, proof state, holding children and counts.</summary>
    public Dictionary<string, Completion> Completion { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets per screen record, by id: the screenshot captures the gallery holds for its route.</summary>
    public Dictionary<string, IReadOnlyList<ScreenCapture>> Captures { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets per screen record, by id: captured, route-not-captured, or no-route.</summary>
    public Dictionary<string, string> CaptureState { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets the semantic hash each scenario had when a run first passed its proof.</summary>
    public ProofLedger Ledger { get; set; } = ProofLedger.Empty();

    /// <summary>Gets per aggregate, by id: the states some transition leads into, or that start its lifecycle.</summary>
    public Dictionary<string, IReadOnlySet<string>> ReachableStates { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets per aggregate, by id, plus <c>kernel</c>: the unmarked Domain types its folder holds, as <c>path:name</c>.</summary>
    public Dictionary<string, List<string>> Constituents { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets results of measured checks read from tool result files.</summary>
    public List<MeasureResult> Measures { get; } = [];

    /// <summary>Gets screen operations that resolve to no endpoint.</summary>
    public List<UnmatchedCall> UnmatchedCalls { get; } = [];

    /// <summary>Gets records by id.</summary>
    public IReadOnlyDictionary<string, Record> ById =>
        _byId ??= Records
            .Where(record => record.Id.Length > 0)
            .GroupBy(record => record.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    /// <summary>The cached records-by-id index, built on first use.</summary>
    private IReadOnlyDictionary<string, Record>? _byId;


    /// <summary>Gets use-case records by the name their identifier ends in, the first one when names repeat.</summary>
    public IReadOnlyDictionary<string, Record> UseCasesByName
    {
        get
        {
            if (_useCasesByName is null)
            {
                var index = new Dictionary<string, Record>(StringComparer.Ordinal);

                foreach (var record in ById.Values)
                {
                    if (record.Type == RecordTypes.UseCase && Identifiers.TrySplit(record.Id, out _, out _, out var name))
                    {
                        index.TryAdd(name, record);
                    }
                }

                _useCasesByName = index;
            }

            return _useCasesByName;
        }
    }

    /// <summary>The cached use cases by name index, built on first use.</summary>
    private IReadOnlyDictionary<string, Record>? _useCasesByName;

    /// <summary>Gets aggregate records by the folder their file sits in, the first by id when a folder holds several.</summary>
    public IReadOnlyDictionary<string, Record> AggregatesByFolder
    {
        get
        {
            if (_aggregatesByFolder is null)
            {
                var index = new Dictionary<string, Record>(StringComparer.Ordinal);

                foreach (var record in Records)
                {
                    if (record.Type != RecordTypes.Aggregate)
                    {
                        continue;
                    }

                    var folder = Path.GetDirectoryName(record.File) ?? "";

                    if (!index.TryGetValue(folder, out var held) || string.CompareOrdinal(record.Id, held.Id) < 0)
                    {
                        index[folder] = record;
                    }
                }

                _aggregatesByFolder = index;
            }

            return _aggregatesByFolder;
        }
    }

    /// <summary>Gets the ids of the use cases a reaction or job runs.</summary>
    public IReadOnlySet<string> BackgroundRunIds
    {
        get
        {
            if (_backgroundRunIds is null)
            {
                var index = new HashSet<string>(StringComparer.Ordinal);

                foreach (var record in Records)
                {
                    if (record.Type is RecordTypes.Reaction or RecordTypes.Job)
                    {
                        index.UnionWith(record.EdgeIds(RecordFields.Runs));
                    }
                }

                _backgroundRunIds = index;
            }

            return _backgroundRunIds;
        }
    }

    /// <summary>The cached background run ids, built on first use.</summary>
    private HashSet<string>? _backgroundRunIds;

    /// <summary>Gets, by use case id, the use cases that run it in a runs effect.</summary>
    public ILookup<string, Record> UseCaseRunners =>
        _useCaseRunners ??= Records
            .Where(record => record.Type == RecordTypes.UseCase)
            .SelectMany(record => record.EdgeIds(RecordFields.Runs).Distinct(StringComparer.Ordinal), (record, id) => (Id: id, Runner: record))
            .ToLookup(entry => entry.Id, entry => entry.Runner, StringComparer.Ordinal);

    /// <summary>The cached use case runners, built on first use.</summary>
    private ILookup<string, Record>? _useCaseRunners;

    /// <summary>Gets the records by the home their identifier names (<c>place-order</c> for <c>scenario/place-order.paid</c>), in model order.</summary>
    public ILookup<string, Record> RecordsByHome =>
        _recordsByHome ??= Records
            .Select(record => (Record: record, Home: Identifiers.TrySplit(record.Id, out _, out var home, out _) ? home : null))
            .Where(entry => entry.Home is not null)
            .ToLookup(entry => entry.Home!, entry => entry.Record, StringComparer.Ordinal);

    /// <summary>The cached records by home, built on first use.</summary>
    private ILookup<string, Record>? _recordsByHome;

    /// <summary>Gets the records by the name their identifier ends in (<c>paid</c> for <c>scenario/place-order.paid</c>), in model order.</summary>
    public ILookup<string, Record> RecordsByName =>
        _recordsByName ??= Records
            .Select(record => (Record: record, Name: Identifiers.TrySplit(record.Id, out _, out _, out var name) ? name : null))
            .Where(entry => entry.Name is not null)
            .ToLookup(entry => entry.Name!, entry => entry.Record, StringComparer.Ordinal);

    /// <summary>The cached records by name, built on first use.</summary>
    private ILookup<string, Record>? _recordsByName;

    /// <summary>The cached aggregates by folder index, built on first use.</summary>
    private IReadOnlyDictionary<string, Record>? _aggregatesByFolder;
}
