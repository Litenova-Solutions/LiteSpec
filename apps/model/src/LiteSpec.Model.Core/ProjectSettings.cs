using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     The check settings a host reads from litespec.project.json: the mark style
///     and the domain verbs the project adds to the baseline use-case vocabulary.
/// </summary>
/// <param name="Marks">The declared mark style.</param>
public sealed record ProjectSettings(MarkStyle Marks)
{
    /// <summary>The domain verbs the project adds to the baseline vocabulary.</summary>
    public IReadOnlyList<string> Verbs { get; init; } = Array.Empty<string>();

    /// <summary>The project verbs that name a query, beside the baseline reading verbs.</summary>
    public IReadOnlyList<string> QueryVerbs { get; init; } = Array.Empty<string>();

    /// <summary>Multi-word verbs counted as one use-case opening.</summary>
    public IReadOnlyList<string> PhrasalVerbs { get; init; } = Array.Empty<string>();

    /// <summary>
    ///     Reads the settings from the project file text. A missing value reads as its
    ///     default: xml marks. An unknown value is an error the host prints,
    ///     never a silent default.
    /// </summary>
    /// <param name="text">The project file text.</param>
    /// <param name="settings">The parsed settings, or the defaults when parsing fails.</param>
    /// <param name="error">The error the host prints, or null when parsing succeeds.</param>
    /// <returns>Whether the text holds known settings.</returns>
    public static bool TryParse(string text, out ProjectSettings settings, out string? error)
    {
        settings = new ProjectSettings(MarkStyle.Xml);
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

        settings = new ProjectSettings(marks)
        {
            Verbs = VocabularyNames(root, "verbs"),
            QueryVerbs = VocabularyNames(root, "queryVerbs"),
            PhrasalVerbs = VocabularyNames(root, "phrasal"),
        };
        return true;
    }

    private static IReadOnlyList<string> VocabularyNames(JsonText root, string name)
    {
        if (root.TryGetProperty("vocabulary", out var vocabulary)
            && vocabulary.Kind == JsonKind.Object
            && vocabulary.TryGetProperty(name, out var value)
            && value.Kind == JsonKind.Array)
        {
            var names = new List<string>();

            foreach (var entry in value.Items)
            {
                if (entry.GetString() is string text)
                {
                    names.Add(text);
                }
            }

            return names;
        }

        return Array.Empty<string>();
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

    private static string LowerHead(string message) =>
        message.Length == 0 || char.IsLower(message[0])
            ? message
            : char.ToLowerInvariant(message[0]) + message.Substring(1);
}
