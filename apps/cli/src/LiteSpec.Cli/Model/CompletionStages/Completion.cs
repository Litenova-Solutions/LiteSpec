namespace LiteSpec.Cli.Model.CompletionStages;

/// <summary>
///     One record's computed completion: its stage, whether proof is known, the
///     children holding it back, and the counts behind the stage.
/// </summary>
/// <param name="Stage">The stage: unfulfilled, specified, coded, or verified.</param>
/// <param name="Proof">Whether proof is known: known when the model holds a test run, else unknown.</param>
/// <param name="HeldBy">The ids holding the record back, at most ten, with parked children named after them.</param>
/// <param name="Counts">The labelled done and of counts behind the stage.</param>
sealed record Completion(
    string Stage,
    string Proof,
    IReadOnlyList<string> HeldBy,
    IReadOnlyDictionary<string, CompletionCount> Counts);
