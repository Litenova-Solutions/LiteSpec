namespace LiteSpec.Model;

/// <summary>
///     One finding code: its check level, the record layer it governs, whether it ties that layer
///     to the records, the extension that adds it, what it means, and how to fix it. <see cref="FindingCodes" /> holds every code the tools emit.
/// </summary>
/// <follows>litespec/core/language#name-each-finding-code-by-subject-and-problem</follows>
public sealed class FindingCode
{
    /// <summary>Initializes a new instance of the <see cref="FindingCode" /> class.</summary>
    /// <param name="code">The kebab-case code, such as <c>record-invalid</c>.</param>
    /// <param name="level">The check level: G, S or A.</param>
    /// <param name="layer">The record layer the code governs.</param>
    /// <param name="trace">Whether the code ties the layer to the records rather than checking LiteSpec's practice for it.</param>
    /// <param name="meaning">What the finding means, in one sentence.</param>
    /// <param name="fix">How to resolve it, in one sentence.</param>
    /// <param name="extension">The extension that adds the code, or null when the core holds it.</param>
    public FindingCode(string code, string level, RecordLayer layer, bool trace, string meaning, string fix, string? extension = null)
    {
        Code = code;
        Level = level;
        Layer = layer;
        Trace = trace;
        Meaning = meaning;
        Fix = fix;
        Extension = extension;
    }

    /// <summary>Gets the kebab-case code.</summary>
    public string Code { get; }

    /// <summary>Gets the check level: G reads records only and always blocks, S reads code through a stack reader and blocks where the reader is qualified, and A is advisory.</summary>
    public string Level { get; }

    /// <summary>Gets the record layer the code governs.</summary>
    public RecordLayer Layer { get; }

    /// <summary>Gets a value indicating whether the code ties its layer to the records rather than checking LiteSpec's practice for it.</summary>
    public bool Trace { get; }

    /// <summary>Gets the extension that adds the code, or null when the core holds it.</summary>
    public string? Extension { get; }

    /// <summary>Gets what the finding means, in one sentence.</summary>
    public string Meaning { get; }

    /// <summary>Gets how to resolve the finding, in one sentence.</summary>
    public string Fix { get; }
}
