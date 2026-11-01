using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the generated book stays clean: no object strings or raw JSON in shards, no invented labels, no broken links, and source links on one template.</summary>
public sealed class BookHygieneTests
{
    static readonly Regex HashLink = new("href=\"#([^\"]+)\"", RegexOptions.Compiled);
    static readonly Regex CodeSpan = new("<code\\b[^>]*>.*?</code>", RegexOptions.Compiled | RegexOptions.Singleline);
    static readonly Regex HtmlTag = new("<[^>]+>", RegexOptions.Compiled);

    /// <summary>No shard holds an object coercion or the old raw-JSON fallback block.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Shards_Hold_No_Object_String_Or_Raw_Json()
    {
        using var temp = Cli.Temp("book-hygiene");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var files = Directory.GetFiles(Path.Combine(Out(temp), "data"), "*.js");
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("[object Object]", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<pre class=\"json\">", text, StringComparison.Ordinal);
        }
    }

    /// <summary>Rendered labels never show a raw camel-case field key: the client reads the lexicon, and bodies keep keys inside code spans.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Rendered_Labels_Carry_No_Raw_Camel_Case()
    {
        using var temp = Cli.Temp("book-hygiene");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        // The client reads field labels from the lexicon shard; the mockup's
        // camel-splitting helper did not survive the port.
        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        Assert.Contains("function fieldLabel", script, StringComparison.Ordinal);
        Assert.DoesNotContain("function hum(", script, StringComparison.Ordinal);
        Assert.DoesNotContain("([a-z])([A-Z])", script, StringComparison.Ordinal);

        // Every lexicon label reads as words, never as the camel-case key itself.
        var lexicon = ReadShard(Path.Combine(Out(temp), "data", "lexicon.js"), "lexicon");
        var fields = lexicon["fields"]!.AsObject();
        Assert.True(fields.Count > 0, "The lexicon shard holds no field labels.");
        var keys = new List<string>();
        foreach (var pair in fields)
        {
            var label = pair.Value!.GetValue<string>();
            Assert.False(label.Length > 0 && char.IsLower(label[0]), $"Label '{label}' reads as a raw field key.");
            if (pair.Key.Any(char.IsUpper))
            {
                keys.Add(pair.Key);
            }
        }

        // Rendered bodies show those keys only inside code spans.
        foreach (var file in Directory.GetFiles(Path.Combine(Out(temp), "data"), "records-*.js"))
        {
            var pack = ReadShard(file, Path.GetFileNameWithoutExtension(file));
            foreach (var pair in pack)
            {
                foreach (var html in HtmlOf(pair.Value!.AsObject()))
                {
                    var prose = WithoutCode(html);
                    foreach (var key in keys)
                    {
                        Assert.DoesNotContain(key, prose, StringComparison.Ordinal);
                    }
                }
            }
        }
    }

    /// <summary>Every hash link and outgoing edge in the shards points at a record the shard map holds, and the client shows anything else as the missing-record text.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Generated_Links_Resolve_Or_Show_The_Missing_Record_Text()
    {
        using var temp = Cli.Temp("book-hygiene");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        var known = new HashSet<string>(
            manifest["shardOf"]!.AsObject().Select(pair => pair.Key),
            StringComparer.Ordinal);
        Assert.NotEmpty(known);

        var links = 0;
        var edges = 0;
        foreach (var file in Directory.GetFiles(data, "records-*.js"))
        {
            var pack = ReadShard(file, Path.GetFileNameWithoutExtension(file));
            foreach (var pair in pack)
            {
                var detail = pair.Value!.AsObject();
                foreach (var html in HtmlOf(detail))
                {
                    foreach (Match match in HashLink.Matches(html))
                    {
                        var target = match.Groups[1].Value;
                        if (!target.Contains('/'))
                        {
                            continue;
                        }

                        links++;
                        var hash = target.IndexOf('#');
                        var id = hash < 0 ? target : target[..hash];
                        Assert.True(known.Contains(id), $"Shard {Path.GetFileName(file)} links '{target}', which the shard map holds nowhere.");
                    }
                }

                foreach (var edge in detail["out"] as JsonArray ?? new JsonArray())
                {
                    if (edge is JsonObject link
                        && link["id"] is JsonValue scalar
                        && scalar.TryGetValue<string>(out var id)
                        && id is { Length: > 0 })
                    {
                        edges++;
                        Assert.True(known.Contains(id), $"Shard {Path.GetFileName(file)} links '{id}', which the shard map holds nowhere.");
                    }
                }
            }
        }

        Assert.True(links > 0, "The fixture bodies hold no record links to check.");
        Assert.True(edges > 0, "The fixture records hold no outgoing edges to check.");

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        Assert.Contains("No record with this identifier is in the book", script, StringComparison.Ordinal);
        Assert.Contains("missingRef(b)", script, StringComparison.Ordinal);
        Assert.Contains("missingRef(i)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Outside this sample", script, StringComparison.Ordinal);
    }

    /// <summary>Generated content holds no data-go target: the client renders those buttons itself, only for records and outline nodes it holds, and ignores empty ones.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Data_Go_Buttons_Render_Only_For_Known_Records()
    {
        using var temp = Cli.Temp("book-hygiene");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", Out(temp));
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        foreach (var file in Directory.GetFiles(data, "*.js"))
        {
            Assert.DoesNotContain("data-go", File.ReadAllText(file), StringComparison.Ordinal);
        }

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        Assert.Contains("known = !!(REC[b] || NODE[b])", script, StringComparison.Ordinal);
        Assert.Contains("(REC[b] || NODE[b]) ?", script, StringComparison.Ordinal);
        Assert.Contains("NODE[i] ?", script, StringComparison.Ordinal);
        Assert.Contains("if (t.getAttribute(\"data-go\")) select(t.getAttribute(\"data-go\"))", script, StringComparison.Ordinal);
    }

    /// <summary>Record sources carry a path and line, and the client builds each link from the manifest template.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void Source_Links_Follow_The_Configured_Editor_Template()
    {
        using var temp = Cli.Temp("book-hygiene");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/review.json"),
            "--out", Out(temp),
            "--editor", "myeditor://open?f={root}/{path}&l={line}");
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        Assert.Equal("myeditor://open?f={root}/{path}&l={line}", manifest["editor"]!.GetValue<string>());

        // Record shards carry the path and line; the template lives only in the manifest.
        var sources = 0;
        foreach (var file in Directory.GetFiles(data, "records-*.js"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("myeditor://", text, StringComparison.Ordinal);
            Assert.DoesNotContain("vscode://", text, StringComparison.Ordinal);

            var pack = ReadShard(file, Path.GetFileNameWithoutExtension(file));
            foreach (var pair in pack)
            {
                var src = pair.Value!.AsObject()["src"] as JsonObject;
                var path = src?["path"] is JsonValue scalar && scalar.TryGetValue<string>(out var found) ? found : null;
                if (path is { Length: > 0 })
                {
                    sources++;
                    Assert.DoesNotContain("://", path, StringComparison.Ordinal);
                    Assert.DoesNotContain("{", path, StringComparison.Ordinal);
                }
            }
        }

        Assert.True(sources > 0, "No record carries a source path to check.");

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        Assert.Contains("split(\"{root}\")", script, StringComparison.Ordinal);
        Assert.Contains("split(\"{path}\")", script, StringComparison.Ordinal);
        Assert.Contains("split(\"{line}\")", script, StringComparison.Ordinal);
        Assert.Contains("replace(/:{line}/g, \"\")", script, StringComparison.Ordinal);
    }

    /// <summary>Editor none keeps the manifest template at none and ships no editor link.</summary>
    /// <proves>litespec/core/model#point-every-book-entry-to-its-source</proves>
    [Fact]
    public void Editor_None_Suppresses_Source_Links()
    {
        using var temp = Cli.Temp("book-hygiene");
        var result = Cli.Run(
            "book",
            "--model", Cli.Fixture("Book/review.json"),
            "--out", Out(temp),
            "--editor", "none");
        Assert.Equal(0, result.Status);

        var data = Path.Combine(Out(temp), "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        Assert.Equal("none", manifest["editor"]!.GetValue<string>());

        foreach (var file in Directory.GetFiles(data, "*.js"))
        {
            Assert.DoesNotContain("vscode://", File.ReadAllText(file), StringComparison.Ordinal);
        }

        var script = File.ReadAllText(Path.Combine(Out(temp), "assets", "book.js"));
        Assert.Contains("t === \"none\"", script, StringComparison.Ordinal);
    }

    static string Out(TempFolder temp) => Path.Combine(temp.Path, "out");

    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }

    static IEnumerable<string> HtmlOf(JsonObject detail)
    {
        if (detail["bodyHtml"] is JsonValue body
            && body.TryGetValue<string>(out var html)
            && html.Length > 0)
        {
            yield return html;
        }

        var sections = detail["sections"] as JsonObject;
        if (sections?["intro"] is JsonValue intro
            && intro.TryGetValue<string>(out var lead)
            && lead.Length > 0)
        {
            yield return lead;
        }

        foreach (var item in sections?["list"] as JsonArray ?? new JsonArray())
        {
            if (item is JsonObject section)
            {
                if (section["h"] is JsonValue heading
                    && heading.TryGetValue<string>(out var title)
                    && title.Length > 0)
                {
                    yield return title;
                }

                if (section["html"] is JsonValue part
                    && part.TryGetValue<string>(out var text)
                    && text.Length > 0)
                {
                    yield return text;
                }
            }
        }
    }

    static string WithoutCode(string html) => HtmlTag.Replace(CodeSpan.Replace(html, ""), " ");
}
