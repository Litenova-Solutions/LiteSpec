using System.Globalization;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model;

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

            var captures = ForRoute(model, address, record.Field("entryPoint"));

            model.Captures[record.Id] = captures;
            model.CaptureState[record.Id] = captures.Count > 0 ? "captured" : "route-not-captured";
        }
    }

    /// <summary>The captures the galleries hold for one route address, in theme and width order.</summary>
    /// <param name="model">The built project model holding the consumer configuration.</param>
    /// <param name="address">The route address the screen serves, or null when it serves none.</param>
    /// <param name="entryPoint">The entry point the screen belongs to, or null when unknown; a gallery inside a frontend folder serves only that frontend's entry point.</param>
    /// <returns>The captures matching the route slug under the configured pattern, or an empty list when none matches.</returns>
    public static IReadOnlyList<ScreenCapture> ForRoute(ProjectModel model, string? address, string? entryPoint = null)
    {
        if (address is not { Length: > 0 } || model.Config.ScreenshotGalleries.Count == 0)
        {
            return [];
        }

        var matcher = Matcher(model.Config.ScreenshotPattern, RouteSlug(address));
        var captures = new List<ScreenCapture>();

        foreach (var gallery in model.Config.ScreenshotGalleries)
        {
            if (!Directory.Exists(gallery) || !ServesEntryPoint(model, gallery, entryPoint))
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
                    PathText.Relative(model.Config.Root, file),
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
    ///     Whether a gallery folder may hold captures for a screen of the entry point: a gallery
    ///     inside a declared frontend folder holds only that frontend's routes, so two frontends
    ///     that both serve <c>/</c> never share a capture.
    /// </summary>
    /// <param name="model">The built project model holding the declared frontends.</param>
    /// <param name="gallery">The absolute gallery folder.</param>
    /// <param name="entryPoint">The screen's entry point, or null when unknown.</param>
    /// <returns>True when the gallery belongs to no frontend, the entry point is unknown, or the frontend serves it.</returns>
    static bool ServesEntryPoint(ProjectModel model, string gallery, string? entryPoint)
    {
        if (entryPoint is not { Length: > 0 })
        {
            return true;
        }

        var full = Path.GetFullPath(gallery);
        var owner = model.Config.Frontends.FirstOrDefault(frontend =>
            full.StartsWith(Path.GetFullPath(frontend.Path) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

        return owner?.EntryPoint is not { Length: > 0 } served || string.Equals(served, entryPoint, StringComparison.Ordinal);
    }

    /// <summary>The route slug of an address: the path without its leading slash, segments joined with a hyphen, parameters reduced to their names, and the root named <c>home</c>.</summary>
    /// <param name="address">The route address.</param>
    /// <returns>The slug the gallery pattern's <c>{route}</c> placeholder matches.</returns>
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
