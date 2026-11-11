using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Book;

/// <summary>
///     The prose fields of one record rendered to HTML: every C4 field the record
///     carries, with hash record links.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookProseFields
{
    /// <summary>Gets the prose fields the book renders as Markdown.</summary>
    public static IReadOnlyList<string> Prose { get; } =
    [
        "summary", "statement", "offering", "consequences",
        "refusals", "betweenSteps", "delivery", "impact",
        "prerequisites", "procedure", "verification", "recovery", "escalation",
        "exposure", "exit",
    ];

    /// <summary>Renders one record's prose fields to HTML with hash record links.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="record">The record.</param>
    /// <returns>The rendered HTML per prose field the record carries.</returns>
    public static JsonObject Render(BookIndex index, JsonObject record)
    {
        var html = new JsonObject();
        var fields = BookRender.Fields(record);
        if (fields is null)
        {
            return html;
        }

        (string Href, string Title)? LinkRecord(string target) => BookDetailValues.RecordLink(index, target);

        foreach (var name in Prose)
        {
            if (fields[name] is JsonValue scalar
                && scalar.TryGetValue<string>(out var text)
                && text.Length > 0)
            {
                html[name] = BookMarkdown.Render(text, LinkRecord);
            }
        }

        return html;
    }
}
