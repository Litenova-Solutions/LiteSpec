namespace LiteSpec.Model.Readers.Operations;

/// <summary>One drill or harness run: the check it names and what it saw.</summary>
/// <param name="Check">The check name the requirement carries.</param>
/// <param name="Passed">Whether the run passed.</param>
/// <param name="Value">The measured value, when the run states one.</param>
/// <param name="RanOn">The run date, when the run states one.</param>
/// <param name="File">The absolute file path.</param>
sealed record CheckResult(string Check, bool Passed, string? Value, string? RanOn, string File);
