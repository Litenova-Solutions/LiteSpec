namespace LiteSpec.Model;

/// <summary>One result of a measured check, read from a k6, Lighthouse CI or ZAP result file.</summary>
sealed record MeasureResult(string Tool, string Check, bool Passed, string? Value, string Source);
