using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.CompletionStages;

/// <summary>
///     The computed Intent coverage status: for each Intent requirement and
///     flow step, whether some Behavior record reaches it, and the share
///     reached. The book shows it whatever layers the project claims, first on the
///     Intent part.
/// </summary>
/// <param name="Entries">The requirement and flow step entries, requirements first in identifier order, then steps by flow and step order.</param>
/// <follows>litespec/core/intent#reach-every-intent-record-with-behavior</follows>
sealed record IntentCoverage(IReadOnlyList<IntentCoverage.Entry> Entries)
{
    /// <summary>Gets the number of Intent items the status covers.</summary>
    public int Total => Entries.Count;

    /// <summary>Gets the number of Intent items some Behavior record reaches.</summary>
    public int Reached => Entries.Count(entry => entry.Reached);

    /// <summary>Gets the number of requirement items the status covers.</summary>
    public int RequirementsTotal => Entries.Count(entry => entry.Kind == "requirement");

    /// <summary>Gets the number of requirement items some Behavior record reaches.</summary>
    public int RequirementsReached => Entries.Count(entry => entry.Kind == "requirement" && entry.Reached);

    /// <summary>Gets the number of flow step items the status covers.</summary>
    public int StepsTotal => Entries.Count(entry => entry.Kind == "step");

    /// <summary>Gets the number of flow step items some Behavior record reaches.</summary>
    public int StepsReached => Entries.Count(entry => entry.Kind == "step" && entry.Reached);

    /// <summary>Gets the number of problem items the status covers.</summary>
    public int ProblemsTotal => Entries.Count(entry => entry.Kind == "problem");

    /// <summary>Gets the number of problem items some Behavior record reaches.</summary>
    public int ProblemsReached => Entries.Count(entry => entry.Kind == "problem" && entry.Reached);

    /// <summary>Gets the number of constraint items the status covers.</summary>
    public int ConstraintsTotal => Entries.Count(entry => entry.Kind == "constraint");

    /// <summary>Gets the number of constraint items some Behavior record reaches.</summary>
    public int ConstraintsReached => Entries.Count(entry => entry.Kind == "constraint" && entry.Reached);

    /// <summary>Gets the share of Intent items reached, from 0 to 1; 1 when the status covers no item.</summary>
    public double Share => Total == 0 ? 1 : (double)Reached / Total;

    /// <summary>
    ///     Computes the Intent coverage of a built project model: a requirement
    ///     is reached when a record of a fulfilling type names it in
    ///     <c>fulfills</c>, or a served-content entry of an entry point names
    ///     it, and a flow step is reached when a record of a
    ///     step-serving type names it in <c>serves</c>.
    /// </summary>
    /// <param name="model">The built project model that holds every record.</param>
    /// <returns>The computed Intent coverage status.</returns>
    public static IntentCoverage Compute(ProjectModel model)
    {
        var fulfilledBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var servedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Add(Dictionary<string, List<string>> map, string target, string source)
        {
            if (target.Length == 0 || source.Length == 0)
            {
                return;
            }

            if (!map.TryGetValue(target, out var list))
            {
                list = [];
                map[target] = list;
            }

            if (!list.Contains(source, StringComparer.Ordinal))
            {
                list.Add(source);
            }
        }

        foreach (var record in model.Records)
        {
            if (RecordTypes.FulfillingTypes.Contains(record.Type, StringComparer.Ordinal))
            {
                foreach (var target in record.Ids(RecordFields.Fulfills))
                {
                    Add(fulfilledBy, target, record.Id);
                }
            }

            // A served-content entry fulfills the requirements it names for its entry point.
            foreach (var target in record.ServedFulfills())
            {
                Add(fulfilledBy, target, record.Id);
            }

            if (RecordTypes.StepServingTypes.Contains(record.Type, StringComparer.Ordinal))
            {
                foreach (var target in record.Ids(RecordFields.Serves))
                {
                    Add(servedBy, target, record.Id);
                }
            }
        }

        var requirements = model.Records
            .Where(record => record.Type == RecordTypes.Requirement && !record.Derived)
            .Select(record => record.Id)
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var steps = new List<string>();
        foreach (var flow in model.Records
            .Where(record => record.Type == RecordTypes.Flow && !record.Derived)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            steps.AddRange(flow.StepIds());
        }

        // A problem is reached by a feature in its folder or a flow that solves it; a constraint by a flow based on it.
        var answeredBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var record in model.Records.Where(record => !record.Derived))
        {
            if (record.Type == RecordTypes.Feature
                && RecordPlacement.ProblemOf(record.File) is { } problem)
            {
                Add(answeredBy, $"problem/{problem}", record.Id);
            }

            if (record.Type == RecordTypes.Flow)
            {
                foreach (var target in record.Ids(RecordFields.Solves).Concat(record.Ids(RecordFields.BasedOn)))
                {
                    Add(answeredBy, target, record.Id);
                }
            }
        }

