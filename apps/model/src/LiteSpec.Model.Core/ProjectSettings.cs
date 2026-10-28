using System;

namespace LiteSpec.Model;

/// <summary>
///     The project phase a check runs in, from check.phase in litespec.project.json.
/// </summary>
public enum ProjectPhase
{
    /// <summary>The project moves code and tests together; proof findings block as their tier states.</summary>
    Working = 0,

    /// <summary>The project holds its tests still; proof findings report without blocking.</summary>
    Frozen = 1,
}

/// <summary>Writes a <see cref="ProjectPhase" /> back to its project file value.</summary>
public static class ProjectPhaseExtensions
{
    /// <summary>Returns <c>working</c> or <c>frozen</c> for the phase.</summary>
    /// <param name="phase">The phase to write.</param>
    /// <returns>The project file value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The phase is not a known value.</exception>
    public static string ToProjectValue(this ProjectPhase phase)
    {
        switch (phase)
        {
            case ProjectPhase.Working:
                return "working";
            case ProjectPhase.Frozen:
                return "frozen";
            default:
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown project phase.");
        }
    }
}

/// <summary>
///     The check settings a host reads from litespec.project.json: the mark style and the check phase.
/// </summary>
/// <param name="Marks">The declared mark style.</param>
/// <param name="Phase">The declared check phase.</param>
public sealed record ProjectSettings(MarkStyle Marks, ProjectPhase Phase)
{
    /// <summary>
    ///     Reads the settings from the project file text. A missing value reads as its
    ///     default: xml marks and the working phase. An unknown value is an error the
    ///     host prints, never a silent default.
    /// </summary>
    /// <param name="text">The project file text.</param>
    /// <param name="settings">The parsed settings, or the defaults when parsing fails.</param>
    /// <param name="error">The error the host prints, or null when parsing succeeds.</param>
    /// <returns>Whether the text holds known settings.</returns>
    public static bool TryParse(string text, out ProjectSettings settings, out string? error)
    {
        settings = new ProjectSettings(MarkStyle.Xml, ProjectPhase.Working);
        error = null;

        JsonText root;

        try
        {
            root = JsonText.Parse(text);
        }
        catch (JsonTextException exception)
        {
            error = "litespec.project.json: " + LowerHead(exception.Message);
            return false;
        }

        if (root.Kind != JsonKind.Object)
        {
            error = "litespec.project.json: expected an object.";
            return false;
        }

        if (!TryReadMarks(root, out var marks, out error))
        {
            return false;
        }

        if (!TryReadPhase(root, out var phase, out error))
        {
            return false;
        }

        settings = new ProjectSettings(marks, phase);
        return true;
    }

    private static bool TryReadMarks(JsonText root, out MarkStyle marks, out string? error)
    {
        marks = MarkStyle.Xml;
        error = null;

        if (!root.TryGetProperty("marks", out var value) || value.Kind == JsonKind.Null)
        {
            return true;
        }

        if (value.GetString() is string text && MarkStyleParser.TryParse(text, out marks))
        {
            return true;
        }

        error = value.GetString() is string named
            ? "litespec.project.json: unknown marks \"" + named + "\"; expected \"xml\" or \"attribute\"."
            : "litespec.project.json: marks must be \"xml\" or \"attribute\".";
        return false;
    }

    private static bool TryReadPhase(JsonText root, out ProjectPhase phase, out string? error)
    {
        phase = ProjectPhase.Working;
        error = null;

        if (!root.TryGetProperty("check", out var check) || check.Kind == JsonKind.Null)
        {
            return true;
        }

        if (check.Kind != JsonKind.Object)
        {
            error = "litespec.project.json: check must be an object with phase \"working\" or \"frozen\".";
            return false;
        }

        if (!check.TryGetProperty("phase", out var value) || value.Kind == JsonKind.Null)
        {
            return true;
        }

        if (value.GetString() is string text && TryParsePhase(text, out phase))
        {
            return true;
        }

        error = value.GetString() is string named
            ? "litespec.project.json: unknown check.phase \"" + named + "\"; expected \"working\" or \"frozen\"."
            : "litespec.project.json: check.phase must be \"working\" or \"frozen\".";
        return false;
    }

    private static bool TryParsePhase(string? value, out ProjectPhase phase)
    {
        if (value is null || value.Trim().Length == 0)
        {
            phase = ProjectPhase.Working;
            return true;
        }

        var text = value.Trim();

        if (text.Equals("working", StringComparison.OrdinalIgnoreCase))
        {
            phase = ProjectPhase.Working;
            return true;
        }

        if (text.Equals("frozen", StringComparison.OrdinalIgnoreCase))
        {
            phase = ProjectPhase.Frozen;
            return true;
        }

        phase = ProjectPhase.Working;
        return false;
    }

    private static string LowerHead(string message) =>
        message.Length == 0 || char.IsLower(message[0])
            ? message
            : char.ToLowerInvariant(message[0]) + message.Substring(1);
}
