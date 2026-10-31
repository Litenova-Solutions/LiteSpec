using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     The project model: written records, read code, screens, tests, the test run,
///     computed links and per use case status.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class ProjectModel
{
    /// <summary>Gets the consumer configuration the model was built from.</summary>
    public ConsumerConfig Config { get; init; } = null!;
    /// <summary>Gets the problems found while building the model, such as files that do not parse.</summary>
    public List<Finding> Problems { get; } = [];

    /// <summary>Gets or sets the check findings `model build` writes into model.json, as the objects `model check --format json` writes; null when no check result was recorded for this model.</summary>
    public IReadOnlyList<object>? FindingsJson { get; set; }

    /// <summary>Gets or sets the known findings `model build` writes into model.json, as the objects `model check --format json` writes; null when no check result was recorded for this model.</summary>
    public IReadOnlyList<object>? KnownJson { get; set; }
    /// <summary>Gets the written records read from the project pages, with derived records added.</summary>
    public List<Record> Records { get; } = [];
    /// <summary>Gets the prose body of each record page, for the book and the model JSON.</summary>
    public List<PageBody> Bodies { get; } = [];
    /// <summary>Gets or sets the ids of the LiteSpec provisions in the carried release.</summary>
    public IReadOnlySet<string> Provisions { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    ///     Gets or sets the standard pages the project's stacks and extensions apply, as
    ///     <c>docs/...</c> relative paths. Empty when the carried release cannot
    ///     be read, so a provision check falls back to existence only.
    /// </summary>
    public IReadOnlySet<string> AppliedPages { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    /// <summary>Gets or sets what the backend source says: handlers, events and marked elements.</summary>
    public CodeModel Code { get; set; } = new();
    /// <summary>Gets the route screens the frontend readers found.</summary>
    public List<ScreenInfo> Screens { get; } = [];
    /// <summary>Gets the acceptance scenarios the test readers found.</summary>
    public List<ScenarioInfo> Tests { get; } = [];

    /// <summary>Gets the browser tests every frontend reader found.</summary>
    public List<BrowserTestInfo> BrowserTests { get; } = [];

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
    /// <summary>Gets the computed implemented and verified status of each use case, by use-case id.</summary>
    public Dictionary<string, UseCaseStatus> Status { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets the effective status of each record, by id, after holder and page inheritance.</summary>
    public Dictionary<string, string> EffectiveStatus { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets refusal codes by the class whose literal carries them.</summary>
    public Dictionary<string, HashSet<string>> RefusalCodes { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets per requirement, by id: verified from what fulfills it and from its measure.</summary>
    public Dictionary<string, RequirementStatus> RequirementStatus { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets per proof target, by id: whether a passing test run proves it. Only quality requirements, technical decisions, external systems, and integration events carry a tested state.</summary>
    public Dictionary<string, bool> Tested { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the scenarios whose passing proof is stale: the scenario changed after the run first passed it.</summary>
    public SortedSet<string> Stale { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets the semantic hash each scenario had when a run first passed its proof.</summary>
    public ProofLedger Ledger { get; set; } = ProofLedger.Empty();

    /// <summary>Gets per aggregate, by id: the states some transition leads into, or that start its lifecycle.</summary>
    public Dictionary<string, IReadOnlySet<string>> ReachableStates { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets per aggregate, by id, plus <c>kernel</c>: the unmarked Domain types its folder holds, as <c>path:name</c>.</summary>
    public Dictionary<string, List<string>> Constituents { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets code no scenario test reaches; empty when no scenario-only coverage file was read.</summary>
    public List<CoverageGap> UnreachedCode { get; } = [];

    /// <summary>Gets or sets a value indicating whether whether a scenario-only coverage file was read, so an empty <see cref="UnreachedCode"/> means something.</summary>
    public bool CoverageRead { get; set; }

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

    /// <summary>Writes the model as JSON.</summary>
    /// <returns>The model serialized as indented JSON.</returns>
    public string ToJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true, NewLine = "\n" };

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", 1);
            writer.WriteString("root", ".");

            writer.WriteStartArray("records");

            var bodies = new Dictionary<(string File, string? PageId), string>(Records.Count);

            foreach (var page in Bodies)
            {
                bodies[(page.File, page.PageId)] = PageText(page.Body);
            }

            foreach (var record in Records.OrderBy(left => left.Id, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("type", record.Type);
                writer.WriteString("id", record.Id);
                writer.WriteString("title", record.Title);
                WriteNullable(writer, "owner", record.Owner);
                WriteNullable(writer, "status", record.Status);
                WriteNullable(writer, "effectiveStatus", EffectiveStatus.GetValueOrDefault(record.Id));
                WriteNullable(writer, "decider", record.Decider);
                WriteNullable(writer, "decideBy", record.DecideBy);

                if (record.Derived)
                {
                    writer.WriteBoolean("derived", true);
                }

                if (record.Type == RecordTypes.UseCase)
                {
                    WriteNullable(writer, "aggregate", ModelBuilder.AggregateOf(this, record)?.Id);
                }

                writer.WriteString("file", Config.Relative(record.File));
                writer.WriteNumber("line", record.Line);

                if (bodies.TryGetValue((record.File, record.Id), out var text))
                {
                    writer.WriteString("body", text);
                }
                writer.WritePropertyName("fields");
                record.Raw.WriteTo(writer);
                writer.WritePropertyName("links");
                JsonSerializer.Serialize(writer, ModelBuilder.ResolvedLinks(this, record), options);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartObject("code");

            writer.WriteStartArray("handlers");

            foreach (var handler in Code.Handlers)
            {
                writer.WriteStartObject();
                writer.WriteString("useCase", handler.UseCaseId);
                writer.WriteString("operation", handler.Operation);
                writer.WriteString("kind", handler.Kind);
                writer.WriteString("path", Config.Relative(handler.File));
                writer.WriteString("class", handler.Class);
                writer.WriteNumber("line", handler.Line);
                writer.WriteString("messageType", handler.MessageType);
                writer.WriteNumber("dependencyCount", handler.DependencyCount);
                writer.WriteStartArray("aggregateCalls");

                foreach (var call in handler.AggregateCalls ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("receiver", call.Receiver);
                    writer.WriteString("method", call.Method);
                    writer.WriteString("path", Config.Relative(call.File));
                    writer.WriteNumber("line", call.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("collaborators");

                foreach (var collaborator in handler.Collaborators ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", collaborator.Type);
                    writer.WriteString("path", Config.Relative(collaborator.File));
                    writer.WriteNumber("line", collaborator.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("loopReads");

                foreach (var read in handler.LoopReads ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("repository", read.Repository);
                    writer.WriteString("method", read.Method);
                    writer.WriteString("path", Config.Relative(read.File));
                    writer.WriteNumber("line", read.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("memoryFilters");

                foreach (var filter in handler.MemoryFilters ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", Config.Relative(filter.File));
                    writer.WriteNumber("line", filter.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("reactions");

            foreach (var reaction in Code.Reactions)
            {
                writer.WriteStartObject();
                writer.WriteString("class", reaction.Class);
                writer.WriteString("path", Config.Relative(reaction.File));
                WriteNullable(writer, "useCase", reaction.UseCaseId);
                writer.WriteNumber("line", reaction.Line);
                writer.WriteNumber("dependencyCount", reaction.DependencyCount);
                WriteCollaborators(writer, reaction.Collaborators);
                WriteLoopReads(writer, reaction.LoopReads);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("jobs");

            foreach (var job in Code.Jobs)
            {
                writer.WriteStartObject();
                writer.WriteString("class", job.Class);
                writer.WriteString("path", Config.Relative(job.File));
                WriteNullable(writer, "useCase", job.UseCaseId);
                writer.WriteNumber("line", job.Line);
                writer.WriteNumber("dependencyCount", job.DependencyCount);
                WriteCollaborators(writer, job.Collaborators);
                WriteLoopReads(writer, job.LoopReads);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("codes");

            foreach (var literal in Code.Literals.OrderBy(left => left.Value, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("code", literal.Value);
                writer.WriteString("path", Config.Relative(literal.File));
                WriteNullable(writer, "class", literal.Class);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("aggregateMethods");

            foreach (var method in Code.AggregateMethods)
            {
                writer.WriteStartObject();
                writer.WriteString("aggregate", method.Aggregate);
                writer.WriteString("method", method.Method);
                writer.WriteStartArray("events");

                foreach (var raised in method.Events)
                {
                    writer.WriteStringValue(raised);
                }

                writer.WriteEndArray();
                writer.WriteString("path", Config.Relative(method.File));
                writer.WriteNumber("line", method.Line);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("auditActions");

            foreach (var action in Code.AuditActions)
            {
                writer.WriteStartObject();
                writer.WriteString("operation", action.Operation);
                writer.WriteString("action", action.Action);
                writer.WriteString("path", Config.Relative(action.File));
                writer.WriteNumber("line", action.Line);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("endpoints");

            foreach (var endpoint in Code.Endpoints)
            {
                writer.WriteStartObject();
                writer.WriteString("operation", endpoint.Operation);
                writer.WriteString("useCase", endpoint.UseCaseId);
                writer.WriteString("method", endpoint.Method);
                writer.WriteString("route", endpoint.Route);
                writer.WriteString("path", Config.Relative(endpoint.File));
                writer.WriteNumber("line", endpoint.Line);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("events");

            foreach (var domainEvent in Code.Events.OrderBy(left => left.Class, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("class", domainEvent.Class);
                writer.WriteString("path", Config.Relative(domainEvent.File));
                WriteNullable(writer, "event", domainEvent.EventId);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteStartArray("screens");

            foreach (var screen in Screens)
            {
                writer.WriteStartObject();
                writer.WriteString("frontend", screen.Frontend);
                writer.WriteString("route", screen.Route);
                writer.WriteString("file", screen.File);
                WriteNullable(writer, "screen", screen.ScreenId);
                writer.WriteString("reader", screen.Reader);
                writer.WriteStartArray("operations");

                foreach (var operation in screen.Operations)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", operation.Name);
                    writer.WriteString("method", operation.Method);
                    writer.WriteString("path", operation.Path);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("useCases");

                foreach (var useCase in screen.UseCases)
                {
                    writer.WriteStringValue(useCase);
                }

                writer.WriteEndArray();
                writer.WriteStartArray("calls");

                foreach (var call in screen.PairedCalls())
                {
                    writer.WriteStartObject();
                    writer.WriteString("operation", call.Operation);
                    writer.WriteString("useCase", call.UseCase);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("browserTests");

            foreach (var test in BrowserTests)
            {
                writer.WriteStartObject();
                writer.WriteString("frontend", test.Frontend);
                writer.WriteString("title", test.Title);
                writer.WriteString("file", Config.Relative(test.File));
                writer.WriteNumber("line", test.Line);
                writer.WriteStartArray("proves");

                foreach (var claim in test.Proves)
                {
                    writer.WriteStringValue(claim);
                }

                writer.WriteEndArray();
                WriteNullable(writer, "journey", test.Journey);
                writer.WriteStartArray("gotos");

                foreach (var target in test.Gotos)
                {
                    writer.WriteStringValue(target);
                }

                writer.WriteEndArray();
                writer.WriteStartArray("codes");

                foreach (var code in test.Codes)
                {
                    writer.WriteStringValue(code);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("exercisedSteps");

            foreach (var step in ExercisedSteps)
            {
                writer.WriteStringValue(step);
            }

            writer.WriteEndArray();

            writer.WriteStartArray("tests");

            foreach (var test in Tests)
            {
                writer.WriteStartObject();
                writer.WriteString("scenario", test.Name);
                writer.WriteString("class", test.Class);
                writer.WriteString("file", Config.Relative(test.File));
                writer.WriteNumber("line", test.Line);
                writer.WriteStartArray("claims");

                foreach (var claim in test.Claims)
                {
                    writer.WriteStringValue(claim);
                }

                writer.WriteEndArray();
                writer.WriteStartArray("steps");

                foreach (var step in test.Steps)
                {
                    writer.WriteStartObject();
                    writer.WriteString("phase", step.Phase);
                    writer.WriteString("receiver", step.Receiver);
                    WriteNullable(writer, "receiverType", step.ReceiverType);
                    writer.WriteString("method", step.Method);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("codes");

                foreach (var code in test.Codes)
                {
                    writer.WriteStringValue(code);
                }

                writer.WriteEndArray();

                if (TestRun is not null && TestRun.OutcomeByMethod.TryGetValue(test.Name, out var outcome))
                {
                    writer.WriteString("outcome", outcome);
                }
                else
                {
                    writer.WriteNull("outcome");
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            if (TestRun is null)
            {
                writer.WriteNull("testRun");
            }
            else
            {
                writer.WriteStartObject("testRun");
                writer.WriteStartArray("files");

                foreach (var file in TestRun.Files)
                {
                    writer.WriteStringValue(file);
                }

                writer.WriteEndArray();
                writer.WriteNumber("total", TestRun.Total);
                writer.WriteNumber("passed", TestRun.Passed);
                writer.WriteNumber("failed", TestRun.Failed);
                writer.WriteString("date", TestRun.Date);
                writer.WriteEndObject();
            }

            writer.WriteStartObject("links");

            writer.WriteStartArray("implements");

            foreach (var link in Implements)
            {
                writer.WriteStartObject();
                writer.WriteString("path", link.Path);
                writer.WriteString("to", link.To);
                writer.WriteString("kind", link.Kind);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("follows");

            foreach (var link in Follows)
            {
                writer.WriteStartObject();
                writer.WriteString("path", link.Path);
                writer.WriteString("to", link.To);
                writer.WriteString("kind", link.Kind);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("calls");

            foreach (var link in Calls)
            {
                writer.WriteStartObject();
                writer.WriteString("screen", link.Screen);
                writer.WriteString("to", link.To);
                writer.WriteString("operation", link.Operation);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("proves");

            foreach (var link in Proves)
            {
                writer.WriteStartObject();
                writer.WriteString("scenario", link.Scenario);
                writer.WriteString("claim", link.Claim);
                writer.WriteBoolean("confirmed", link.Confirmed);

                if (link.Passing is null)
                {
                    writer.WriteNull("passing");
                }
                else
                {
                    writer.WriteBoolean("passing", link.Passing.Value);
                }

                writer.WriteBoolean("indirect", link.Indirect);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteStartObject("status");

            foreach (var entry in Status.OrderBy(left => left.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(entry.Key);
                writer.WriteBoolean("implemented", entry.Value.Implemented);
                writer.WriteBoolean("verified", entry.Value.Verified);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();

            writer.WriteStartObject("requirementStatus");

            foreach (var entry in RequirementStatus.OrderBy(left => left.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(entry.Key);
                writer.WriteBoolean("verified", entry.Value.Verified);

                if (entry.Value.MeasurePassed is null)
                {
                    writer.WriteNull("measurePassed");
                }
                else
                {
                    writer.WriteBoolean("measurePassed", entry.Value.MeasurePassed.Value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();

            writer.WriteStartArray("stale");

            foreach (var scenario in Stale)
            {
                writer.WriteStringValue(scenario);
            }

            writer.WriteEndArray();

            writer.WriteStartObject("tested");

            foreach (var entry in Tested.OrderBy(left => left.Key, StringComparer.Ordinal))
            {
                writer.WriteBoolean(entry.Key, entry.Value);
            }

            writer.WriteEndObject();

            writer.WriteStartObject("reachableStates");

            foreach (var entry in ReachableStates.OrderBy(left => left.Key, StringComparer.Ordinal))
            {
                writer.WriteStartArray(entry.Key);

                foreach (var state in entry.Value.OrderBy(left => left, StringComparer.Ordinal))
                {
                    writer.WriteStringValue(state);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();

            writer.WriteStartObject("constituents");

            foreach (var entry in Constituents.OrderBy(left => left.Key, StringComparer.Ordinal))
            {
                writer.WriteStartArray(entry.Key);

                foreach (var constituent in entry.Value.OrderBy(left => left, StringComparer.Ordinal))
                {
                    writer.WriteStringValue(constituent);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();

            if (CoverageRead)
            {
                writer.WriteStartArray("unreachedCode");

                foreach (var gap in UnreachedCode)
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", Config.Relative(gap.File));
                    writer.WriteNumber("line", gap.Line);
                    writer.WriteString("member", gap.Member);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNull("unreachedCode");
            }

            writer.WriteStartArray("measures");

            foreach (var measure in Measures)
            {
                writer.WriteStartObject();
                writer.WriteString("tool", measure.Tool);
                writer.WriteString("check", measure.Check);
                writer.WriteBoolean("passed", measure.Passed);
                WriteNullable(writer, "value", measure.Value);
                writer.WriteString("source", Config.Relative(measure.Source));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("unmatchedCalls");

            foreach (var call in UnmatchedCalls)
            {
                writer.WriteStartObject();
                writer.WriteString("screen", call.Screen);
                writer.WriteString("operation", call.Operation);
                writer.WriteString("file", call.File);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WritePropertyName("findings");

            if (FindingsJson is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, FindingsJson, options);
            }

            writer.WritePropertyName("known");

            if (KnownJson is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, KnownJson, options);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>The page text the book shows: the body without its H1 title, trimmed.</summary>
    /// <param name="body">The raw page body, with any line-ending form.</param>
    /// <returns>The body without its H1 title, trimmed, with LF line endings.</returns>
    private static string PageText(string body)
    {
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var first = 0;

        while (first < lines.Length && lines[first].Trim().Length == 0)
        {
            first++;
        }

        if (first < lines.Length && IsTitle(lines[first]))
        {
            first++;
        }

        return string.Join('\n', lines[first..]).Trim();
    }

    /// <summary>Tests whether a body line is the H1 title.</summary>
    /// <param name="line">The body line to test.</param>
    /// <returns>True when the trimmed line is an H1 heading.</returns>
    private static bool IsTitle(string line)
    {
        var text = line.TrimStart();

        return text.StartsWith("# ", StringComparison.Ordinal) || text == "#";
    }

    /// <summary>Writes the collaborators a reaction or job builds with <c>new</c>.</summary>
    /// <param name="writer">The JSON writer to write the array into.</param>
    /// <param name="collaborators">The collaborators to write, or null for an empty array.</param>
    private void WriteCollaborators(Utf8JsonWriter writer, IReadOnlyList<CodeCollaborator>? collaborators)
    {
        writer.WriteStartArray("collaborators");

        foreach (var collaborator in collaborators ?? [])
        {
            writer.WriteStartObject();
            writer.WriteString("type", collaborator.Type);
            writer.WriteString("path", Config.Relative(collaborator.File));
            writer.WriteNumber("line", collaborator.Line);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Writes the repository reads a reaction or job makes inside a loop.</summary>
    /// <param name="writer">The JSON writer to write the array into.</param>
    /// <param name="reads">The loop reads to write, or null for an empty array.</param>
    private void WriteLoopReads(Utf8JsonWriter writer, IReadOnlyList<CodeLoopRead>? reads)
    {
        writer.WriteStartArray("loopReads");

        foreach (var read in reads ?? [])
        {
            writer.WriteStartObject();
            writer.WriteString("repository", read.Repository);
            writer.WriteString("method", read.Method);
            writer.WriteString("path", Config.Relative(read.File));
            writer.WriteNumber("line", read.Line);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Writes one string property, or a JSON null when the value is null.</summary>
    /// <param name="writer">The JSON writer to write the property into.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value, or null.</param>
    private static void WriteNullable(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }
}
