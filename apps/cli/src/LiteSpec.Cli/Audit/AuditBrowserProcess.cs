using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Audit;

/// <summary>The launched audit browser: its process, its temporary profile, and its debugging address.</summary>
/// <follows>litespec/frontend/ui#pass-every-route-suite-check</follows>
sealed class AuditBrowserProcess : IDisposable
{
    /// <summary>How long the browser may take to open its debugging port: fifteen seconds.</summary>
    static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(15);

    /// <summary>How long the version address may take to answer: five seconds.</summary>
    static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>The browser process.</summary>
    readonly Process process;

    /// <summary>The temporary profile folder, removed on disposal.</summary>
    readonly string tempDir;

    /// <summary>Whether the browser was disposed.</summary>
    bool disposed;

    /// <summary>Initializes a new instance of the <see cref="AuditBrowserProcess"/> class.</summary>
    /// <param name="process">The browser process.</param>
    /// <param name="tempDir">The temporary profile folder.</param>
    /// <param name="webSocketUrl">The browser debugging WebSocket address.</param>
    AuditBrowserProcess(Process process, string tempDir, string webSocketUrl)
    {
        this.process = process;
        this.tempDir = tempDir;
        WebSocketUrl = webSocketUrl;
    }

    /// <summary>Gets the browser debugging WebSocket address.</summary>
    public string WebSocketUrl { get; }

    /// <summary>Stops the browser and removes its temporary profile.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or Win32Exception or NotSupportedException)
        {
            // The browser is already gone; its profile is still removed below.
        }

        process.Dispose();

        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // A profile the system will not remove still audits; the temporary folder keeps it.
        }
    }

    /// <summary>Launches the browser headless with a debugging port and reads its debugging address.</summary>
    /// <param name="browserPath">The browser executable path.</param>
    /// <param name="token">The token that stops the launch.</param>
    /// <returns>The running browser.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The process transfers to the returned browser, which disposes it; the failure path disposes it before throwing.")]
    internal static async Task<AuditBrowserProcess> StartAsync(string browserPath, CancellationToken token)
    {
        string tempDir;

        try
        {
            tempDir = Directory.CreateTempSubdirectory("litespec-audit-").FullName;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Cannot write a browser profile: {exception.Message}", exception);
        }

        var start = new ProcessStartInfo(browserPath)
        {
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        start.ArgumentList.Add("--headless=new");
        start.ArgumentList.Add("--remote-debugging-port=0");
        start.ArgumentList.Add($"--user-data-dir={tempDir}");
        start.ArgumentList.Add("--no-first-run");
        start.ArgumentList.Add("--no-default-browser-check");

        Process? process;

        try
        {
            process = Process.Start(start);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            RemoveProfile(tempDir);
            throw new InvalidOperationException($"Cannot start the browser '{browserPath}': {exception.Message}", exception);
        }

        if (process is null)
        {
            RemoveProfile(tempDir);
            throw new InvalidOperationException($"Cannot start the browser '{browserPath}'.");
        }

        _ = DrainAsync(process.StandardOutput);
        _ = DrainAsync(process.StandardError);

        var launched = false;

        try
        {
            var port = await WaitForPortAsync(tempDir, process, token);
            var webSocketUrl = await FetchWebSocketUrlAsync(port, token);
            launched = true;
            return new AuditBrowserProcess(process, tempDir, webSocketUrl);
        }
        finally
        {
            if (!launched)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception kill) when (kill is InvalidOperationException or IOException or UnauthorizedAccessException or Win32Exception or NotSupportedException)
                {
                    // The launch already failed; the kill is only cleanup.
                }

                process.Dispose();
                RemoveProfile(tempDir);
            }
        }
    }

    /// <summary>Reads the debugging port the browser writes into its profile folder.</summary>
    /// <param name="tempDir">The temporary profile folder.</param>
    /// <param name="process">The browser process.</param>
    /// <param name="token">The token that stops the wait.</param>
    /// <returns>The debugging port.</returns>
    static async Task<int> WaitForPortAsync(string tempDir, Process process, CancellationToken token)
    {
        var file = Path.Combine(tempDir, "DevToolsActivePort");
        var deadline = DateTimeOffset.UtcNow + StartTimeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            token.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                throw new InvalidOperationException("The browser exited before opening its debugging port.");
            }

            if (File.Exists(file))
            {
                string text;

                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(50, token);
                    continue;
                }

                var first = text.Split('\n')[0].Trim();

                if (int.TryParse(first, out var port) && port > 0)
                {
                    return port;
                }
            }

            await Task.Delay(50, token);
        }

        throw new InvalidOperationException("The browser did not open its debugging port in time.");
    }

    /// <summary>Fetches the browser debugging WebSocket address from its version address.</summary>
    /// <param name="port">The debugging port.</param>
    /// <param name="token">The token that stops the fetch.</param>
    /// <returns>The WebSocket address.</returns>
    static async Task<string> FetchWebSocketUrlAsync(int port, CancellationToken token)
    {
        using var http = new HttpClient { Timeout = VersionTimeout };
        string text;

        try
        {
            text = await http.GetStringAsync($"http://127.0.0.1:{port}/json/version", token);
        }
        catch (TaskCanceledException exception) when (!token.IsCancellationRequested)
        {
            throw new InvalidOperationException($"Cannot reach the browser at port {port}: the version address did not answer in time.", exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or UriFormatException)
        {
            throw new InvalidOperationException($"Cannot reach the browser at port {port}: {FirstLine(exception.Message)}", exception);
        }

        JsonNode? root;

        try
        {
            root = JsonNode.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"The browser at port {port} answered no debugging address: {exception.Message}", exception);
        }

        if (root is JsonObject version
            && version["webSocketDebuggerUrl"] is JsonValue address
            && address.TryGetValue<string>(out var url)
            && url.Length > 0)
        {
            return url;
        }

        throw new InvalidOperationException($"The browser at port {port} answered no debugging address.");
    }

    /// <summary>Reads one browser stream to its end, so the browser never blocks on a full buffer.</summary>
    /// <param name="reader">The stream reader.</param>
    /// <returns>A task that completes when the stream closes.</returns>
    static async Task DrainAsync(StreamReader reader)
    {
        try
        {
            await reader.ReadToEndAsync();
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ObjectDisposedException or OutOfMemoryException)
        {
            // The launch reads nothing back; the drain only keeps the browser unblocked.
        }
    }

    /// <summary>Removes a profile folder the launch leaves behind.</summary>
    /// <param name="tempDir">The profile folder.</param>
    static void RemoveProfile(string tempDir)
    {
        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // A profile the system will not remove still fails the launch the same way.
        }
    }

    /// <summary>The first non-empty line of a message.</summary>
    /// <param name="message">The message.</param>
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
}
