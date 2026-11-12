using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Output;

namespace LiteSpec.Cli.Book.Details;

/// <summary>
///     The explanation detail of one record: its explanation file rendered to HTML
///     with hash record links, split by section.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgements-in-the-book</follows>
static class BookBodyDetail
{
    /// <summary>Renders one record explanation to HTML with hash record links, split by section.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="record">The record.</param>
    /// <returns>The explanation HTML and its sections.</returns>
    public static (string Html, JsonObject Sections) Render(BookIndex index, JsonObject record)
    {
        var explanation = WithoutTitle(BookRender.StrProp(record, "explanation") ?? "");
        (string Href, string Title)? LinkRecord(string target) => BookDetailValues.RecordLink(index, target);
        Func<string, string?>? linkSource = string.Equals(BookRender.StrProp(record, "type"), "research", StringComparison.Ordinal)
            ? key => SourceUrl(record, key)
            : null;

        string HtmlOf(string text) => BookMarkdown.Render(text, LinkRecord, linkSource);

        var split = SplitSections(explanation);
        var items = new JsonArray();
        foreach (var section in split.Items)
        {
            items.Add((JsonNode)new JsonObject
            {
                ["h"] = section.Heading,
                ["html"] = HtmlOf(section.Body),
            });
        }

        return (HtmlOf(explanation), new JsonObject
        {
            ["intro"] = HtmlOf(split.Intro),
            ["list"] = items,
        });
    }

    /// <summary>Drops the leading title line of one explanation file, leaving the sections.</summary>
    /// <param name="explanation">The explanation file text.</param>
    /// <returns>The explanation without its leading title line.</returns>
    internal static string WithoutTitle(string explanation)
    {
        var normalized = explanation.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!normalized.StartsWith("# ", StringComparison.Ordinal))
        {
            return explanation;
        }

        var newline = normalized.IndexOf('\n');
        return newline < 0 ? "" : normalized[(newline + 1)..];
    }

    /// <summary>One research source key as its URL, or null when the record names no URL source under the key.</summary>
    /// <param name="record">The record.</param>
    /// <param name="key">The source key.</param>
    /// <returns>The source URL, or null.</returns>
    static string? SourceUrl(JsonObject record, string key)
    {
        foreach (var source in BookRender.Fields(record)?["sources"] as JsonArray ?? [])
        {
            if (source is JsonObject entry
                && string.Equals(BookRender.StrProp(entry, "key"), key, StringComparison.Ordinal))
            {
                return BookRender.StrProp(entry, "url");
            }
        }

        return null;
    }

    /// <summary>Splits one explanation on its level-two headings into an intro and sections.</summary>
    /// <param name="body">The explanation text without its title line.</param>
    /// <returns>The intro and the sections.</returns>
    public static (string Intro, List<(string Heading, string Body)> Items) SplitSections(string body)
    {
        var normalized = body.Replace("\r\n", "\n").Replace('\r', '\n');
        var intro = new List<string>();
        var items = new List<(string Heading, string Body)>();
        string? heading = null;
        var current = new List<string>();
        foreach (var line in normalized.Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal) && line.Length > 3)
            {
                if (heading is not null)
                {
                    items.Add((heading, string.Join("\n", current)));
                }

                heading = line[3..].Trim();
                current = new List<string>();
                continue;
            }

            if (heading is null)
            {
                intro.Add(line);
            }
            else
            {
                current.Add(line);
            }
        }

        if (heading is not null)
        {
            items.Add((heading, string.Join("\n", current)));
        }

        return (string.Join("\n", intro), items);
    }
}
