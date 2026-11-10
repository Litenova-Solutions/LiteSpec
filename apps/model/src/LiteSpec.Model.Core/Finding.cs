using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     One check finding: a code, a message, a path and a line.
/// </summary>
/// <param name="Code">The stable finding code.</param>
/// <param name="Message">What is wrong, in one sentence.</param>
/// <param name="Path">The absolute path.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Subject">The record id the finding is about, when there is one.</param>
public sealed record Finding(string Code, string Message, string Path, int Line, string? Subject)
{
    /// <summary>Gets the reader that produced this finding's facts: <c>roslyn</c>, <c>typescript</c>, or null for a record-only finding.</summary>
    public string? Reader { get; init; }

    /// <summary>Gets a value indicating whether whether the reader named by <see cref="Reader" /> is qualified, so its S findings block.</summary>
    public bool ReaderIsQualified { get; init; } = true;

    /// <summary>The .NET (Roslyn) reader name.</summary>
    public const string RoslynReader = "roslyn";

    /// <summary>Gets the tier this one finding carries instead of its code's tier, when the check grades it apart from its code.</summary>
    public string? TierOverride { get; init; }

    /// <summary>The TypeScript reader name.</summary>
    public const string TypeScriptReader = "typescript";

    /// <summary>Gets the finding tier from <see cref="FindingCodes" />: G blocks, S blocks where qualified, A reports.</summary>
    public string Tier => TierOverride ?? (FindingCodes.IsKnown(Code) ? FindingCodes.Of(Code).Level : "G");
}
