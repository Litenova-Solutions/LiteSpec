using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Model;

/// <summary>
///     The JSON frontmatter a record page carries between two fence lines.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class Frontmatter
{
    /// <summary>
    ///     The parsed fences: the object, the body after it, and the body's first line.
    /// </summary>
    /// <param name="Root">The frontmatter object.</param>
    /// <param name="Body">The text after the closing fence.</param>
    /// <param name="BodyStartLine">The body's first line, 1-based.</param>
    public sealed record Parsed(JsonElement Root, string Body, int BodyStartLine);

    /// <summary>
    ///     Parses the frontmatter when the file starts with a fence line.
    /// </summary>
    /// <param name="text">The file's text.</param>
    /// <param name="parsed">The fences.</param>
    /// <param name="error">Why parsing failed.</param>
    /// <returns>Whether the file carries readable frontmatter.</returns>
    public static bool TryParse(string text, out Parsed parsed, out string error)
    {
        parsed = null!;
        error = "";

        var lines = text.Split('\n');

        if (lines.Length < 3 || lines[0].TrimEnd('\r') != "---")
        {
            error = "no frontmatter fence";
            return false;
        }

        var closing = -1;

        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].TrimEnd('\r') == "---")
            {
                closing = index;
                break;
            }
        }

        if (closing < 0)
        {
            error = "frontmatter never closes";
            return false;
        }

        var json = string.Join('\n', lines[1..closing]);

        try
        {
            using var document = JsonDocument.Parse(json);
            var body = string.Join('\n', lines[(closing + 1)..]);

            parsed = new Parsed(document.RootElement.Clone(), body, closing + 2);
            return true;
        }
        catch (JsonException exception)
        {
            error = $"frontmatter is not JSON: {exception.Message}";
            return false;
        }
    }

    /// <summary>
    ///     Splits a page into the opening fence, its frontmatter and the rest, for a
    ///     writer that replaces the frontmatter and keeps the rest. Line endings are
    ///     normalized to <c>\n</c> first, so the head is always <c>---\n</c> and the
    ///     tail starts at the closing fence's line ending.
    /// </summary>
    /// <param name="text">The file's text.</param>
    /// <returns>The opening fence, the parsed frontmatter or null when the page carries none, and the rest.</returns>
    public static (string Head, JsonNode? Node, string Tail) SplitForWrite(string text)
    {
        const string fence = "---\n";
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!normalized.StartsWith(fence, StringComparison.Ordinal))
        {
            return ("", null, "");
        }

        var end = normalized.IndexOf("\n---", fence.Length, StringComparison.Ordinal);

        if (end < 0)
        {
            return ("", null, "");
        }

        var body = normalized[fence.Length..end];
        return (fence, JsonNode.Parse(body), normalized[end..]);
    }
}
