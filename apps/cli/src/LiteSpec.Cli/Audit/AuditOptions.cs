using System.Globalization;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Audit;

/// <summary>The parsed options of <c>litespec ui audit</c>.</summary>
/// <param name="BaseUrl">The running application the audit visits, without a trailing slash.</param>
/// <param name="RoutesFile">The routes file, or null to collect the routes from the screen records.</param>
/// <param name="Widths">The viewport widths to audit at, in pixels.</param>
/// <param name="Schemes">The color schemes to audit in: light, dark, or both.</param>
/// <param name="StorageState">The Playwright storage state for signed-in pages, or null.</param>
/// <param name="Out">The folder receiving the screenshots and the JSON report.</param>
/// <param name="FailOn">When the audit exits 1: blocking for any finding, none to always pass.</param>
/// <param name="PrimarySelector">The CSS selector marking the primary action.</param>
/// <param name="Browser">The browser executable, or null for the installed Chrome, Edge, or Chromium.</param>
/// <param name="Root">The --root value, or null for the working directory.</param>
sealed record AuditOptions(
    string BaseUrl,
    string? RoutesFile,
    IReadOnlyList<int> Widths,
    IReadOnlyList<string> Schemes,
    string? StorageState,
    string Out,
    string FailOn,
    string PrimarySelector,
    string? Browser,
    string? Root)
{
    /// <summary>The viewport widths the audit uses when --widths is absent: phone, laptop and wide.</summary>
    internal const string DefaultWidths = "390,1440,2560";

    /// <summary>The color schemes the audit uses when --schemes is absent.</summary>
    internal const string DefaultSchemes = "light,dark";

    /// <summary>How the audit exits when --fail-on is absent: 1 on any finding.</summary>
    internal const string DefaultFailOn = "blocking";

    /// <summary>The CSS selector the audit reads the primary action with when --primary-selector is absent.</summary>
    internal const string DefaultPrimarySelector = "[data-variant=primary]";

    /// <summary>The folder the audit writes into when --out is absent, under the working directory.</summary>
    internal const string DefaultOut = "ui-audit";

    /// <summary>Parses the audit options, in usage order.</summary>
    /// <param name="parsed">The parsed command arguments.</param>
    /// <returns>The options, or the usage error when an option is missing or invalid.</returns>
    internal static (AuditOptions? Options, string? Error) Parse(CliArgs parsed)
    {
        var baseUrl = parsed.Value("--base-url");

        if (string.IsNullOrEmpty(baseUrl))
        {
            return (null, "Option --base-url needs a value.");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return (null, $"Invalid base URL '{baseUrl}': use an absolute http or https URL.");
        }

        var (widths, widthsError) = ParseWidths(parsed.Value("--widths"));

        if (widths is null)
        {
            return (null, widthsError);
        }

        var (schemes, schemesError) = ParseSchemes(parsed.Value("--schemes"));

        if (schemes is null)
        {
            return (null, schemesError);
        }

        var routesFile = parsed.Value("--routes");

        if (routesFile is not null && routesFile.Length == 0)
        {
            return (null, "Option --routes needs a value.");
        }

        var storageState = parsed.Value("--storage-state");

        if (storageState is not null && storageState.Length == 0)
        {
            return (null, "Option --storage-state needs a value.");
        }

        var failOn = parsed.Value("--fail-on") ?? DefaultFailOn;

        if (failOn is not ("blocking" or "none"))
        {
            return (null, $"Unknown fail-on '{failOn}'. Use blocking or none.");
        }

        var primarySelector = parsed.Value("--primary-selector") ?? DefaultPrimarySelector;

        if (primarySelector.Length == 0)
        {
            return (null, "Option --primary-selector needs a value.");
        }

        var browser = parsed.Value("--browser");

        if (browser is not null && browser.Length == 0)
        {
            return (null, "Option --browser needs a value.");
        }

        return (new AuditOptions(
            baseUrl.TrimEnd('/'),
            routesFile,
            widths,
            schemes,
            storageState,
            parsed.Value("--out") ?? DefaultOut,
            failOn,
            primarySelector,
            browser,
            parsed.Value("--root")), null);
    }

    /// <summary>Parses the --widths comma list into viewport widths.</summary>
    /// <param name="raw">The option value, or null when it was not passed.</param>
    /// <returns>The widths in order, or the usage error when one is not a positive number.</returns>
    static (IReadOnlyList<int>? Widths, string? Error) ParseWidths(string? raw)
    {
        var text = raw ?? DefaultWidths;

        if (text.Length == 0)
        {
            return (null, "Option --widths needs a value.");
        }

        var widths = new List<int>();

        foreach (var part in text.Split(',', StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var width) || width <= 0)
            {
                return (null, $"Invalid width '{part}': use a positive number.");
            }

            widths.Add(width);
        }

        return (widths, null);
    }

    /// <summary>Parses the --schemes comma list into color schemes.</summary>
    /// <param name="raw">The option value, or null when it was not passed.</param>
    /// <returns>The schemes in order, or the usage error when one is not light or dark.</returns>
    static (IReadOnlyList<string>? Schemes, string? Error) ParseSchemes(string? raw)
    {
        var text = raw ?? DefaultSchemes;

        if (text.Length == 0)
        {
            return (null, "Option --schemes needs a value.");
        }

        var schemes = new List<string>();

        foreach (var part in text.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part is not ("light" or "dark"))
            {
                return (null, $"Unknown scheme '{part}'. Use light or dark.");
            }

            schemes.Add(part);
        }

        return (schemes, null);
    }
}
