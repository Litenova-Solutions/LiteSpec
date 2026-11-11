using LiteSpec.Model;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The model's records and links indexed once for one computation, so each
///     completion reads a lookup instead of scanning every record or link. Every
///     list keeps the order a scan of the model gives.
/// </summary>
sealed class CompletionLookups
{
    /// <summary>The empty record list a lookup returns for a key with no records.</summary>
    static readonly List<Record> NoRecords = [];

    /// <summary>The empty link list a lookup returns for a claim with no proof.</summary>
    static readonly List<ProvesLink> NoLinks = [];

    /// <summary>The records of each type, in id order.</summary>
    readonly Dictionary<string, List<Record>> byType;

    /// <summary>The written scenarios by the use-case name their identifier is homed at, in id order.</summary>
    readonly Dictionary<string, List<Record>> scenariosByHome;

    /// <summary>The Behavior records fulfilling each requirement, in model order.</summary>
    readonly Dictionary<string, List<Record>> fulfilling;

    /// <summary>The implementation decisions based on each record, in model order.</summary>
    readonly Dictionary<string, List<Record>> decisionsBasedOn;

    /// <summary>The records serving each journey step, in model order.</summary>
    readonly Dictionary<string, List<Record>> servers;

    /// <summary>The proves links by the record they claim, in model order.</summary>
    readonly Dictionary<string, List<ProvesLink>> provesByClaim;

    /// <summary>For each proving test name or title, whether the test asserts something; the first test with a name wins.</summary>
    readonly Dictionary<string, bool> asserting;

    /// <summary>Gets the built project model.</summary>
    public ProjectModel Model { get; }

    /// <summary>Gets the ids that a confirmed, passing proof claims.</summary>
    public HashSet<string> ProvenPassing { get; }

    /// <summary>Gets the ids an implements link reaches.</summary>
    public HashSet<string> Implemented { get; }

    /// <summary>Gets the ids a follows link reaches.</summary>
    public HashSet<string> Followed { get; }

