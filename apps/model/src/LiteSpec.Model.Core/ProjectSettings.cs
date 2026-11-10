namespace LiteSpec.Model;

/// <summary>
///     The check settings a host reads from .litespec/project.yaml: the mark style and the
///     conformance level the layer claims map to.
/// </summary>
/// <param name="Marks">The declared mark style.</param>
/// <param name="Conformance">The level the declared layer claims read as.</param>
public sealed record ProjectSettings(MarkStyle Marks, ConformanceLevel Conformance)
{
    /// <summary>
    ///     Reads the settings from the project file text. A missing or unknown value reads as
    ///     its default: xml marks, and the full level when no layer is claimed. The project
    ///     schema reports a value of the wrong shape, so this reader never reports one.
    /// </summary>
    /// <param name="text">The project file text.</param>
    /// <param name="settings">The parsed settings, or the defaults when parsing fails.</param>
    /// <param name="error">The error the host prints, or null when parsing succeeds.</param>
    /// <returns>Whether the text holds a mapping the settings read from.</returns>
    public static bool TryParse(string text, out ProjectSettings settings, out string? error)
    {
        settings = new ProjectSettings(MarkStyle.Xml, ConformanceLevel.Full);
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

        settings = new ProjectSettings(ReadMarks(root), ReadConformance(root));
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

    /// <summary>
    ///     Maps the implementation and proof claims to a level. Neither claimed is
    ///     <see cref="ConformanceLevel.Records"/>; both claimed as LiteSpec's own practice is
    ///     <see cref="ConformanceLevel.Full"/>; any other claim is <see cref="ConformanceLevel.Traced"/>.
    /// </summary>
    /// <param name="implementation">The implementation claim, or null when the layer is not claimed.</param>
    /// <param name="proof">The proof claim, or null when the layer is not claimed.</param>
    /// <returns>The level the claims read as.</returns>
    public static ConformanceLevel LevelOf(string? implementation, string? proof) =>
        implementation is null && proof is null
            ? ConformanceLevel.Records
            : implementation == "litespec" && proof == "litespec" ? ConformanceLevel.Full : ConformanceLevel.Traced;

    /// <summary>Reads the level the layer claims map to; full when the file claims no layer.</summary>
    /// <param name="root">The project file mapping.</param>
    /// <returns>The level.</returns>
    private static ConformanceLevel ReadConformance(JsonText root)
    {
        if (!root.TryGetProperty("layers", out var layers) || layers.Kind != JsonKind.Object)
        {
            return ConformanceLevel.Full;
        }

        return LevelOf(Claim(layers, "implementation"), Claim(layers, "proof"));
    }

    /// <summary>The claim a layer makes, or null when the layer is not claimed.</summary>
    /// <param name="layers">The layers mapping.</param>
    /// <param name="layer">The layer name.</param>
    /// <returns>The claim text, or null.</returns>
    private static string? Claim(JsonText layers, string layer) =>
        layers.TryGetProperty(layer, out var value) ? value.GetString() : null;

    /// <summary>Lowercases the first letter of a message.</summary>
    /// <param name="message">The message to read.</param>
    /// <returns>The message with a lowercase first letter.</returns>
    private static string LowerHead(string message) =>
        message.Length == 0 || char.IsLower(message[0])
            ? message
            : char.ToLowerInvariant(message[0]) + message.Substring(1);
}
