namespace LiteSpec.Model;

/// <summary>
///     One finding code: its check level, the lowest conformance level that runs it, what it
///     means, and how to fix it. <see cref="FindingCodes" /> holds every code the tools emit.
/// </summary>
/// <follows>litespec/core/language#name-each-finding-code-by-subject-and-problem</follows>
public sealed class FindingCode
{
    /// <summary>Initializes a new instance of the <see cref="FindingCode" /> class.</summary>
    /// <param name="code">The kebab-case code, such as <c>record-invalid</c>.</param>
    /// <param name="level">The check level: G, S or A.</param>
    /// <param name="lowest">The lowest conformance level that runs the code.</param>
    /// <param name="meaning">What the finding means, in one sentence.</param>
    /// <param name="fix">How to resolve it, in one sentence.</param>
    public FindingCode(string code, string level, ConformanceLevel lowest, string meaning, string fix)
    {
        Code = code;
        Level = level;
        Lowest = lowest;
        Meaning = meaning;
        Fix = fix;
    }

    /// <summary>Gets the kebab-case code.</summary>
    public string Code { get; }

    /// <summary>Gets the check level: G reads records only and always blocks, S reads code through a stack reader and blocks where the reader is qualified, and A is advisory.</summary>
    public string Level { get; }

    /// <summary>Gets the lowest conformance level that runs the code.</summary>
    public ConformanceLevel Lowest { get; }

    /// <summary>Gets what the finding means, in one sentence.</summary>
    public string Meaning { get; }

    /// <summary>Gets how to resolve the finding, in one sentence.</summary>
    public string Fix { get; }
}
