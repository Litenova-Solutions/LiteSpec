using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The body detail of one record: its body rendered to HTML with hash record
///     links, split by section.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookBodyDetail
{
    /// <summary>Renders one record body to HTML with hash record links, split by section.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="record">The record.</param>
    /// <returns>The body HTML and its sections.</returns>
    public static (string Html, JsonObject Sections) Render(BookIndex index, JsonObject record)
    {
        var body = BookRender.StrProp(record, "body") ?? "";
        (string Href, string Title)? LinkRecord(string target)
        {
            var targetBase = BookDetailValues.BaseId(target);
            if (!index.ById.TryGetValue(targetBase, out var found))
            {
                return null;
            }

            return ("#" + target, BookRender.StrProp(found, "title") ?? targetBase);
        }

        var split = SplitSections(body);
        var items = new JsonArray();
        foreach (var section in split.Items)
        {
            items.Add(new JsonObject
            {
                ["h"] = section.Heading,
                ["html"] = BookMarkdown.Render(section.Body, LinkRecord),
            });
        }

        return (BookMarkdown.Render(body, LinkRecord), new JsonObject
        {
            ["intro"] = BookMarkdown.Render(split.Intro, LinkRecord),
            ["list"] = items,
        });
    }

    /// <summary>Splits one body on its level-two headings into an intro and sections.</summary>
    /// <param name="body">The record body.</param>
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
