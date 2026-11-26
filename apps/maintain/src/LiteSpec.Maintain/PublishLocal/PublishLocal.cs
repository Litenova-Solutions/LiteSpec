using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LiteSpec.Maintain.PublishLocal;

/// <summary>
///     The file edits behind <c>litespec-maintain publish-local</c>: the dev version text, the
///     NuGet feed source, the tool manifest entry, and the analyzer version in central package files.
/// </summary>
/// <follows>litespec/core/release#run-one-litespec-build</follows>
static class PublishLocal
{
    /// <summary>The NuGet source key the command adds to the consumer's config.</summary>
    internal const string FeedKey = "litespec-local";

    /// <summary>An MSBuild package element that can carry the analyzer version.</summary>
    static readonly Regex PackageElement = new(
        @"<\s*(PackageVersion|PackageReference)\b([^>]*?)(/?)>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>The Include or Update attribute naming the analyzer package.</summary>
    static readonly Regex AnalyzerName = new(
        """\b(Include|Update)\s*=\s*["']LiteSpec\.Analyzers["']""",
        RegexOptions.Compiled);

    /// <summary>The Version attribute of a package element.</summary>
    static readonly Regex VersionAttribute = new(
        """\bVersion\s*=\s*["']""",
        RegexOptions.Compiled);

    /// <summary>The value attribute of a package source element.</summary>
    static readonly Regex ValueAttribute = new(
        """\bvalue\s*=\s*["']""",
        RegexOptions.Compiled);

    /// <summary>The Version child element of a package reference.</summary>
    static readonly Regex VersionElement = new(
        @"<\s*Version\s*>([^<]+)<\s*/\s*Version\s*>",
        RegexOptions.Compiled);

    /// <summary>The package source element naming the local feed.</summary>
    static readonly Regex FeedSource = new(
        """<add\b[^>]*?\bkey\s*=\s*["']""" + Regex.Escape(FeedKey) + """["'][^>]*?>""",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>An indentation run at the start of a line.</summary>
    static readonly Regex LineIndent = new(
        @"(?m)^(?<indent>[ \t]*)<add\b",
        RegexOptions.Compiled);

    /// <summary>The dev version for a release and a UTC stamp: the release with a <c>-dev</c> timestamp suffix.</summary>
    /// <param name="baseVersion">The manifest release, such as 7.0.0.</param>
    /// <param name="stampUtc">The UTC stamp the version carries.</param>
    /// <returns>The dev version, such as 7.0.0-dev.20261124090000.</returns>
    internal static string DevVersion(string baseVersion, DateTime stampUtc) =>
        $"{baseVersion}-dev.{stampUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}";

    /// <summary>The dev version CI publishes for a release-branch run: the release with a <c>-dev</c> run suffix.</summary>
    /// <param name="baseVersion">The manifest release, such as 7.0.0.</param>
    /// <param name="run">The CI run number, such as 42.</param>
    /// <returns>The dev version, such as 7.0.0-dev.42.</returns>
    internal static string CiDevVersion(string baseVersion, string run) =>
        $"{baseVersion}-dev.{run}";

    /// <summary>Whether a <c>--from-ci</c> value names a CI run: a positive whole number.</summary>
    /// <param name="value">The option value.</param>
    /// <returns>True when the value holds ASCII digits naming a number above zero; otherwise false.</returns>
    internal static bool IsRunNumber(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        var nonzero = false;

        foreach (var digit in value)
        {
            if (!char.IsAsciiDigit(digit))
            {
                return false;
            }

            if (digit != '0')
            {
                nonzero = true;
            }
        }

        return nonzero;
    }

    /// <summary>The feed value the NuGet config carries: relative to the consumer when the feed sits under it, else absolute.</summary>
    /// <param name="feed">The absolute feed folder.</param>
    /// <param name="consumer">The absolute consumer folder holding the NuGet config.</param>
    /// <returns>The config value with forward-slash segments.</returns>
    internal static string FeedValueFor(string feed, string consumer)
    {
        var relative = Path.GetRelativePath(consumer, feed);

        return Path.IsPathRooted(relative)
            ? feed.Replace(Path.DirectorySeparatorChar, '/')
            : relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>Adds the local feed source to a NuGet config, or moves it to the feed.</summary>
    /// <param name="text">The config text, or null when the consumer holds no config.</param>
    /// <param name="feedValue">The feed value the source carries.</param>
    /// <returns>The config text and whether it changed.</returns>
    /// <exception cref="InvalidOperationException">The config holds no configuration element to add the source to.</exception>
    internal static (string Text, bool Changed) EnsureFeedSource(string? text, string feedValue)
    {
        var value = Escape(feedValue);

        if (text is null)
        {
            return ($"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <add key="{FeedKey}" value="{value}" />
                  </packageSources>
                </configuration>

                """, true);
        }

        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var source = FeedSource.Match(text);

        if (source.Success)
        {
            return SetSourceValue(text, source, value);
        }

        var sourcesOpen = text.IndexOf("<packageSources", StringComparison.Ordinal);

        if (sourcesOpen >= 0)
        {
            var tagEnd = text.IndexOf('>', sourcesOpen);

            if (tagEnd < 0)
            {
                throw new InvalidOperationException("The NuGet config holds a <packageSources> element with no closing bracket.");
            }

            if (text[tagEnd - 1] == '/')
            {
                var replacement = $"<packageSources>{newline}    <add key=\"{FeedKey}\" value=\"{value}\" />{newline}</packageSources>";

                return (text[..sourcesOpen] + replacement + text[(tagEnd + 1)..], true);
            }

            var close = text.IndexOf("</packageSources>", sourcesOpen, StringComparison.Ordinal);

            if (close < 0)
            {
                throw new InvalidOperationException("The NuGet config holds a <packageSources> element with no closing tag.");
            }

            var block = text[sourcesOpen..close];
            var indentMatch = LineIndent.Match(block);
            var lineStart = text.LastIndexOf('\n', close) + 1;
            var closingIndent = text[lineStart..close].TakeWhile(char.IsWhiteSpace);
            var indent = indentMatch.Success
                ? indentMatch.Groups["indent"].Value
                : new string([.. closingIndent]) + "  ";
            var entry = $"{indent}<add key=\"{FeedKey}\" value=\"{value}\" />{newline}";

            return (text[..lineStart] + entry + text[lineStart..], true);
        }

        var configurationClose = text.IndexOf("</configuration>", StringComparison.Ordinal);

        if (configurationClose < 0)
        {
            throw new InvalidOperationException("The NuGet config holds no <configuration> element to add the feed source to.");
        }

        var insertAt = text.LastIndexOf('\n', configurationClose) + 1;
        var section = $"  <packageSources>{newline}    <add key=\"{FeedKey}\" value=\"{value}\" />{newline}  </packageSources>{newline}";

        return (text[..insertAt] + section + text[insertAt..], true);
    }

    /// <summary>Moves the feed source in a config to the feed.</summary>
    /// <param name="text">The config text.</param>
    /// <param name="source">The source element match.</param>
    /// <param name="value">The escaped feed value the source carries.</param>
    /// <returns>The config text and whether it changed.</returns>
    static (string Text, bool Changed) SetSourceValue(string text, Match source, string value)
    {
        var element = source.Value;
        var valueMatch = ValueAttribute.Match(element);

        if (!valueMatch.Success)
        {
            var insertAt = element.EndsWith("/>", StringComparison.Ordinal) ? element.Length - 2 : element.Length - 1;
            var withValue = element[..insertAt] + $" value=\"{value}\"" + element[insertAt..];

            return (text[..source.Index] + withValue + text[(source.Index + source.Length)..], true);
        }

        var quote = element[valueMatch.Index + valueMatch.Length - 1];
        var open = source.Index + valueMatch.Index + valueMatch.Length;
        var close = text.IndexOf(quote, open);

        if (close < 0 || close > source.Index + source.Length)
        {
            var repaired = element[..(valueMatch.Index + valueMatch.Length)] + $"{value}\"" + ">";

            return (text[..source.Index] + repaired + text[(source.Index + source.Length)..], true);
        }

        if (string.Equals(text[open..close], value, StringComparison.Ordinal))
        {
            return (text, false);
        }

        return (text[..open] + value + text[close..], true);
    }

    /// <summary>Escapes a config value for an XML attribute.</summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The value with XML metacharacters escaped.</returns>
    static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    /// <summary>The local tool manifest pinning the LiteSpec.Cli tool at a version.</summary>
    /// <param name="version">The LiteSpec package version to pin.</param>
    /// <returns>The dotnet-tools manifest JSON text.</returns>
    internal static string ToolManifest(string version) => $$"""
        {
          "version": 1,
          "isRoot": true,
          "tools": {
            "LiteSpec.Cli": {
              "version": "{{version}}",
              "commands": ["litespec"]
            }
          }
        }
        """ + "\n";

    /// <summary>Sets the LiteSpec.Cli version in a tool manifest, creating the manifest when the consumer holds none.</summary>
    /// <param name="text">The manifest text, or null when the consumer holds no manifest.</param>
    /// <param name="version">The LiteSpec package version to pin.</param>
    /// <returns>The manifest text and whether it changed.</returns>
    /// <exception cref="InvalidOperationException">The manifest is not valid JSON or holds no tools object.</exception>
    internal static (string Text, bool Changed) SetToolVersion(string? text, string version)
    {
        if (text is null)
        {
            return (ToolManifest(version), true);
        }

        JsonNode? manifest;

        try
        {
            manifest = JsonNode.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"The tool manifest is not valid JSON: {exception.Message}", exception);
        }

        if ((manifest as JsonObject)?["tools"] is not JsonObject tools)
        {
            throw new InvalidOperationException("The tool manifest holds no tools object to pin LiteSpec.Cli in.");
        }

        string? current = null;

        if (tools["LiteSpec.Cli"] is JsonObject entry
            && entry["version"] is JsonValue value
            && value.TryGetValue<string>(out var pinned)
            && pinned.Length > 0)
        {
            current = pinned;
        }

        if (current == version)
        {
            return (text, false);
        }

        // A version-only change keeps the file byte for byte except the version itself.
        if (current is { Length: > 0 } && ReplaceVersionValue(text, current, version) is { } replaced)
        {
            return (replaced, true);
        }

        tools["LiteSpec.Cli"] = new JsonObject
        {
            ["version"] = version,
            ["commands"] = new JsonArray("litespec"),
        };

        return (manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n", true);
    }

    /// <summary>Replaces the pinned tool version in the manifest text without touching anything else.</summary>
    /// <param name="text">The manifest text.</param>
    /// <param name="current">The version the manifest pins now.</param>
    /// <param name="version">The version to pin.</param>
    /// <returns>The manifest text with the version replaced, or null when the text does not hold the version where the entry stands.</returns>
    static string? ReplaceVersionValue(string text, string current, string version)
    {
        var key = text.IndexOf("\"LiteSpec.Cli\"", StringComparison.Ordinal);

        if (key < 0)
        {
            return null;
        }

        var name = text.IndexOf("\"version\"", key, StringComparison.Ordinal);

        if (name < 0)
        {
            return null;
        }

        var colon = text.IndexOf(':', name);

        if (colon < 0)
        {
            return null;
        }

        var open = text.IndexOf('"', colon);

        if (open < 0)
        {
            return null;
        }

        var close = text.IndexOf('"', open + 1);

        if (close < 0 || !string.Equals(text[(open + 1)..close], current, StringComparison.Ordinal))
        {
            return null;
        }

        return text[..(open + 1)] + version + text[close..];
    }

    /// <summary>Reads the LiteSpec.Analyzers version one MSBuild file states.</summary>
    /// <param name="text">The file text.</param>
    /// <returns>The first analyzer version the file states, or null when it states none.</returns>
    internal static string? AnalyzerVersionIn(string text)
    {
        foreach (var edit in AnalyzerEdits(text))
        {
            return edit.Version;
        }

        return null;
    }

    /// <summary>Sets the LiteSpec.Analyzers version one MSBuild file states.</summary>
    /// <param name="text">The file text.</param>
    /// <param name="version">The LiteSpec package version to state.</param>
    /// <returns>The file text and how many stated versions changed.</returns>
    internal static (string Text, int Changed) SetAnalyzerVersion(string text, string version)
    {
        var edits = AnalyzerEdits(text).ToList();
        var changed = 0;
        var result = text;

        foreach (var (offset, length, current) in edits.OrderByDescending(edit => edit.Offset))
        {
            if (current == version)
            {
                continue;
            }

            result = result[..offset] + version + result[(offset + length)..];
            changed++;
        }

        return (result, changed);
    }

    /// <summary>Every analyzer version one MSBuild file states, with the span holding it.</summary>
    /// <param name="text">The file text.</param>
    /// <returns>The offset, length, and current text of each stated version.</returns>
    static List<(int Offset, int Length, string Version)> AnalyzerEdits(string text)
    {
        var edits = new List<(int Offset, int Length, string Version)>();

        foreach (Match element in PackageElement.Matches(text))
        {
            var attributes = element.Groups[2].Value;

            if (!AnalyzerName.IsMatch(attributes))
            {
                continue;
            }

            var version = VersionAttribute.Match(element.Value);

            if (version.Success)
            {
                var quote = element.Value[version.Index + version.Length - 1];
                var open = element.Index + version.Index + version.Length;
                var close = text.IndexOf(quote, open);

                if (close >= 0 && close <= element.Index + element.Length)
                {
                    edits.Add((open, close - open, text[open..close]));
                    continue;
                }
            }

            // A direct project reference can carry its version as a child element instead.
            if (element.Groups[3].Value.Length == 0 && element.Groups[1].Value == "PackageReference")
            {
                var body = text.Substring(element.Index, Math.Min(2000, text.Length - element.Index));
                var end = body.IndexOf("</PackageReference", StringComparison.Ordinal);

                if (end >= 0)
                {
                    var child = VersionElement.Match(body[..end]);

                    if (child.Success)
                    {
                        var value = child.Groups[1].Value;
                        var start = value.Length - value.TrimStart().Length;
                        var trimmed = value.Trim();

                        edits.Add((element.Index + child.Groups[1].Index + start, trimmed.Length, trimmed));
                    }
                }
            }
        }

        return edits;
    }
}
