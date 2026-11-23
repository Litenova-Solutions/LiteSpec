using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright;

namespace LiteSpec.Cli.Audit;

/// <summary>The browser run: one page per route, width and scheme, with the audit script read back.</summary>
/// <follows>litespec/frontend/ui#pass-every-route-suite-check</follows>
static class AuditBrowser
{
    /// <summary>The viewport height for every audited width.</summary>
    const int ViewportHeight = 900;

    /// <summary>Runs the audit script over every route, width and scheme, screenshotting each page.</summary>
    /// <param name="options">The parsed audit options.</param>
    /// <param name="routes">The routes to visit.</param>
    /// <param name="script">The audit script text.</param>
    /// <param name="outDir">The folder receiving the screenshots.</param>
    /// <returns>The run, or null when no browser is available.</returns>
    internal static async Task<Run?> RunAsync(
        AuditOptions options, IReadOnlyList<AuditRoute> routes, string script, string outDir)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchAsync(playwright);

        if (browser is null)
        {
            return null;
        }

        var raw = new List<AuditRawFinding>();
        var shots = new List<string>();

        foreach (var route in routes)
        {
            foreach (var width in options.Widths)
            {
                foreach (var scheme in options.Schemes)
                {
                    raw.AddRange(await AuditPageAsync(browser, options, route, width, scheme, script, outDir));
                    shots.Add(AuditReport.FileName(route.Path, scheme, width));
                }
            }
        }

        return new Run(raw, shots);
    }

    /// <summary>Launches the installed Chrome, falling back to the bundled Chromium.</summary>
    /// <param name="playwright">The Playwright driver.</param>
    /// <returns>The browser, or null when neither browser is available.</returns>
    static async Task<IBrowser?> LaunchAsync(IPlaywright playwright)
    {
        try
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Channel = "chrome", Headless = true });
        }
        catch (PlaywrightException)
        {
            // The installed Chrome is absent; the bundled Chromium serves instead.
        }

        try
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (PlaywrightException)
        {
            return null;
        }
    }

    /// <summary>Audits one page: loads it, reads the script findings back, and screenshots it.</summary>
    /// <param name="browser">The launched browser.</param>
    /// <param name="options">The parsed audit options.</param>
    /// <param name="route">The route to visit.</param>
    /// <param name="width">The viewport width.</param>
    /// <param name="scheme">The color scheme.</param>
    /// <param name="script">The audit script text.</param>
    /// <param name="outDir">The folder receiving the screenshot.</param>
    /// <returns>The script findings at this width.</returns>
    static async Task<IReadOnlyList<AuditRawFinding>> AuditPageAsync(
        IBrowser browser, AuditOptions options, AuditRoute route, int width, string scheme, string script, string outDir)
    {
        var url = options.BaseUrl + (route.Path.StartsWith('/') ? route.Path : "/" + route.Path);
        var contextOptions = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = ViewportHeight },
            ColorScheme = scheme == "dark" ? ColorScheme.Dark : ColorScheme.Light,
        };

        if (options.StorageState is not null)
        {
            contextOptions.StorageStatePath = options.StorageState;
        }

        try
        {
            await using var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var json = await page.EvaluateAsync<string>(script, options.PrimarySelector);
            var file = AuditReport.FileName(route.Path, scheme, width);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(outDir, file), FullPage = true });
            return ReadScriptFindings(json, route.Path, scheme, width);
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
        {
            throw new InvalidOperationException($"Cannot audit '{url}': {FirstLine(exception.Message)}", exception);
        }
    }

    /// <summary>Reads the script's JSON array back into raw findings at one width.</summary>
    /// <param name="json">The script's JSON text.</param>
    /// <param name="route">The route path the findings were read on.</param>
    /// <param name="scheme">The color scheme the findings were read in.</param>
    /// <param name="width">The viewport width the findings were read at.</param>
    /// <returns>The raw findings the script reported.</returns>
    static List<AuditRawFinding> ReadScriptFindings(string json, string route, string scheme, int width)
    {
        JsonNode? root;

        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("The audit script returned no findings array.");
        }

        if (root is not JsonArray entries)
        {
            throw new InvalidOperationException("The audit script returned no findings array.");
        }

        var findings = new List<AuditRawFinding>();

        foreach (var entry in entries)
        {
            if (entry is not JsonObject finding
                || finding["code"] is not JsonValue codeValue || !codeValue.TryGetValue<string>(out var code)
                || finding["selector"] is not JsonValue selectorValue || !selectorValue.TryGetValue<string>(out var selector)
                || finding["message"] is not JsonValue messageValue || !messageValue.TryGetValue<string>(out var message))
            {
                continue;
            }

            findings.Add(new AuditRawFinding(code, selector, message, route, scheme, width));
        }

        return findings;
    }

    /// <summary>The first non-empty line of a browser error, without the driver's own detail lines.</summary>
    /// <param name="message">The browser error message.</param>
    /// <returns>The first non-empty line.</returns>
    static string FirstLine(string message)
    {
        foreach (var line in message.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.Length > 0)
            {
                return trimmed;
            }
        }

        return message.Trim();
    }

    /// <summary>One browser run: the raw findings and the screenshot file names.</summary>
    /// <param name="Raw">Every script finding, each at its own width.</param>
    /// <param name="Screenshots">The screenshot file names inside the output folder, in run order.</param>
    internal sealed record Run(IReadOnlyList<AuditRawFinding> Raw, IReadOnlyList<string> Screenshots);
}
