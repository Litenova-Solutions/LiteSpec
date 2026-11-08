namespace LiteSpec.Model.Readers.Suppressions;

/// <summary>
///     One lowering or suppression of a LiteSpec analyzer rule: the rule it names, the
///     place that lowers or suppresses it, the lowered severity where one is stated,
///     and the file and line that state it.
/// </summary>
/// <param name="Rule">The analyzer rule identifier, such as LS1001.</param>
/// <param name="Kind">The place that lowers or suppresses the rule: .editorconfig, .globalconfig, NoWarn, #pragma warning disable or SuppressMessage.</param>
/// <param name="Detail">The lowered severity the place states, or empty when it states none.</param>
/// <param name="File">The absolute path of the file that states the lowering or suppression.</param>
/// <param name="Line">The 1-based line that states the lowering or suppression.</param>
/// <follows>litespec/core/model#record-a-project-wide-lowering-as-an-exception</follows>
sealed record AnalyzerSuppression(string Rule, string Kind, string Detail, string File, int Line);
