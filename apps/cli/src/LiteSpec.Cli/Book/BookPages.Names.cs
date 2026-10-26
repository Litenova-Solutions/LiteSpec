using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>The book generator's names page: one row per module with its names in use, then the lookalike terms.</summary>
static partial class BookPages
{
    /// <summary>The URL of the names page.</summary>
    public static string NamesUrl() => "names/index.html";

    /// <summary>Renders the names page: one row per module, then the lookalike findings.</summary>
    public static string RenderNames(BookIndex index, string rel, ReviewBundle review)
    {
        var modules = index.ByType.TryGetValue("module", out var found) ? found : [];
        var useCases = index.ByType.TryGetValue("use-case", out var cases) ? cases : [];
        var lookalikes = review.Findings.Where(finding => BookRender.StrProp(finding, "code") == "term-lookalike").ToList();
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader(
            "Names",
            "Names",
            "Every name in use, per module, with the lookalike terms.",
            [
                ("Modules", $"<b>{modules.Count}</b>"),
                ("Use cases", $"<b>{useCases.Count}</b>"),
                ("Lookalikes", $"<b>{lookalikes.Count}</b>"),
            ]));
        parts.Add("<p class=\"dim\">Context: this page is rendered from the model and stays outside the approval.</p>");
        parts.Add(BookRender.SectionHeading(2, "modules", "Modules"));

        var rows = string.Concat(modules.Select(module =>
        {
            var id = BookRender.StrProp(module, "id") ?? "";
            var name = BookModel.SplitId(id).Name;
            return "<tr>"
                + $"<td><a href=\"{rel}{BookModel.RecordUrl(id)}\">{BookRender.Esc(module["title"])}</a></td>"
                + $"<td>{VerbsCell(index, name)}</td>"
                + $"<td>{NounsCell(index, name)}</td>"
                + $"<td>{FactsCell(index, rel, name)}</td>"
                + $"<td>{EventsCell(index, rel, name)}</td>"
                + $"<td>{CodesCell(index, rel, name)}</td>"
                + $"<td>{KeysCell(index, rel, name)}</td>"
                + $"<td>{SettingsCell(index, rel, name)}</td>"
                + "</tr>";
        }));
        parts.Add("<div class=\"tablewrap\"><table class=\"tbl\">"
            + "<thead><tr><th>Module</th><th>Verbs</th><th>Nouns</th><th>Reaction facts</th><th>Events</th>"
            + "<th>Refusal codes</th><th>Template keys</th><th>Settings</th></tr></thead>"
            + $"<tbody>{rows}</tbody></table></div>");

        parts.Add(BookRender.SectionHeading(2, "lookalikes", "Lookalikes"));

        if (lookalikes.Count == 0)
        {
            parts.Add("<p>No lookalike terms are reported.</p>");
        }
        else
        {
            var items = string.Concat(lookalikes.Select(finding =>
            {
                var subject = BookRender.IsTruthy(finding?["subject"])
                    && BookRender.StrProp(finding, "subject") is { } subjectId && index.ById.ContainsKey(subjectId)
                    ? $"<p class=\"d\">About {BookRender.RecordLink(index, rel, subjectId)}</p>" : "";
                return "<div class=\"fitem\"><span class=\"sev\"></span><div>"
                    + $"<div class=\"t\"><span class=\"dev-only\"><code>{BookRender.Esc(finding?["code"])}</code> </span>{BookRender.Esc(finding?["message"])}</div>"
                    + subject + "</div></div>";
            }));
            parts.Add(items);
        }

