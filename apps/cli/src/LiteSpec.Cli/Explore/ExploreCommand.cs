using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Explore;

/// <summary>The <c>litespec api explore</c> command: walks a running API from its OpenAPI document and reports server faults.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class ExploreCommand
{
    /// <summary>How long one call may take before the walk reports it as a timeout.</summary>
    static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Runs the walk: reads the OpenAPI document, makes the calls, and writes the reports.</summary>
    /// <param name="args">The command arguments after the subcommand.</param>
    /// <returns>The exit code: 0 for no 5xx, 1 for a 5xx or a failed run, 2 for a usage error.</returns>
    public static int Run(string[] args)
    {
        var (headersRaw, deniesRaw, rest, splitError) = ExploreOptions.SplitRepeatable(args);
        var parsed = CliArgs.Parse(
            [.. rest],
            ["--allow-delete", "--allow-remote"],
            ["--base-url", "--openapi", "--steps", "--seed", "--cookie-file", "--out"],
            maxPositionals: 0);

        if (splitError is not null)
        {
            return CommandFailure.UsageError(splitError, ApiCommand.Usage, json: false);
        }

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(ApiCommand.Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, ApiCommand.Usage, json: false);
        }

        var (options, optionsError) = ExploreOptions.Parse(parsed, headersRaw, deniesRaw);

        if (options is null)
        {
            return CommandFailure.UsageError(optionsError!, ApiCommand.Usage, json: false);
        }

        string? cookieHeader = null;

        if (options.CookieFile is { } cookieOption)
        {
            var cookieFull = FullPath(cookieOption);

            if (!File.Exists(cookieFull))
            {
                return CommandFailure.UsageError($"Cookie file '{cookieOption}' does not exist.", ApiCommand.Usage, json: false);
            }

            var (header, cookieError) = ExploreCookies.ReadFile(cookieFull, cookieOption);

            if (header is null)
            {
                return CommandFailure.UsageError(cookieError!, ApiCommand.Usage, json: false);
            }

            cookieHeader = header;
        }

        var outDir = FullPath(options.Out);

        try
        {
            Directory.CreateDirectory(outDir);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CommandFailure.Fail($"Cannot write '{options.Out}': {exception.Message}", json: false, code: 1);
        }

        using var http = CreateClient(options.BaseUrl);
        var started = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var (document, openApiError) = ReadOpenApi(http, options);

        if (document is null)
        {
            return CommandFailure.Fail(openApiError!, json: false, code: 1);
        }

        var denies = new List<(ExploreDeny Deny, string Reason)>();
        denies.AddRange(ExploreDeny.Defaults(options.AllowDelete));

        foreach (var deny in options.Denies)
        {
            denies.Add((deny, $"Matched --deny {deny.Method} {deny.Pattern}."));
        }

        var result = ExploreWalk.RunAsync(
            http,
            document,
            new ExploreWalk.Settings(options.Steps, options.Seed, denies, options.Headers, cookieHeader),
            CancellationToken.None).GetAwaiter().GetResult();

        stopwatch.Stop();

        foreach (var fault in result.Faults)
        {
            CliHost.Out.WriteLine(ExploreReport.Line(fault));
        }

        var markdownPath = Path.Combine(outDir, ExploreReport.MarkdownFileName);
        var jsonPath = Path.Combine(outDir, ExploreReport.JsonFileName);

        try
        {
            File.WriteAllText(
                markdownPath,
                ExploreReport.AsMarkdown(options.BaseUrl, options.OpenApi, options.Seed, started, stopwatch.Elapsed, options.Steps, result),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.WriteAllText(
                jsonPath,
                CliJson.Serialize(ExploreReport.AsJson(options.BaseUrl, options.OpenApi, options.Seed, result)),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CommandFailure.Fail($"Cannot write '{outDir}': {exception.Message}", json: false, code: 1);
        }

        if (result.OperationsListed == 0)
        {
            CliHost.Out.WriteLine("No callable operations in the OpenAPI document.");
        }
        else if (result.Faults.Count == 0)
        {
            CliHost.Out.WriteLine($"No faults in {result.Steps} calls.");
        }
        else
        {
            CliHost.Out.WriteLine();
            CliHost.Out.WriteLine($"{result.Faults.Count} faults in {result.Steps} calls ({SummaryCounts(result)}).");
        }

        if (result.StoppedEarly is { } stopped)
        {
            CliHost.Out.WriteLine($"Stopped early: {stopped}");
        }

        CliHost.Out.WriteLine($"Wrote {markdownPath} and {jsonPath}.");

        return result.Faults.Any(fault => string.Equals(fault.Kind, ExploreFault.ServerError, StringComparison.Ordinal)) ? 1 : 0;
    }

    /// <summary>Creates the client calling the running API: no redirects, no cookie jar, one timeout.</summary>
    /// <param name="baseUrl">The running API base URL, without a trailing slash.</param>
    /// <returns>The client.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The client owns its handler and the caller disposes the client with using.")]
    static HttpClient CreateClient(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
        };
        return new HttpClient(handler) { BaseAddress = new Uri(baseUrl + "/", UriKind.Absolute), Timeout = RequestTimeout };
    }

    /// <summary>Reads the OpenAPI document the walk lists its operations from.</summary>
    /// <param name="http">The client calling the running API.</param>
    /// <param name="options">The parsed explore options.</param>
    /// <returns>The document, or the reason it cannot be read.</returns>
    static (JsonObject? Document, string? Error) ReadOpenApi(HttpClient http, ExploreOptions options)
    {
        if (Uri.TryCreate(options.OpenApi, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return FetchOpenApi(http, options.OpenApi, options.OpenApi);
        }

        var file = FullPath(options.OpenApi);

        if (File.Exists(file))
        {
            return ReadOpenApiFile(file, options.OpenApi);
        }

        var path = options.OpenApi.StartsWith('/') ? options.OpenApi : "/" + options.OpenApi;
        return FetchOpenApi(http, path, options.BaseUrl + path);
    }

    /// <summary>Fetches the OpenAPI document from the running API.</summary>
    /// <param name="http">The client calling the running API.</param>
    /// <param name="address">The address to fetch, relative or absolute.</param>
    /// <param name="display">The address the errors name.</param>
    /// <returns>The document, or the reason it cannot be read.</returns>
    static (JsonObject? Document, string? Error) FetchOpenApi(HttpClient http, string address, string display)
    {
        string body;
        int status;

        try
        {
            using var response = http.GetAsync(address).GetAwaiter().GetResult();
            status = (int)response.StatusCode;
            body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch (TaskCanceledException)
        {
            return (null, $"Cannot read the OpenAPI document at '{display}': the request timed out.");
        }
        catch (HttpRequestException exception)
        {
            return (null, $"Cannot read the OpenAPI document at '{display}': {exception.Message}");
        }

        if (status is < 200 or >= 300)
        {
            return (null, $"Cannot read the OpenAPI document at '{display}': answered {status}.");
        }

        return ReadOpenApiText(body, display);
    }

    /// <summary>Reads the OpenAPI document from a file.</summary>
    /// <param name="file">The absolute file path.</param>
    /// <param name="display">The option value the errors name.</param>
    /// <returns>The document, or the reason it cannot be read.</returns>
    static (JsonObject? Document, string? Error) ReadOpenApiFile(string file, string display)
    {
        string body;

        try
        {
            body = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (null, $"Cannot read the OpenAPI document at '{display}': {exception.Message}");
        }

        return ReadOpenApiText(body, display);
    }

    /// <summary>Parses the OpenAPI document text.</summary>
    /// <param name="body">The document text.</param>
    /// <param name="display">The address the errors name.</param>
    /// <returns>The document, or the reason it cannot be read.</returns>
    static (JsonObject? Document, string? Error) ReadOpenApiText(string body, string display)
    {
        try
        {
            if (JsonNode.Parse(body) is JsonObject document)
            {
                return (document, null);
            }
        }
        catch (JsonException)
        {
            // Reported below as a document that is not JSON.
        }

        return (null, $"Cannot read the OpenAPI document at '{display}': the document is not JSON.");
    }

    /// <summary>Counts the faults by kind for the summary line.</summary>
    /// <param name="result">The walk result.</param>
    /// <returns>The non-zero kind counts, such as server-error 2, timeout 1.</returns>
    static string SummaryCounts(ExploreWalk.Result result)
    {
        var parts = new List<string>();
        var server = result.Faults.Count(fault => string.Equals(fault.Kind, ExploreFault.ServerError, StringComparison.Ordinal));
        var client = result.Faults.Count(fault => string.Equals(fault.Kind, ExploreFault.ClientFormat, StringComparison.Ordinal));
        var timeout = result.Faults.Count(fault => string.Equals(fault.Kind, ExploreFault.Timeout, StringComparison.Ordinal));

        if (server > 0)
        {
            parts.Add($"{ExploreFault.ServerError} {server}");
        }

        if (client > 0)
        {
            parts.Add($"{ExploreFault.ClientFormat} {client}");
        }

        if (timeout > 0)
        {
            parts.Add($"{ExploreFault.Timeout} {timeout}");
        }

        return string.Join(", ", parts);
    }

    /// <summary>The absolute path of an option value: as written when absolute, else under the working directory.</summary>
    /// <param name="path">The option value.</param>
    /// <returns>The absolute path.</returns>
    static string FullPath(string path) =>
        Path.IsPathFullyQualified(path) ? path : Path.Combine(CliHost.WorkingDirectory, path);
}
