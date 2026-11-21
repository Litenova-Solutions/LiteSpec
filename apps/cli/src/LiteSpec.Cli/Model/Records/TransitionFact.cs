namespace LiteSpec.Cli.Model.Records;

/// <summary>One transition an aggregate declares: its source and target state, and the job and duration that time it.</summary>
/// <param name="From">The state the transition leaves, or the creation marker.</param>
/// <param name="To">The state the transition enters.</param>
/// <param name="MovedBy">The job identifier that moves the aggregate by time, or empty when the transition is not timed by a job.</param>
/// <param name="After">The ISO 8601 duration from entering the source state, or empty when none is declared.</param>
sealed record TransitionFact(string From, string To, string MovedBy, string After);