        return string.Join("\n", parts);
    }

    /// <summary>The module's use cases: records of type use-case homed in the module.</summary>
    static IReadOnlyList<JsonObject> ModuleUseCases(BookIndex index, string module)
    {
        var useCases = index.ByType.TryGetValue("use-case", out var cases) ? cases : [];
        return useCases.Where(useCase => BookModel.SplitId(BookRender.StrProp(useCase, "id") ?? "").Home == module).ToList();
    }

    /// <summary>The verbs the module's use cases open with, each with its uses, most used first.</summary>
    static string VerbsCell(BookIndex index, string module)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var useCase in ModuleUseCases(index, module))
        {
            var name = BookModel.SplitId(BookRender.StrProp(useCase, "id") ?? "").Name;
            var dash = name.IndexOf('-');
            var verb = dash < 0 ? name : name[..dash];

            if (verb.Length > 0)
            {
                counts[verb] = counts.GetValueOrDefault(verb) + 1;
            }
        }

        return CountsCell(counts);
    }

    /// <summary>The nouns after the verb in the module's use cases, each with its uses.</summary>
    static string NounsCell(BookIndex index, string module)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var useCase in ModuleUseCases(index, module))
        {
            var name = BookModel.SplitId(BookRender.StrProp(useCase, "id") ?? "").Name;
            var dash = name.IndexOf('-');

            if (dash >= 0 && dash + 1 < name.Length)
            {
                var noun = name[(dash + 1)..];
                counts[noun] = counts.GetValueOrDefault(noun) + 1;
            }
        }

        return CountsCell(counts);
    }

    /// <summary>Counted names as "name (count)" pairs, most used first, then by name.</summary>
    static string CountsCell(Dictionary<string, int> counts) =>
        counts.Count == 0
            ? "<span class=\"dim\">none</span>"
            : BookRender.Esc(string.Join(", ", counts.OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => $"{entry.Key} ({entry.Value})")));

    /// <summary>The facts the module's reactions answer: their trigger ids.</summary>
    static string FactsCell(BookIndex index, string rel, string module)
    {
        var reactions = index.ByType.TryGetValue("reaction", out var found) ? found : [];
        var facts = reactions.Where(reaction => BookModel.SplitId(BookRender.StrProp(reaction, "id") ?? "").Home == module)
                             .SelectMany(reaction => StringItems(BookRender.Fields(reaction)?["on"]))
                             .Distinct(StringComparer.Ordinal)
                             .Order(StringComparer.Ordinal)
                             .ToList();
        return NamesCell(index, rel, facts, true);
    }

    /// <summary>The module's events: event records raised by its aggregates.</summary>
    static string EventsCell(BookIndex index, string rel, string module)
    {
        var aggregates = index.ByType.TryGetValue("aggregate", out var found) ? found : [];
        var owned = aggregates.Where(aggregate => BookModel.SplitId(BookRender.StrProp(aggregate, "id") ?? "").Home == module)
                              .Select(aggregate => BookModel.SplitId(BookRender.StrProp(aggregate, "id") ?? "").Name)
                              .ToHashSet(StringComparer.Ordinal);
        var events = index.ByType.TryGetValue("event", out var listed) ? listed : [];
        var ids = events.Select(record => BookRender.StrProp(record, "id") ?? "")
                        .Where(id => owned.Contains(BookModel.SplitId(id).Home ?? ""))
                        .Order(StringComparer.Ordinal)
                        .ToList();
        return NamesCell(index, rel, ids, true);
    }

    /// <summary>The refusal codes of the module's use cases.</summary>
    static string CodesCell(BookIndex index, string rel, string module)
    {
        var owned = ModuleUseCases(index, module)
                    .Select(useCase => BookModel.SplitId(BookRender.StrProp(useCase, "id") ?? "").Name)
                    .ToHashSet(StringComparer.Ordinal);
        var results = index.ByType.TryGetValue("result", out var found) ? found : [];
        var codes = results.Where(result => owned.Contains(BookModel.SplitId(BookRender.StrProp(result, "id") ?? "").Home ?? ""))
                           .Select(result => BookRender.Fields(result)?["code"] is JsonValue scalar
                               && scalar.TryGetValue<string>(out var code) ? code ?? "" : "")
                           .Where(code => code.Length > 0)
                           .Distinct(StringComparer.Ordinal)
                           .Order(StringComparer.Ordinal)
                           .ToList();
        return NamesCell(index, rel, codes, false);
    }

    /// <summary>The module's notification template keys.</summary>
    static string KeysCell(BookIndex index, string rel, string module)
    {
        var notifications = index.ByType.TryGetValue("notification", out var found) ? found : [];
        var keys = notifications.Where(notification => BookModel.SplitId(BookRender.StrProp(notification, "id") ?? "").Home == module)
                                .SelectMany(notification => StringItems(BookRender.Fields(notification)?["template"]))
                                .Distinct(StringComparer.Ordinal)
                                .Order(StringComparer.Ordinal)
                                .ToList();
        return NamesCell(index, rel, keys, false);
    }

    /// <summary>The module's setting names.</summary>
    static string SettingsCell(BookIndex index, string rel, string module)
    {
        var settings = index.ByType.TryGetValue("setting", out var found) ? found : [];
        var names = settings.Where(setting => BookModel.SplitId(BookRender.StrProp(setting, "id") ?? "").Home == module)
                            .Select(setting => BookModel.SplitId(BookRender.StrProp(setting, "id") ?? "").Name)
                            .Where(name => name.Length > 0)
                            .Distinct(StringComparer.Ordinal)
                            .Order(StringComparer.Ordinal)
                            .ToList();
        return NamesCell(index, rel, names, false);
    }

    /// <summary>One cell of names: record links when they resolve, words otherwise.</summary>
    static string NamesCell(BookIndex index, string rel, IReadOnlyList<string> names, bool link)
    {
        if (names.Count == 0)
        {
            return "<span class=\"dim\">none</span>";
        }

        // Facts and events resolve to records; codes, keys, and settings are words as written.
        return string.Join(", ", names.Select(name => link && index.ById.ContainsKey(name)
            ? BookRender.RecordLink(index, rel, name)
            : BookRender.Esc(name)));
    }

    /// <summary>The string items of a field that holds one string or an array of strings.</summary>
    static IReadOnlyList<string> StringItems(JsonNode? field) =>
        field switch
        {
            JsonValue scalar when scalar.TryGetValue<string>(out var text) && text is { Length: > 0 } => [text],
            JsonArray items => items.OfType<JsonValue>()
                                    .Select(item => item.TryGetValue<string>(out var text) ? text ?? "" : "")
                                    .Where(text => text.Length > 0)
                                    .ToList(),
            _ => [],
        };
}
