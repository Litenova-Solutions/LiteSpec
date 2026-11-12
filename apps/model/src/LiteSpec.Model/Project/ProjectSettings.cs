namespace LiteSpec.Model;

/// <summary>
///     The check settings a host reads from .litespec/project.yaml: the mark style, the
///     record layers the project claims, and the layers whose records carry explanations.
/// </summary>
/// <param name="Marks">The declared mark style.</param>
/// <param name="Layers">The declared layer claims.</param>
/// <param name="Explanations">The declared explanation claims.</param>
public sealed record ProjectSettings(MarkStyle Marks, LayerClaims Layers, ExplanationClaims Explanations)
{
    /// <summary>
    ///     Reads the settings from the project file text. A missing or unknown value reads as
    ///     its default: xml marks, every layer with LiteSpec's practice when the file has no layers,
    ///     and no required explanation layer. The project
    ///     schema reports a value of the wrong shape, so this reader never reports one.
    /// </summary>
    /// <param name="text">The project file text.</param>
    /// <param name="settings">The parsed settings, or the defaults when parsing fails.</param>
    /// <param name="error">The error the host prints, or null when parsing succeeds.</param>
    /// <returns>Whether the text holds a mapping the settings read from.</returns>
    public static bool TryParse(string text, out ProjectSettings settings, out string? error)
    {
        settings = new ProjectSettings(MarkStyle.Xml, LayerClaims.All, ExplanationClaims.None);
        error = null;

        JsonText root;

        try
        {
            root = YamlText.Parse(text);
        }
        catch (JsonTextException exception)
        {
            error = ".litespec/project.yaml is not valid YAML or JSON: " + LowerHead(exception.Message);
            return false;
        }

        if (root.Kind != JsonKind.Object)
        {
            error = ".litespec/project.yaml: expected a mapping.";
            return false;
        }

        settings = new ProjectSettings(ReadMarks(root), LayerClaims.Read(root), ExplanationClaims.Read(root));
        return true;
    }

    /// <summary>Reads the declared mark style; xml when the file names none or an unknown style.</summary>
    /// <param name="root">The project file mapping.</param>
    /// <returns>The declared style, or the default.</returns>
    private static MarkStyle ReadMarks(JsonText root) =>
        root.TryGetProperty("marks", out var value)
        && value.GetString() is string text
        && MarkStyleParser.TryParse(text, out var marks)
            ? marks
            : MarkStyle.Xml;

    /// <summary>Lowercases the first letter of a message.</summary>
    /// <param name="message">The message to read.</param>
    /// <returns>The message with a lowercase first letter.</returns>
    private static string LowerHead(string message) =>
        message.Length == 0 || char.IsLower(message[0])
            ? message
            : char.ToLowerInvariant(message[0]) + message.Substring(1);
}
