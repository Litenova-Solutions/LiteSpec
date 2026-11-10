using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteSpec.Model;

/// <summary>
///     The conformance level names and the lowest level that runs each finding code,
///     so <c>model check</c>, the book and <c>rules</c> report only what a project claims.
///     Each code takes the lowest level whose inputs its check reads: a check that
///     reads records alone runs at <see cref="ConformanceLevel.Records" />, a check
///     that reads code, screens, tests, test runs or measure results through a stack
///     reader runs at <see cref="ConformanceLevel.Traced" />, and a check that reads
///     LiteSpec's code layout, operations files, the bill of materials or the
///     controlled UI runs at <see cref="ConformanceLevel.Full" />.
/// </summary>
/// <follows>litespec/core/scope#declare-the-conformance-level</follows>
public static class ConformanceLevels
{
    /// <summary>Gets the lowest level that runs each finding code; a code missing from the map runs at full.</summary>
    public static IReadOnlyDictionary<string, ConformanceLevel> Lowest { get; } =
        FindingCodes.All.ToDictionary(code => code.Code, code => code.Lowest, StringComparer.Ordinal);

    /// <summary>Gets the layout group: the codes about LiteSpec's own code layout, each running only at full.</summary>
    public static IReadOnlyCollection<string> Layout { get; } = FindingCodes.Layout;

    /// <summary>Reads a level from its name in the project file.</summary>
    /// <param name="value">The <c>conformance</c> value: records, traced or full.</param>
    /// <param name="level">The level the value names, or full when it names none.</param>
    /// <returns>True when the value names a level; otherwise false.</returns>
    public static bool TryParse(string? value, out ConformanceLevel level)
    {
        switch (value)
        {
            case "records":
                level = ConformanceLevel.Records;
                return true;
            case "traced":
                level = ConformanceLevel.Traced;
                return true;
            case "full":
                level = ConformanceLevel.Full;
                return true;
            default:
                level = ConformanceLevel.Full;
                return false;
        }
    }

    /// <summary>The name the project file writes for a level.</summary>
    /// <param name="level">The level.</param>
    /// <returns>records, traced or full.</returns>
    public static string Name(ConformanceLevel level) => level switch
    {
        ConformanceLevel.Records => "records",
        ConformanceLevel.Traced => "traced",
        ConformanceLevel.Full => "full",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Not a conformance level."),
    };

    /// <summary>The lowest level that runs a finding code.</summary>
    /// <param name="code">The finding code.</param>
    /// <returns>The lowest level that runs the code; full for a code the map does not hold.</returns>
    public static ConformanceLevel LowestOf(string code) =>
        Lowest.TryGetValue(code, out var level) ? level : ConformanceLevel.Full;

    /// <summary>Whether a project at the claimed level runs a finding code.</summary>
    /// <param name="code">The finding code.</param>
    /// <param name="claimed">The level the project claims.</param>
    /// <returns>True when the code's lowest level is at or below the claimed level.</returns>
    public static bool Runs(string code, ConformanceLevel claimed) => LowestOf(code) <= claimed;
}
