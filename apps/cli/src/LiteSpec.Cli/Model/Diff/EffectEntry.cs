namespace LiteSpec.Model.Diff;

/// <summary>One parsed effect edge: its verb, target and timing.</summary>
/// <param name="Verb">The effect verb: runs, sends, emits, or calls.</param>
/// <param name="Target">The record the effect targets.</param>
/// <param name="Timing">The effect timing, as written.</param>
sealed record EffectEntry(string Verb, string Target, string? Timing)
{
    /// <summary>The edge without its timing, such as calls external-system/stripe#charge.</summary>
    /// <returns>The edge without its timing.</returns>
    public string Edge() => $"{Verb} {Target}";

    /// <summary>The edge with its timing.</summary>
    /// <returns>The edge with its timing.</returns>
    public string Describe() => $"{Verb} {Target} ({Timing ?? "no timing"})";
}