        var status = Build(requirements, steps, fulfilledBy, servedBy);
        var answerables = AnswerableEntries(model, answeredBy);

        return new IntentCoverage([
            .. status.Entries.Take(requirements.Count),
            .. answerables,
            .. status.Entries.Skip(requirements.Count),
        ]);
    }

    /// <summary>
    ///     One entry per problem, then one per constraint, each in identifier order, reached by the features
    ///     in its folder or the flows that name it in <c>solves</c> or <c>basedOn</c>.
    /// </summary>
    /// <param name="model">The built project model that holds every record.</param>
    /// <param name="answeredBy">The feature or flow record ids per problem or constraint id.</param>
    /// <returns>The problem and constraint entries.</returns>
    private static List<Entry> AnswerableEntries(ProjectModel model, IReadOnlyDictionary<string, List<string>> answeredBy)
    {
        var entries = new List<Entry>();

        foreach (var (type, kind) in new[] { (RecordTypes.Problem, "problem"), (RecordTypes.Constraint, "constraint") })
        {
            foreach (var id in model.Records
                .Where(record => record.Type == type && !record.Derived && record.Id.Length > 0)
                .Select(record => record.Id)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal))
            {
                var by = ReachedByOf(answeredBy, id);
                entries.Add(new Entry(id, kind, by.Count > 0, by));
            }
        }

        return entries;
    }

    /// <summary>
    ///     Builds the status from the reaching maps: one entry per requirement,
    ///     then one per flow step, each with its reaching records in
    ///     identifier order.
    /// </summary>
    /// <param name="requirements">The requirement ids, in identifier order.</param>
    /// <param name="steps">The flow step ids, by flow and step order.</param>
    /// <param name="fulfilledBy">The fulfilling record ids per requirement id.</param>
    /// <param name="servedBy">The serving record ids per flow step id.</param>
    /// <returns>The computed Intent coverage status.</returns>
    internal static IntentCoverage Build(
        IReadOnlyList<string> requirements,
        IReadOnlyList<string> steps,
        IReadOnlyDictionary<string, List<string>> fulfilledBy,
        IReadOnlyDictionary<string, List<string>> servedBy)
    {
        var entries = new List<Entry>();

        foreach (var id in requirements)
        {
            var by = ReachedByOf(fulfilledBy, id);
            entries.Add(new Entry(id, "requirement", by.Count > 0, by));
        }

        foreach (var id in steps)
        {
            var by = ReachedByOf(servedBy, id);
            entries.Add(new Entry(id, "step", by.Count > 0, by));
        }

        return new IntentCoverage(entries);
    }

    /// <summary>The reaching record ids of one item in identifier order, empty when no record reaches it.</summary>
    /// <param name="map">The reaching record ids per item id.</param>
    /// <param name="id">The item id.</param>
    /// <returns>The reaching record ids in identifier order.</returns>
    static List<string> ReachedByOf(IReadOnlyDictionary<string, List<string>> map, string id) =>
        map.TryGetValue(id, out var list)
            ? list.Distinct(StringComparer.Ordinal).OrderBy(source => source, StringComparer.Ordinal).ToList()
            : [];

    /// <summary>
    ///     One Intent item in the Intent coverage status: a requirement, a flow
    ///     step, a problem or a constraint, whether a Behavior record reaches it,
    ///     and the records that reach it.
    /// </summary>
    /// <param name="Id">The requirement, problem or constraint id, or the flow step id as <c>step/&lt;flow&gt;.&lt;act&gt;</c>.</param>
    /// <param name="Kind">The item kind: <c>requirement</c>, <c>step</c>, <c>problem</c> or <c>constraint</c>.</param>
    /// <param name="Reached">Whether some Behavior record reaches the item.</param>
    /// <param name="ReachedBy">The Behavior record ids that reach the item, in identifier order.</param>
    public sealed record Entry(
        string Id,
        string Kind,
        bool Reached,
        IReadOnlyList<string> ReachedBy);
}