    /// <summary>Gets the stage of each journey step worked out so far.</summary>
    public Dictionary<string, string> StepStages { get; } = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="CompletionLookups"/> class, indexing the model's records and links once.</summary>
    /// <param name="model">The built project model, with its links, tests and ledger read.</param>
    public CompletionLookups(ProjectModel model)
    {
        Model = model;
        byType = RecordsByType(model);
        scenariosByHome = ScenariosByHome(model);
        fulfilling = Citing(model, RecordTypes.FulfillingTypes, RecordFields.Fulfills);
        decisionsBasedOn = Citing(
            model,
            [RecordTypes.Decision],
            RecordFields.BasedOn,
            record => Checker.UnderImplementationDecisions(model.Config, record.File));
        servers = Citing(model, RecordTypes.StepServingTypes, RecordFields.Serves);
        provesByClaim = model.Proves
            .GroupBy(link => link.Claim, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        asserting = AssertingTests(model);
        ProvenPassing = model.Proves
            .Where(link => link.Confirmed && link.Passing == true)
            .Select(link => link.Claim)
            .ToHashSet(StringComparer.Ordinal);
        Implemented = model.Implements.Select(link => link.To).ToHashSet(StringComparer.Ordinal);
        Followed = model.Follows.Select(link => link.To).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The records of one type, in id order.</summary>
    /// <param name="type">The record type to list.</param>
    /// <returns>The records of the type, in id order.</returns>
    public List<Record> OfType(string type) =>
        byType.TryGetValue(type, out var records) ? records : NoRecords;

    /// <summary>The written scenarios of a use case, by identifier home, in id order.</summary>
    /// <param name="useCase">The use case whose scenarios are wanted.</param>
    /// <returns>The scenario records the use case holds, in id order.</returns>
    public List<Record> ScenariosOf(Record useCase) =>
        Identifiers.TrySplit(useCase.Id, out _, out _, out var name)
        && scenariosByHome.TryGetValue(name, out var scenarios)
            ? scenarios
            : NoRecords;

    /// <summary>The Behavior records fulfilling a requirement, with a technical decision serving a quality requirement.</summary>
    /// <param name="requirement">The requirement whose fulfillers are wanted.</param>
    /// <returns>The fulfilling records in id order.</returns>
    public List<Record> FulfillersOf(Record requirement)
    {
        var fulfillers = new List<Record>(fulfilling.TryGetValue(requirement.Id, out var found) ? found : NoRecords);

        if (requirement.Field(RecordFields.Kind) == "quality"
            && decisionsBasedOn.TryGetValue(requirement.Id, out var decisions))
        {
            fulfillers.AddRange(decisions);
        }

        return fulfillers.OrderBy(record => record.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>The records serving a journey step.</summary>
    /// <param name="step">The journey step id.</param>
    /// <returns>The records serving the step, in model order.</returns>
    public List<Record> ServersOf(string step) =>
        servers.TryGetValue(step, out var found) ? found : NoRecords;

    /// <summary>The proves links that claim a record.</summary>
    /// <param name="claim">The claimed record id.</param>
    /// <returns>The links claiming the record, in model order.</returns>
    public List<ProvesLink> ProvesOf(string claim) =>
        provesByClaim.TryGetValue(claim, out var links) ? links : NoLinks;

    /// <summary>
    ///     Whether the proving test asserts something: a backend test with an assertion
    ///     call, or a steps test with a then step asserting through its steps class, and
    ///     a browser test with an asserted text or code.
    /// </summary>
    /// <param name="scenario">The proving test's name or title.</param>
    /// <returns>True when the proving test asserts something.</returns>
    public bool TestAsserts(string scenario) =>
        asserting.TryGetValue(scenario, out var asserts) && asserts;

    /// <summary>The records of each type, in id order.</summary>
    /// <param name="model">The built project model.</param>
    /// <returns>The records by type.</returns>
    static Dictionary<string, List<Record>> RecordsByType(ProjectModel model) =>
        model.Records
            .GroupBy(record => record.Type, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(record => record.Id, StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);

    /// <summary>The written scenarios by the use-case name their identifier is homed at, in id order.</summary>
    /// <param name="model">The built project model.</param>
    /// <returns>The scenarios by home.</returns>
    static Dictionary<string, List<Record>> ScenariosByHome(ProjectModel model)
    {
        var byHome = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Type == RecordTypes.Scenario
                && !record.Derived
                && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                && home is not null)
            {
                Add(byHome, home, record);
            }
        }

        foreach (var scenarios in byHome.Values)
        {
            var ordered = scenarios.OrderBy(record => record.Id, StringComparer.Ordinal).ToList();

            scenarios.Clear();
            scenarios.AddRange(ordered);
        }

        return byHome;
    }

    /// <summary>The records of the given types by each id one of their fields names, each record once per id, in model order.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="types">The record types that cite.</param>
    /// <param name="field">The field holding the cited ids.</param>
    /// <param name="include">An extra condition a citing record meets, or null for none.</param>
    /// <returns>The citing records by cited id.</returns>
    static Dictionary<string, List<Record>> Citing(
        ProjectModel model, IReadOnlyList<string> types, string field, Func<Record, bool>? include = null)
    {
        var byTarget = new Dictionary<string, List<Record>>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (!types.Contains(record.Type, StringComparer.Ordinal) || (include is not null && !include(record)))
            {
                continue;
            }

            foreach (var target in record.Ids(field).Distinct(StringComparer.Ordinal))
            {
                Add(byTarget, target, record);
            }
        }

        return byTarget;
    }

    /// <summary>For each proving test name or title, whether the test asserts something; backend tests first, then browser tests.</summary>
    /// <param name="model">The built project model.</param>
    /// <returns>Whether each named test asserts something.</returns>
    static Dictionary<string, bool> AssertingTests(ProjectModel model)
    {
        var asserting = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var test in model.Tests)
        {
            asserting.TryAdd(test.Name, test.HasAssertion || test.Steps.Any(step => step.Phase == "then"));
        }

        foreach (var browser in model.BrowserTests)
        {
            asserting.TryAdd(browser.Title, browser.Texts.Count > 0 || browser.Codes.Count > 0);
        }

        return asserting;
    }

    /// <summary>Adds a record to the list a key holds, starting the list when the key is new.</summary>
    /// <param name="index">The index to add to.</param>
    /// <param name="key">The key.</param>
    /// <param name="record">The record to add.</param>
    static void Add(Dictionary<string, List<Record>> index, string key, Record record)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = [];
            index[key] = list;
        }

        list.Add(record);
    }
}
