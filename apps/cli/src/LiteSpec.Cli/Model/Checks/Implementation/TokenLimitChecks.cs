using System.Globalization;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using ResolvedColor = (double Red, double Green, double Blue, int Line, string? Problem);

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The token limit check: every token role meets its limit in every declared color scheme,
///     read from the token file the design contract names.
/// </summary>
/// <follows>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</follows>
internal static class TokenLimitChecks
{
    /// <summary>The slack a limit comparison allows for floating-point arithmetic.</summary>
    private const double Tolerance = 1e-9;

    /// <summary>The color schemes a token file declares: the top-level groups it holds.</summary>
    private static readonly string[] Schemes = ["light", "dark"];

    /// <summary>The five tones, each with text, fill and edge.</summary>
    private static readonly string[] Tones = ["neutral", "positive", "caution", "destructive", "info"];

    /// <summary>An rgb() color with integer or percent channels and an optional alpha.</summary>
    private static readonly Regex RgbColor = new(
        @"^rgba?\(\s*(?<red>[^,]+?)\s*,\s*(?<green>[^,]+?)\s*,\s*(?<blue>[^,)]+?)\s*(?:,[^)]*)?\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Reports design-token-limit-failed.</summary>
    /// <param name="model">The loaded project model that holds the consumer root.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        if (TokensFileOf(model) is not { } tokensFile)
        {
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText(tokensFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        JsonText root;

        try
        {
            root = JsonText.Parse(text);
        }
        catch (JsonTextException)
        {
            return;
        }

        if (root.Kind != JsonKind.Object)
        {
            return;
        }

        foreach (var scheme in Schemes)
        {
            if (root.TryGetProperty(scheme, out var group) && group.Kind == JsonKind.Object)
            {
                CheckScheme(tokensFile, root, scheme, group, findings);
            }
        }
    }

    /// <summary>The token file the design contract names, or null when no contract names one that exists.</summary>
    /// <param name="model">The loaded project model that holds the consumer root.</param>
    /// <returns>The absolute token file path, or null.</returns>
    private static string? TokensFileOf(ProjectModel model)
    {
        try
        {
            var projectFile = ConsumerRoot.ProjectFile(model.Config.Root);

            if (!File.Exists(projectFile))
            {
                return null;
            }

            var project = YamlText.Parse(File.ReadAllText(projectFile));
            var contract = ChildOf(ChildOf(ChildOf(project, "ui"), "package"), "designContract")?.GetString();

            if (contract is not { Length: > 0 })
            {
                return null;
            }

            var contractFile = Combine(model.Config.Root, contract);

            if (!File.Exists(contractFile) || MetadataOf(contractFile) is not { } metadata)
            {
                return null;
            }

            var tokens = ChildOf(metadata, "tokens")?.GetString();

            if (tokens is not { Length: > 0 })
            {
                return null;
            }

            var file = Combine(model.Config.Root, tokens);

            if (File.Exists(file))
            {
                return file;
            }

            var beside = Combine(Path.GetDirectoryName(contractFile) ?? model.Config.Root, tokens);

            return File.Exists(beside) ? beside : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or JsonTextException)
        {
            return null;
        }
    }

    /// <summary>The named child of a JSON object, or null when the value is no object or holds no such child.</summary>
    /// <param name="value">The value whose child is read, or null.</param>
    /// <param name="name">The child name.</param>
    /// <returns>The child, or null.</returns>
    private static JsonText? ChildOf(JsonText? value, string name) =>
        value is { Kind: JsonKind.Object } && value.TryGetProperty(name, out var child) ? child : null;

    /// <summary>Resolves a root-relative path to an absolute path.</summary>
    /// <param name="root">The absolute root the path resolves against.</param>
    /// <param name="relative">The path with forward slashes.</param>
    /// <returns>The absolute path.</returns>
    private static string Combine(string root, string relative) =>
        Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>The metadata block opening a design contract, or null when it carries none.</summary>
    /// <param name="contractFile">The absolute design contract path.</param>
    /// <returns>The parsed metadata, or null.</returns>
    private static JsonText? MetadataOf(string contractFile)
    {
        string raw;

        try
        {
            raw = File.ReadAllText(contractFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (!raw.StartsWith("---", StringComparison.Ordinal))
        {
            return null;
        }

        var end = raw.IndexOf("\n---", 3, StringComparison.Ordinal);

        if (end < 0)
        {
            return null;
        }

        try
        {
            return YamlText.Parse(raw.Substring(3, end - 3).Trim('\r', '\n'));
        }
        catch (JsonTextException)
        {
            return null;
        }
    }

    /// <summary>Evaluates every token limit in one scheme.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="root">The token file aliases resolve against.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="group">The scheme group.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckScheme(string tokensFile, JsonText root, string scheme, JsonText group, List<Finding> findings)
    {
        var roles = new Dictionary<string, ResolvedColor>(StringComparer.Ordinal);

        foreach (var role in RoleNames())
        {
            var resolved = ResolveRole(root, group, role);
            roles[role] = resolved;

            if (resolved.Problem is { } problem)
            {
                findings.Add(new Finding(
                    "design-token-limit-failed",
                    $"Token role '{role}' has no color in scheme '{scheme}': {problem}.",
                    tokensFile, resolved.Line, null));
            }
        }

        CheckContrastLimits(tokensFile, scheme, roles, findings);
        CheckSurfaces(tokensFile, scheme, roles, findings);
        CheckTones(tokensFile, scheme, roles, findings);
        CheckCharts(tokensFile, scheme, roles, findings);
        CheckMagnitude(tokensFile, root, scheme, group, findings);
    }

    /// <summary>Every token role with a limit, in finding order.</summary>
    /// <returns>The role names.</returns>
    private static List<string> RoleNames()
    {
        var roles = new List<string>
        {
            "page", "layer-1", "layer-2", "layer-accent", "separator", "layer-edge", "control-boundary", "text", "muted-text",
            "primary", "primary-text", "focus-ring",
        };

        foreach (var tone in Tones)
        {
            roles.Add($"tone.{tone}.text");
            roles.Add($"tone.{tone}.fill");
            roles.Add($"tone.{tone}.edge");
        }

        roles.Add("chart.1");
        roles.Add("chart.2");
        roles.Add("chart.3");

        return roles;
    }

    /// <summary>Resolves one role of a scheme to its color.</summary>
    /// <param name="root">The token file aliases resolve against.</param>
    /// <param name="group">The scheme group.</param>
    /// <param name="role">The role name, with dots between group keys.</param>
    /// <returns>The color with its line, or the reason the role has none.</returns>
    private static ResolvedColor ResolveRole(JsonText root, JsonText group, string role)
    {
        var node = group;
        var line = group.Line;

        foreach (var segment in role.Split('.'))
        {
            if (node.Kind != JsonKind.Object || !node.TryGetProperty(segment, out var child))
            {
                return (0, 0, 0, line, $"it names no '{segment}' token");
            }

            node = child;
            line = child.Line;
        }

        return ResolveValue(root, node, line, new HashSet<string>(StringComparer.Ordinal));
    }

    /// <summary>Resolves one token node to its color, following aliases.</summary>
    /// <param name="root">The token file aliases resolve against.</param>
    /// <param name="node">The token node.</param>
    /// <param name="line">The line of the role's value.</param>
    /// <param name="seen">The alias paths already followed, against circles.</param>
    /// <returns>The color with its line, or the reason the role has none.</returns>
    private static ResolvedColor ResolveValue(JsonText root, JsonText node, int line, HashSet<string> seen)
    {
        var value = node.Kind == JsonKind.Object && node.TryGetProperty("$value", out var held)
            ? held.GetString()
            : node.GetString();

        if (value is null)
        {
            return (0, 0, 0, line, "its value holds no text");
        }

        var trimmed = value.Trim();

        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            var path = trimmed[1..^1].Trim();

            if (path.Length == 0)
            {
                return (0, 0, 0, line, $"its alias '{trimmed}' names nothing");
            }

            if (!seen.Add(path))
            {
                return (0, 0, 0, line, $"its alias '{trimmed}' circles back");
            }

            var target = root;

            foreach (var segment in path.Split('.'))
            {
                if (target.Kind != JsonKind.Object || !target.TryGetProperty(segment, out var child))
                {
                    return (0, 0, 0, line, $"its alias '{trimmed}' resolves to nothing");
                }

                target = child;
            }

            return ResolveValue(root, target, line, seen);
        }

        if (!TryParseColor(trimmed, out var red, out var green, out var blue))
        {
            return (0, 0, 0, line, $"its value '{trimmed}' is no hex or rgb() color");
        }

        return (red, green, blue, line, null);
    }

    /// <summary>Reads a hex or rgb() color into channels of zero to one.</summary>
    /// <param name="text">The trimmed color text.</param>
    /// <param name="red">The red channel, or zero when the text is no color.</param>
    /// <param name="green">The green channel, or zero when the text is no color.</param>
    /// <param name="blue">The blue channel, or zero when the text is no color.</param>
    /// <returns>True when the text is a hex or rgb() color; otherwise false.</returns>
    private static bool TryParseColor(string text, out double red, out double green, out double blue)
    {
        red = 0;
        green = 0;
        blue = 0;

        if (text.StartsWith('#'))
        {
            return TryParseHex(text[1..], out red, out green, out blue);
        }

        var color = RgbColor.Match(text);

        return color.Success
            && TryParseChannel(color.Groups["red"].Value, out red)
            && TryParseChannel(color.Groups["green"].Value, out green)
            && TryParseChannel(color.Groups["blue"].Value, out blue);
    }

    /// <summary>Reads hex digits into channels of zero to one, skipping an alpha the limits do not judge.</summary>
    /// <param name="digits">The hex digits without the hash.</param>
    /// <param name="red">The red channel, or zero when the digits are no color.</param>
    /// <param name="green">The green channel, or zero when the digits are no color.</param>
    /// <param name="blue">The blue channel, or zero when the digits are no color.</param>
    /// <returns>True when the digits are three, four, six or eight hex digits; otherwise false.</returns>
    private static bool TryParseHex(string digits, out double red, out double green, out double blue)
    {
        red = 0;
        green = 0;
        blue = 0;

        string expanded = digits.Length switch
        {
            3 or 4 => new string([digits[0], digits[0], digits[1], digits[1], digits[2], digits[2]]),
            6 or 8 => digits[..6],
            _ => "",
        };

        if (expanded.Length != 6 || !expanded.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        red = Convert.ToInt32(expanded[..2], 16) / 255.0;
        green = Convert.ToInt32(expanded[2..4], 16) / 255.0;
        blue = Convert.ToInt32(expanded[4..6], 16) / 255.0;
        return true;
    }

    /// <summary>Reads one rgb() channel, an integer of zero to 255 or a percent, into zero to one.</summary>
    /// <param name="text">The trimmed channel text.</param>
    /// <param name="channel">The channel, or zero when the text is no channel.</param>
    /// <returns>True when the text is an integer of zero to 255 or a percent; otherwise false.</returns>
    private static bool TryParseChannel(string text, out double channel)
    {
        channel = 0;

        if (text.EndsWith('%')
            && double.TryParse(text[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent)
            && percent is >= 0 and <= 100)
        {
            channel = percent / 100.0;
            return true;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
            && level is >= 0 and <= 255)
        {
            channel = level / 255.0;
            return true;
        }

        return false;
    }

    /// <summary>Whether two roles resolved to colors.</summary>
    /// <param name="roles">The resolved roles by name.</param>
    /// <param name="first">The first role name.</param>
    /// <param name="second">The second role name.</param>
    /// <returns>True when both roles carry colors.</returns>
    private static bool Ready(Dictionary<string, ResolvedColor> roles, string first, string second) =>
        roles[first].Problem is null && roles[second].Problem is null;

    /// <summary>Checks text, boundary, ring and primary text against their contrast limits.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="roles">The resolved roles by name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContrastLimits(string tokensFile, string scheme, Dictionary<string, ResolvedColor> roles, List<Finding> findings)
    {
        CheckContrast(tokensFile, scheme, roles, "text", "page", 4.5, findings);
        CheckContrast(tokensFile, scheme, roles, "text", "layer-1", 4.5, findings);
        CheckContrast(tokensFile, scheme, roles, "muted-text", "page", 4.5, findings);
        CheckContrast(tokensFile, scheme, roles, "muted-text", "layer-1", 4.5, findings);
        CheckContrast(tokensFile, scheme, roles, "control-boundary", "layer-1", 3.0, findings);
        CheckContrast(tokensFile, scheme, roles, "focus-ring", "layer-1", 3.0, findings);
        CheckContrast(tokensFile, scheme, roles, "focus-ring", "page", 3.0, findings);
        CheckContrast(tokensFile, scheme, roles, "primary-text", "primary", 4.5, findings);
    }

    /// <summary>Reports a role below its contrast limit on a surface, when both resolved.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="roles">The resolved roles by name.</param>
    /// <param name="role">The role name.</param>
    /// <param name="surface">The surface role name.</param>
    /// <param name="limit">The lowest passing ratio.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContrast(string tokensFile, string scheme, Dictionary<string, ResolvedColor> roles, string role, string surface, double limit, List<Finding> findings)
    {
        if (!Ready(roles, role, surface))
        {
            return;
        }

        var ink = roles[role];
        var ground = roles[surface];
        var ratio = ContrastOf(
            LuminanceOf(ink.Red, ink.Green, ink.Blue),
            LuminanceOf(ground.Red, ground.Green, ground.Blue));

        if (ratio < limit - Tolerance)
        {
            var reached = ratio.ToString("0.0", CultureInfo.InvariantCulture);
            var lowest = limit.ToString("0.0", CultureInfo.InvariantCulture);

            findings.Add(new Finding(
                "design-token-limit-failed",
                $"Token role '{role}' reaches {reached}:1 on '{surface}' in scheme '{scheme}', below the {lowest}:1 limit.",
                tokensFile, ink.Line, null));
        }
    }

    /// <summary>Checks each layer against the surface it sits on, the layer edge against both, and the separator against the edge.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="roles">The resolved roles by name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSurfaces(string tokensFile, string scheme, Dictionary<string, ResolvedColor> roles, List<Finding> findings)
    {
        CheckSeparation(tokensFile, scheme, roles, "layer-1", "page", 1.06, findings);
        CheckSeparation(tokensFile, scheme, roles, "layer-2", "layer-1", 1.06, findings);
        CheckSeparation(tokensFile, scheme, roles, "layer-accent", "layer-1", 1.06, findings);
        CheckSeparation(tokensFile, scheme, roles, "layer-accent", "layer-2", 1.06, findings);
        CheckContrast(tokensFile, scheme, roles, "layer-edge", "page", 1.2, findings);

        if (Ready(roles, "layer-edge", "layer-1") && SameColor(roles["layer-edge"], roles["layer-1"]))
        {
            findings.Add(new Finding(
                "design-token-limit-failed",
                $"Token role 'layer-edge' equals 'layer-1' in scheme '{scheme}'; the edge differs from 'page' and 'layer-1'.",
                tokensFile, roles["layer-edge"].Line, null));
        }

        if (Ready(roles, "separator", "layer-1") && Ready(roles, "layer-edge", "layer-1"))
        {
            var separator = roles["separator"];
            var edge = roles["layer-edge"];
            var layer = roles["layer-1"];
            var soft = ContrastOf(
                LuminanceOf(separator.Red, separator.Green, separator.Blue),
                LuminanceOf(layer.Red, layer.Green, layer.Blue));
            var firm = ContrastOf(
                LuminanceOf(edge.Red, edge.Green, edge.Blue),
                LuminanceOf(layer.Red, layer.Green, layer.Blue));

            if (soft > firm + Tolerance)
            {
                var reached = soft.ToString("0.0", CultureInfo.InvariantCulture);
                var held = firm.ToString("0.0", CultureInfo.InvariantCulture);

                findings.Add(new Finding(
                    "design-token-limit-failed",
                    $"Token role 'separator' reaches {reached}:1 on 'layer-1' in scheme '{scheme}', above the layer edge's {held}:1.",
                    tokensFile, separator.Line, null));
            }
        }
    }

    /// <summary>Reports a layer below its separation limit on the surface it sits on, when both resolved.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="roles">The resolved roles by name.</param>
    /// <param name="role">The layer role name.</param>
    /// <param name="surface">The surface role name it sits on.</param>
    /// <param name="limit">The lowest passing ratio.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSeparation(string tokensFile, string scheme, Dictionary<string, ResolvedColor> roles, string role, string surface, double limit, List<Finding> findings)
    {
        if (!Ready(roles, role, surface))
        {
            return;
        }

        var layer = roles[role];
        var ground = roles[surface];
        var ratio = ContrastOf(
            LuminanceOf(layer.Red, layer.Green, layer.Blue),
            LuminanceOf(ground.Red, ground.Green, ground.Blue));

        if (ratio < limit - Tolerance)
        {
            var reached = ratio.ToString("0.00", CultureInfo.InvariantCulture);
            var lowest = limit.ToString("0.00", CultureInfo.InvariantCulture);

            findings.Add(new Finding(
                "design-token-limit-failed",
                $"Token role '{role}' reaches {reached}:1 on '{surface}' in scheme '{scheme}', below the {lowest}:1 limit.",
                tokensFile, layer.Line, null));
        }
    }

    /// <summary>Whether two resolved roles hold the same color.</summary>
    /// <param name="first">The first role.</param>
    /// <param name="second">The second role.</param>
    /// <returns>True when every channel matches.</returns>
    private static bool SameColor(ResolvedColor first, ResolvedColor second) =>
        first.Red == second.Red && first.Green == second.Green && first.Blue == second.Blue;

    /// <summary>Checks each tone's text against its fill.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="roles">The resolved roles by name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTones(string tokensFile, string scheme, Dictionary<string, ResolvedColor> roles, List<Finding> findings)
    {
        foreach (var tone in Tones)
        {
            CheckContrast(tokensFile, scheme, roles, $"tone.{tone}.text", $"tone.{tone}.fill", 4.5, findings);
        }
    }

    /// <summary>Checks each chart color against its surface and every pair under color-vision simulation.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="roles">The resolved roles by name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCharts(string tokensFile, string scheme, Dictionary<string, ResolvedColor> roles, List<Finding> findings)
    {
        string[] charts = ["chart.1", "chart.2", "chart.3"];

        foreach (var chart in charts)
        {
            CheckContrast(tokensFile, scheme, roles, chart, "layer-1", 3.0, findings);
        }

        (string First, string Second)[] pairs = [("chart.1", "chart.2"), ("chart.1", "chart.3"), ("chart.2", "chart.3")];
        (string Name, bool Protan)[] visions = [("protanopia", true), ("deuteranopia", false)];

        foreach (var (first, second) in pairs)
        {
            if (!Ready(roles, first, second))
            {
                continue;
            }

            foreach (var (name, protan) in visions)
            {
                var delta = SeparationOf(roles[first], roles[second], protan);

                if (delta < 10.0 - Tolerance)
                {
                    var apart = delta.ToString("0.0", CultureInfo.InvariantCulture);

                    findings.Add(new Finding(
                        "design-token-limit-failed",
                        $"Chart colors '{first}' and '{second}' read {apart} apart under {name} simulation in scheme '{scheme}', below the 10.0 limit.",
                        tokensFile, roles[first].Line, null));
                }
            }
        }
    }

    /// <summary>The CIE76 distance between two roles after Machado 2009 color-vision simulation at severity 1.0.</summary>
    /// <param name="first">The first role.</param>
    /// <param name="second">The second role.</param>
    /// <param name="protan">True to simulate protanopia, false for deuteranopia.</param>
    /// <returns>The delta E of the simulated colors.</returns>
    private static double SeparationOf(ResolvedColor first, ResolvedColor second, bool protan)
    {
        var before = Simulate(first.Red, first.Green, first.Blue, protan);
        var after = Simulate(second.Red, second.Green, second.Blue, protan);
        var labBefore = LabOf(before.Red, before.Green, before.Blue);
        var labAfter = LabOf(after.Red, after.Green, after.Blue);

        return Math.Sqrt(
            (labBefore.L - labAfter.L) * (labBefore.L - labAfter.L)
            + (labBefore.A - labAfter.A) * (labBefore.A - labAfter.A)
            + (labBefore.B - labAfter.B) * (labBefore.B - labAfter.B));
    }

    /// <summary>Simulates how a color reads under protanopia or deuteranopia, per Machado 2009 at severity 1.0.</summary>
    /// <param name="red">The red channel, zero to one.</param>
    /// <param name="green">The green channel, zero to one.</param>
    /// <param name="blue">The blue channel, zero to one.</param>
    /// <param name="protan">True to simulate protanopia, false for deuteranopia.</param>
    /// <returns>The simulated channels, zero to one.</returns>
    private static (double Red, double Green, double Blue) Simulate(double red, double green, double blue, bool protan) =>
        protan
            ? (0.567 * red + 0.433 * green, 0.558 * red + 0.442 * green, 0.242 * green + 0.758 * blue)
            : (0.625 * red + 0.375 * green, 0.7 * red + 0.3 * green, 0.3 * green + 0.7 * blue);

    /// <summary>Converts sRGB channels to CIELAB under D65.</summary>
    /// <param name="red">The red channel, zero to one.</param>
    /// <param name="green">The green channel, zero to one.</param>
    /// <param name="blue">The blue channel, zero to one.</param>
    /// <returns>The L, a and b coordinates.</returns>
    private static (double L, double A, double B) LabOf(double red, double green, double blue)
    {
        var linearRed = LinearOf(red);
        var linearGreen = LinearOf(green);
        var linearBlue = LinearOf(blue);
        var x = (0.4124564 * linearRed + 0.3575761 * linearGreen + 0.1804375 * linearBlue) / 0.95047;
        var y = 0.2126729 * linearRed + 0.7151522 * linearGreen + 0.0721750 * linearBlue;
        var z = (0.0193339 * linearRed + 0.1191920 * linearGreen + 0.9503041 * linearBlue) / 1.08883;
        var fx = PivotOf(x);
        var fy = PivotOf(y);
        var fz = PivotOf(z);

        return (116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz));
    }

    /// <summary>The CIELAB pivot of one normalized tristimulus value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The pivoted value.</returns>
    private static double PivotOf(double value)
    {
        const double Epsilon = 216.0 / 24389.0;
        const double Kappa = 24389.0 / 27.0;

        return value > Epsilon ? Math.Cbrt(value) : (Kappa * value + 16.0) / 116.0;
    }

    /// <summary>Checks the magnitude steps rise in lightness through the scheme.</summary>
    /// <param name="tokensFile">The absolute token file path.</param>
    /// <param name="root">The token file aliases resolve against.</param>
    /// <param name="scheme">The scheme name.</param>
    /// <param name="group">The scheme group.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckMagnitude(string tokensFile, JsonText root, string scheme, JsonText group, List<Finding> findings)
    {
        if (!group.TryGetProperty("magnitude", out var magnitude))
        {
            findings.Add(new Finding(
                "design-token-limit-failed",
                $"Token role 'magnitude' has no color in scheme '{scheme}': it names no 'magnitude' token.",
                tokensFile, group.Line, null));
            return;
        }

        var steps = new List<ResolvedColor>();
        var names = new List<string>();
        var index = 0;

        foreach (var step in MagnitudeStepsOf(magnitude))
        {
            index++;
            steps.Add(ResolveValue(root, step.Node, step.Line, new HashSet<string>(StringComparer.Ordinal)));
            names.Add($"magnitude step {index}");
        }

        if (steps.Count == 0)
        {
            findings.Add(new Finding(
                "design-token-limit-failed",
                $"Token role 'magnitude' holds no steps in scheme '{scheme}'.",
                tokensFile, magnitude.Line, null));
            return;
        }

        for (var position = 0; position < steps.Count; position++)
        {
            if (steps[position].Problem is { } problem)
            {
                findings.Add(new Finding(
                    "design-token-limit-failed",
                    $"Token role '{names[position]}' has no color in scheme '{scheme}': {problem}.",
                    tokensFile, steps[position].Line, null));
            }
        }

        if (steps.Any(step => step.Problem is not null))
        {
            return;
        }

        for (var position = 1; position < steps.Count; position++)
        {
            var before = steps[position - 1];
            var after = steps[position];

            if (LuminanceOf(after.Red, after.Green, after.Blue) <= LuminanceOf(before.Red, before.Green, before.Blue))
            {
                findings.Add(new Finding(
                    "design-token-limit-failed",
                    $"Token role 'magnitude' falls from step {position} to step {position + 1} in scheme '{scheme}'; the steps rise in lightness.",
                    tokensFile, after.Line, null));
                return;
            }
        }
    }

    /// <summary>The magnitude steps of a scheme: an array in order, or a group in file order without its metadata.</summary>
    /// <param name="magnitude">The magnitude node.</param>
    /// <returns>Each step node with its line.</returns>
    private static List<(JsonText Node, int Line)> MagnitudeStepsOf(JsonText magnitude)
    {
        if (magnitude.Kind == JsonKind.Array)
        {
            return magnitude.Items.Select(item => (item, item.Line)).ToList();
        }

        if (magnitude.Kind != JsonKind.Object)
        {
            return [];
        }

        return magnitude.Properties
            .Where(entry => !entry.Key.StartsWith('$'))
            .Select(entry => (entry.Value, entry.Value.Line))
            .ToList();
    }

    /// <summary>The relative luminance of a color, per WCAG 2.</summary>
    /// <param name="red">The red channel, zero to one.</param>
    /// <param name="green">The green channel, zero to one.</param>
    /// <param name="blue">The blue channel, zero to one.</param>
    /// <returns>The relative luminance, zero to one.</returns>
    private static double LuminanceOf(double red, double green, double blue) =>
        0.2126 * LinearOf(red) + 0.7152 * LinearOf(green) + 0.0722 * LinearOf(blue);

    /// <summary>Linearizes one sRGB channel, per WCAG 2.</summary>
    /// <param name="channel">The channel, zero to one.</param>
    /// <returns>The linearized channel.</returns>
    private static double LinearOf(double channel) =>
        channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

    /// <summary>The contrast ratio of two colors, per WCAG 2.</summary>
    /// <param name="first">The luminance of the first color.</param>
    /// <param name="second">The luminance of the second color.</param>
    /// <returns>The ratio, one to twenty-one.</returns>
    private static double ContrastOf(double first, double second) =>
        (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
}
