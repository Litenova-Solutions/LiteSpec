namespace LiteSpec.Cli.Model.CompletionStages;

/// <summary>
///     One record's computed completion: its stage, whether proof is known, the
///     in-scope children holding it back, the parked children, and the counts behind the stage.
/// </summary>
/// <param name="Stage">The stage: unfulfilled, specified, coded, or verified.</param>
/// <param name="Proof">Whether proof is known: known when the model holds a test run, else unknown.</param>
/// <param name="HeldBy">The in-scope ids holding the record back, at most ten.</param>
/// <param name="Counts">The labelled done and of counts behind the stage, over in-scope children.</param>
/// <param name="ParkedBy">The parked children with the waiver parking each and its expiry, in id order.</param>
/// <param name="InScope">How many children are in scope.</param>
/// <param name="Parked">How many children a live waiver parks.</param>
/// <param name="Total">How many children the record holds, in scope and parked.</param>
/// <param name="Outcome">A goal's outcome: "not measured" until it holds observations, then "measured"; empty for every other record.</param>
sealed record Completion(
    string Stage,
    string Proof,
    IReadOnlyList<string> HeldBy,
    IReadOnlyDictionary<string, CompletionCount> Counts,
    IReadOnlyList<ParkedEntry> ParkedBy,
    int InScope,
    int Parked,
    int Total,
    string Outcome = "");
