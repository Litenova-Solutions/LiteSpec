using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Audit;

/// <summary>The browser run: one page per route, width and scheme, with the audit script read back.</summary>
/// <follows>litespec/frontend/ui#pass-every-route-suite-check</follows>
static class AuditBrowser
{
    /// <summary>The viewport height for every audited width.</summary>
    const int ViewportHeight = 900;

    /// <summary>How long one protocol command may take to answer: fifteen seconds.</summary>
    static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);

    /// <summary>How long a page may take to load and settle: fifteen seconds.</summary>
    static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(15);

    /// <summary>The quiet network wait after the load event: half a second without a request.</summary>
    static readonly TimeSpan QuietWait = TimeSpan.FromMilliseconds(500);

    /// <summary>Runs the audit script over every route, width and scheme, screenshotting each page.</summary>
    /// <param name="options">The parsed audit options.</param>
    /// <param name="routes">The routes to visit.</param>
    /// <param name="script">The audit script text.</param>
    /// <param name="outDir">The folder receiving the screenshots.</param>
    /// <returns>The run, or null when no browser is available.</returns>
    internal static async Task<Run?> RunAsync(
        AuditOptions options, IReadOnlyList<AuditRoute> routes, string script, string outDir)
    {
        var browserPath = AuditBrowserDiscovery.Find(options.Browser);

        if (browserPath is null)
        {
            return null;
        }

        AuditStorage.State? storage = null;

        if (options.StorageState is not null)
        {
            storage = AuditStorage.Read(options.StorageState);
        }

        using var browser = await AuditBrowserProcess.StartAsync(browserPath, CancellationToken.None);
        using var cdp = await AuditCdp.ConnectAsync(browser.WebSocketUrl, CancellationToken.None);

        var raw = new List<AuditRawFinding>();
        var shots = new List<string>();
        var seeded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var route in routes)
        {
            foreach (var width in options.Widths)
            {
                foreach (var scheme in options.Schemes)
                {
                    raw.AddRange(await AuditPageAsync(cdp, options, storage, seeded, route, width, scheme, script, outDir));
                    shots.Add(AuditReport.FileName(route.Path, scheme, width));
                }
            }
        }

        return new Run(raw, shots);
    }

    /// <summary>Audits one page: loads it, reads the script findings back, and screenshots it.</summary>
    /// <param name="cdp">The connected protocol client.</param>
    /// <param name="options">The parsed audit options.</param>
    /// <param name="storage">The storage state, or null for signed-out pages.</param>
    /// <param name="seeded">The origins whose local storage is already seeded.</param>
    /// <param name="route">The route to visit.</param>
    /// <param name="width">The viewport width.</param>
    /// <param name="scheme">The color scheme.</param>
    /// <param name="script">The audit script text.</param>
    /// <param name="outDir">The folder receiving the screenshot.</param>
    /// <returns>The script findings at this width.</returns>
    static async Task<IReadOnlyList<AuditRawFinding>> AuditPageAsync(
        AuditCdp cdp,
        AuditOptions options,
        AuditStorage.State? storage,
        HashSet<string> seeded,
        AuditRoute route,
        int width,
        string scheme,
        string script,
        string outDir)
    {
        var url = options.BaseUrl + (route.Path.StartsWith('/') ? route.Path : "/" + route.Path);
        string? targetId = null;

        try
        {
            var created = await cdp.SendAsync("Target.createTarget", new JsonObject { ["url"] = "about:blank" }, null, CommandTimeout, CancellationToken.None);
            targetId = created["targetId"] is JsonValue createdId && createdId.TryGetValue<string>(out var createdText) ? createdText : null;

            if (targetId is null)
            {
                throw new InvalidOperationException("The browser opened no target.");
            }

            var attached = await cdp.SendAsync(
                "Target.attachToTarget", new JsonObject { ["targetId"] = targetId, ["flatten"] = true }, null, CommandTimeout, CancellationToken.None);
            var sessionId = attached["sessionId"] is JsonValue attachedId && attachedId.TryGetValue<string>(out var attachedText) ? attachedText : null;

            if (sessionId is null)
            {
                throw new InvalidOperationException("The browser attached to no target.");
            }

            await cdp.SendAsync("Page.enable", null, sessionId, CommandTimeout, CancellationToken.None);
            await cdp.SendAsync("Network.enable", null, sessionId, CommandTimeout, CancellationToken.None);
            await cdp.SendAsync(
                "Emulation.setDeviceMetricsOverride",
                new JsonObject { ["width"] = width, ["height"] = ViewportHeight, ["deviceScaleFactor"] = 1, ["mobile"] = width < 600 },
                sessionId,
                CommandTimeout,
                CancellationToken.None);
            await cdp.SendAsync(
                "Emulation.setEmulatedMedia",
                new JsonObject
                {
                    ["media"] = "screen",
                    ["features"] = new JsonArray((JsonNode)new JsonObject { ["name"] = "prefers-color-scheme", ["value"] = scheme }),
                },
                sessionId,
                CommandTimeout,
                CancellationToken.None);

            if (storage is not null && storage.Cookies.Count > 0)
            {
                await SetCookiesAsync(cdp, sessionId, storage.Cookies);
            }

            await NavigateAsync(cdp, sessionId, url);
            await WaitForSettledAsync(cdp, sessionId);

            if (storage is not null)
            {
                await SeedLocalStorageAsync(cdp, sessionId, storage, seeded, url);
            }

            var selectorJson = JsonValue.Create(options.PrimarySelector)!.ToJsonString();
            var evaluated = await cdp.SendAsync(
                "Runtime.evaluate",
                new JsonObject { ["expression"] = $"({script})({selectorJson})", ["returnByValue"] = true },
                sessionId,
                CommandTimeout,
                CancellationToken.None);

            if (evaluated["exceptionDetails"] is JsonObject details)
            {
                throw new InvalidOperationException(ExceptionText(details));
            }

            var json = evaluated["result"] is JsonObject result
                && result["value"] is JsonValue value
                && value.TryGetValue<string>(out var text) ? text : null;

            if (json is null)
            {
                throw new InvalidOperationException("The audit script returned no findings array.");
            }

            var findings = ReadScriptFindings(json, route.Path, scheme, width);
            var file = AuditReport.FileName(route.Path, scheme, width);
            await CaptureScreenshotAsync(cdp, sessionId, Path.Combine(outDir, file));

            await CloseTargetAsync(cdp, targetId);
            targetId = null;

            return findings;
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException or TaskCanceledException or OperationCanceledException or HttpRequestException or WebSocketException or IOException or UnauthorizedAccessException)
        {
            if (targetId is not null)
            {
                await CloseTargetAsync(cdp, targetId);
            }

            if (exception.Message.StartsWith("Cannot audit '", StringComparison.Ordinal))
            {
                throw;
            }

            throw new InvalidOperationException($"Cannot audit '{url}': {FirstLine(exception.Message)}", exception);
        }
    }

    /// <summary>Sets the storage state cookies on one target.</summary>
    /// <param name="cdp">The connected protocol client.</param>
    /// <param name="sessionId">The target session.</param>
    /// <param name="cookies">The cookies to set.</param>
    /// <returns>A task that completes when the cookies are set.</returns>
    static async Task SetCookiesAsync(AuditCdp cdp, string sessionId, IReadOnlyList<AuditStorage.Cookie> cookies)
    {
        var entries = new JsonArray();

        foreach (var cookie in cookies)
        {
            var node = new JsonObject
            {
                ["name"] = cookie.Name,
                ["value"] = cookie.Value,
            };

            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                node["domain"] = cookie.Domain;
            }

            if (!string.IsNullOrEmpty(cookie.Path))
            {
                node["path"] = cookie.Path;
            }

            if (cookie.Secure)
            {
                node["secure"] = true;
            }

            if (cookie.HttpOnly)
            {
                node["httpOnly"] = true;
            }

            if (cookie.SameSite is "Strict" or "Lax" or "None")
            {
                node["sameSite"] = cookie.SameSite;
            }

            if (cookie.Expires is { } expires && expires > 0)
            {
                node["expires"] = expires;
            }

            entries.Add((JsonNode)node);
        }

        try
        {
            await cdp.SendAsync("Network.setCookies", new JsonObject { ["cookies"] = entries }, sessionId, CommandTimeout, CancellationToken.None);
        }
        catch (InvalidOperationException exception) when (IsUnknownMethod(exception))
        {
            // An older browser names the singular command only; each cookie is set alone.
            foreach (var entry in entries.OfType<JsonObject>())
            {
                await cdp.SendAsync("Network.setCookie", (JsonObject)entry.DeepClone(), sessionId, CommandTimeout, CancellationToken.None);
            }
        }
    }

    /// <summary>Seeds one origin's local storage after its first navigation, then reloads the page.</summary>
    /// <param name="cdp">The connected protocol client.</param>
    /// <param name="sessionId">The target session.</param>
    /// <param name="storage">The storage state.</param>
    /// <param name="seeded">The origins already seeded.</param>
    /// <param name="url">The navigated address.</param>
    /// <returns>A task that completes when the origin is seeded.</returns>
    static async Task SeedLocalStorageAsync(AuditCdp cdp, string sessionId, AuditStorage.State storage, HashSet<string> seeded, string url)
    {
        if (storage.Origins.Count == 0)
        {
            return;
        }

        var origin = OriginOf(url);

        if (origin is null || !seeded.Add(origin))
        {
            return;
        }

        var match = storage.Origins.FirstOrDefault(candidate => string.Equals(candidate.Url, origin, StringComparison.OrdinalIgnoreCase));

        if (match is null || match.Entries.Count == 0)
        {
            return;
        }

        var setter = new StringBuilder("(() => {");

        foreach (var entry in match.Entries)
        {
            setter.Append("localStorage.setItem(");
            setter.Append(JsonValue.Create(entry.Name)!.ToJsonString());
            setter.Append(',');
            setter.Append(JsonValue.Create(entry.Value)!.ToJsonString());
            setter.Append(");");
        }

        setter.Append("})()");
        await cdp.SendAsync("Runtime.evaluate", new JsonObject { ["expression"] = setter.ToString(), ["returnByValue"] = true }, sessionId, CommandTimeout, CancellationToken.None);
        await NavigateAsync(cdp, sessionId, url);
        await WaitForSettledAsync(cdp, sessionId);
    }

    /// <summary>Navigates one target to its address.</summary>
    /// <param name="cdp">The connected protocol client.</param>
    /// <param name="sessionId">The target session.</param>
    /// <param name="url">The address to visit.</param>
    /// <returns>A task that completes when the navigation starts.</returns>
    static async Task NavigateAsync(AuditCdp cdp, string sessionId, string url)
    {
        var navigated = await cdp.SendAsync("Page.navigate", new JsonObject { ["url"] = url }, sessionId, CommandTimeout, CancellationToken.None);

        if (navigated["errorText"] is JsonValue error && error.TryGetValue<string>(out var text) && text.Length > 0)
        {
            throw new InvalidOperationException(text);
        }
    }

    /// <summary>Waits for the load event plus a quiet network, capped at the settle timeout.</summary>
    /// <param name="cdp">The connected protocol client.</param>
    /// <param name="sessionId">The target session.</param>
    /// <returns>A task that completes when the page settles or the timeout passes.</returns>
    static async Task WaitForSettledAsync(AuditCdp cdp, string sessionId)
    {
        var state = new SettleState();
        var start = DateTimeOffset.UtcNow;

        lock (state.Gate)
        {
            state.LastRequest = start;
        }

        void Handler(AuditCdp.CdpEvent evt)
        {
            if (!string.Equals(evt.SessionId, sessionId, StringComparison.Ordinal))
            {
                return;
            }

            lock (state.Gate)
            {
                if (evt.Method == "Page.loadEventFired")
                {
                    state.Loaded = true;
                }
                else if (evt.Method == "Network.requestWillBeSent")
                {
                    state.LastRequest = DateTimeOffset.UtcNow;
                }
            }
        }

        Action<AuditCdp.CdpEvent> handler = Handler;
        cdp.Received += handler;

        try
        {
            var deadline = start + SettleTimeout;

            while (DateTimeOffset.UtcNow < deadline)
            {
                bool loaded;
                DateTimeOffset last;

                lock (state.Gate)
                {
                    loaded = state.Loaded;
                    last = state.LastRequest;
                }

                if (loaded && DateTimeOffset.UtcNow - last >= QuietWait)
                {
                    return;
                }

                await Task.Delay(50);
            }
        }
        finally
        {
            cdp.Received -= handler;
        }
    }

    /// <summary>Captures the full page into its screenshot file.</summary>
    /// <param name="cdp">The connected protocol client.</param>
    /// <param name="sessionId">The target session.</param>
    /// <param name="filePath">The screenshot file path.</param>
    /// <returns>A task that completes when the screenshot is written.</returns>
    static async Task CaptureScreenshotAsync(AuditCdp cdp, string sessionId, string filePath)
    {
        var contentWidth = 0.0;
        var contentHeight = 0.0;

        try
        {
            var metrics = await cdp.SendAsync("Page.getLayoutMetrics", null, sessionId, CommandTimeout, CancellationToken.None);

            if (metrics["contentSize"] is JsonObject size
                && size["width"] is JsonValue widthValue && widthValue.TryGetValue<double>(out var width)
                && size["height"] is JsonValue heightValue && heightValue.TryGetValue<double>(out var height))
            {
                contentWidth = width;
                contentHeight = height;
            }
        }
        catch (InvalidOperationException)
        {
            // An unreadable layout still screenshots; the capture below reads the viewport.
        }

        JsonObject parameters;

        if (contentWidth > 0 && contentHeight > 0)
        {
            parameters = new JsonObject
            {
                ["format"] = "png",
                ["captureBeyondViewport"] = true,
                ["clip"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["width"] = contentWidth, ["height"] = contentHeight, ["scale"] = 1 },
            };
        }
        else
        {
            parameters = new JsonObject { ["format"] = "png", ["captureBeyondViewport"] = true };
        }

        var shot = await cdp.SendAsync("Page.captureScreenshot", parameters, sessionId, CommandTimeout, CancellationToken.None);
        var data = shot["data"] is JsonValue encoded && encoded.TryGetValue<string>(out var base64) ? base64 : null;

        if (string.IsNullOrEmpty(data))
        {
            throw new InvalidOperationException("The browser returned no screenshot.");
        }

        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(data);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("The browser returned no screenshot.", exception);
        }

        try
        {
            await File.WriteAllBytesAsync(filePath, bytes, CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new InvalidOperationException($"Cannot write '{filePath}': {exception.Message}", exception);
        }
    }

    /// <summary>Closes one target, ignoring a target that is already gone.</summary>
    /// <param name="cdp">The connected protocol client.</param>
    /// <param name="targetId">The target id.</param>
    /// <returns>A task that completes when the target closes.</returns>
    static async Task CloseTargetAsync(AuditCdp cdp, string targetId)
    {
        try
        {
            await cdp.SendAsync("Target.closeTarget", new JsonObject { ["targetId"] = targetId }, null, TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException or TaskCanceledException or OperationCanceledException or WebSocketException)
        {
            // The target is already gone; the audit keeps its findings.
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

    /// <summary>The origin of an address: its scheme, host, and port.</summary>
    /// <param name="url">The address.</param>
    /// <returns>The origin, or null when the address is not absolute.</returns>
    static string? OriginOf(string url)
    {
        try
        {
            return new Uri(url, UriKind.Absolute).GetLeftPart(UriPartial.Authority);
        }
        catch (Exception exception) when (exception is UriFormatException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Whether a protocol error names an unknown method.</summary>
    /// <param name="exception">The protocol error.</param>
    /// <returns>True when the method is unknown to the browser.</returns>
    static bool IsUnknownMethod(InvalidOperationException exception) =>
        exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("No such", StringComparison.Ordinal)
        || exception.Message.Contains("wasn't found", StringComparison.OrdinalIgnoreCase);

    /// <summary>The text of a script exception: its description, or its short text.</summary>
    /// <param name="details">The exception details.</param>
    /// <returns>The exception text.</returns>
    static string ExceptionText(JsonObject details)
    {
        if (details["exception"] is JsonObject thrown
            && thrown["description"] is JsonValue description
            && description.TryGetValue<string>(out var narrative)
            && narrative.Length > 0)
        {
            return narrative;
        }

        return details["text"] is JsonValue shortText && shortText.TryGetValue<string>(out var summary) && summary.Length > 0
            ? summary
            : "the audit script failed";
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

    /// <summary>The settle wait of one page: whether it loaded and when it last requested.</summary>
    sealed class SettleState
    {
        /// <summary>Gets the gate guarding the settle fields.</summary>
        public object Gate { get; } = new();

        /// <summary>Gets or sets a value indicating whether the load event fired.</summary>
        public bool Loaded { get; set; }

        /// <summary>Gets or sets when the page last sent a network request.</summary>
        public DateTimeOffset LastRequest { get; set; }
    }

    /// <summary>One browser run: the raw findings and the screenshot file names.</summary>
    /// <param name="Raw">Every script finding, each at its own width.</param>
    /// <param name="Screenshots">The screenshot file names inside the output folder, in run order.</param>
    internal sealed record Run(IReadOnlyList<AuditRawFinding> Raw, IReadOnlyList<string> Screenshots);
}
