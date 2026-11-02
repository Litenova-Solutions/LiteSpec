using System.Globalization;
using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>
///     Records each screen's screenshot captures from the gallery folders the
///     project names. The route slug of a screen address drops the leading
///     slash, joins segments with a hyphen, reduces a bracketed, braced, colon
///     or dollar parameter to its name, and names the root route <c>home</c>.
///     A missing gallery folder captures nothing and fails nothing.
/// </summary>
/// <follows>litespec/core/model#record-each-screens-captures-from-the-gallery</follows>
static class ScreenshotCaptures
{
    /// <summary>Computes the captures and capture state of every screen record in the model.</summary>
    /// <param name="model">The built project model holding the screen records and the read routes.</param>
    public static void Compute(ProjectModel model)
    {
        model.Captures.Clear();
        model.CaptureState.Clear();

        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Screen))
        {
            var address = record.Field(RecordFields.Address)
                ?? model.Screens.FirstOrDefault(screen => screen.ScreenId == record.Id)?.Route;

            if (address is not { Length: > 0 })
            {
                model.Captures[record.Id] = [];
                model.CaptureState[record.Id] = "no-route";
                continue;
            }

            var captures = ForRoute(model, address);

            model.Captures[record.Id] = captures;
            model.CaptureState[record.Id] = captures.Count > 0 ? "captured" : "route-not-captured";
        }
    }

    /// <summary>The captures the galleries hold for one route address, in theme and width order.</summary>
    /// <param name="model">The built project model holding the consumer configuration.</param>
    /// <param name="address">The route address the screen serves, or null when it serves none.</param>
    /// <returns>The captures matching the route slug under the configured pattern, or an empty list when none matches.</returns>
    public static IReadOnlyList<ScreenCapture> ForRoute(ProjectModel model, string? address)
    {
        if (address is not { Length: > 0 } || model.Config.ScreenshotGalleries.Count == 0)
        {
            return [];
        }

        var matcher = Matcher(model.Config.ScreenshotPattern, RouteSlug(address));
        var captures = new List<ScreenCapture>();

        foreach (var gallery in model.Config.ScreenshotGalleries)
        {
            if (!Directory.Exists(gallery))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(gallery, "*", SearchOption.AllDirectories))
            {
                var match = matcher.Match(Path.GetFileName(file));

                if (!match.Success
                    || !int.TryParse(match.Groups["width"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var width))
                {
                    continue;
                }

                captures.Add(new ScreenCapture(
                    model.Config.Relative(file),
                    match.Groups["theme"].Value,
                    width,
                    File.GetLastWriteTimeUtc(file).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            }
        }

        return captures
            .OrderBy(capture => capture.Theme, StringComparer.Ordinal)
            .ThenByDescending(capture => capture.Width)
            .ThenBy(capture => capture.Path, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    ///     The route slug of a screen address: the leading slash dropped, segments
    ///     joined with a hyphen, a parameter segment reduced to its name, and the
    ///     root route named <c>home</c>.
    /// </summary>
    /// <param name="address">The route address, such as <c>/events/[id]</c>.</param>
    /// <returns>The route slug, such as <c>events-id</c>, or <c>home</c> for the root route.</returns>
    public static string RouteSlug(string address)
    {
        var path = address.Split('?', '#')[0].Trim().Trim('/');
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(SegmentSlug)
            .Where(segment => segment.Length > 0)
            .ToList();

        return segments.Count == 0 ? "home" : string.Join("-", segments);
    }

    /// <summary>One route segment's slug: a parameter reduced to its name, any other segment as written.</summary>
    /// <param name="segment">The route segment to slug.</param>
    /// <returns>The parameter name for a parameter segment, else the segment.</returns>
    static string SegmentSlug(string segment)
    {
        var trimmed = segment.Trim();

        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            return trimmed[1..^1].Split(':', '=')[0];
        }

        if (trimmed.StartsWith('{') && trimmed.Contains('}'))
        {
            return trimmed[1..trimmed.IndexOf('}')].Split(':')[0];
        }

        if (trimmed.StartsWith(':') || trimmed.StartsWith('$'))
        {
            return trimmed[1..].Split('.')[0];
        }

        return trimmed;
    }

    /// <summary>The file-name matcher for a pattern and a route slug, with theme and width groups.</summary>
    /// <param name="pattern">The file-name pattern with <c>{route}</c>, <c>{theme}</c> and <c>{width}</c> placeholders.</param>
    /// <param name="slug">The route slug the <c>{route}</c> placeholder matches.</param>
    /// <returns>The anchored matcher for one capture file name.</returns>
    static Regex Matcher(string pattern, string slug)
    {
        var parts = pattern.Split(["{route}", "{theme}", "{width}"], StringSplitOptions.None);
        var names = Placeholders(pattern);
        var built = "^";

        for (var index = 0; index < parts.Length; index++)
        {
            built += Regex.Escape(parts[index]);

            if (index < names.Count)
            {
                built += names[index] switch
                {
                    "route" => Regex.Escape(slug),
                    "theme" => "(?<theme>[a-z][a-z0-9-]*)",
                    _ => "(?<width>[0-9]+)",
                };
            }
        }

        return new Regex(built + "$", RegexOptions.CultureInvariant);
    }

    /// <summary>The placeholder names a pattern carries, in order.</summary>
    /// <param name="pattern">The file-name pattern to scan.</param>
    /// <returns>The placeholder names in the order they appear.</returns>
    static List<string> Placeholders(string pattern)
    {
        var names = new List<string>();
        var rest = pattern;

        while (rest.Contains('{'))
        {
            var open = rest.IndexOf('{');
            var close = rest.IndexOf('}', open);

            if (close < 0)
            {
                break;
            }

            names.Add(rest[(open + 1)..close]);
            rest = rest[(close + 1)..];
        }

        return names;
    }
}
