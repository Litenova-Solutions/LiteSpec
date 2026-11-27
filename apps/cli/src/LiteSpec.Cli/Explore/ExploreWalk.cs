using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Explore;

/// <summary>Drives the running API with valid-looking calls built from its OpenAPI document and reports every fault. It asserts nothing about behavior; it only reports.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class ExploreWalk
{
    /// <summary>The media type of a handled client error.</summary>
    internal const string ProblemDetailsMediaType = "application/problem+json";

    /// <summary>How many response characters a fault keeps.</summary>
    internal const int ResponseExcerptLength = 500;

    /// <summary>How many request-body characters a fault keeps.</summary>
    internal const int RequestExcerptLength = 2000;

    /// <summary>The settings of one walk: the seed, the call count, the denies and the headers.</summary>
    /// <param name="Steps">How many calls the walk makes.</param>
    /// <param name="Seed">The seed that replays the same calls.</param>
    /// <param name="Denies">The deny entries with their reasons.</param>
    /// <param name="Headers">The request headers sent on every call.</param>
    /// <param name="CookieHeader">The Cookie header value, or null to send no cookies.</param>
    internal sealed record Settings(
        int Steps,
        int Seed,
        IReadOnlyList<(ExploreDeny Deny, string Reason)> Denies,
        IReadOnlyList<(string Name, string Value)> Headers,
        string? CookieHeader);

    /// <summary>The result of one walk: the calls made, the faults found and the status counts.</summary>
    /// <param name="Steps">How many calls the walk made.</param>
    /// <param name="Faults">The faults found, in step order.</param>
    /// <param name="StatusCounts">The calls per status code, with timeouts under timeout.</param>
    /// <param name="OperationsListed">How many callable operations the document lists.</param>
    /// <param name="Skipped">The denied templates with their reasons.</param>
    /// <param name="StoppedEarly">Why the walk stopped before its steps, or null when it ran them all.</param>
    internal sealed record Result(
        int Steps,
        IReadOnlyList<ExploreFault> Faults,
        IReadOnlyDictionary<string, int> StatusCounts,
        int OperationsListed,
        IReadOnlyList<(string Template, string Reason)> Skipped,
        string? StoppedEarly);

    /// <summary>Walks the API and collects the faults.</summary>
    /// <param name="http">The client calling the running API.</param>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="settings">The walk settings.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    /// <returns>The walk result.</returns>
    internal static async Task<Result> RunAsync(HttpClient http, JsonObject document, Settings settings, CancellationToken cancellationToken)
    {
        var skipped = new List<(string Template, string Reason)>();
        var operations = ExploreOpenApi.ParseOperations(document, settings.Denies, skipped);
        var random = new Random(settings.Seed);
        var identifiers = new ExploreIdentifiers();
        var faults = new List<ExploreFault>();
        var statusCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var steps = 0;
        string? stoppedEarly = null;

        foreach (var operation in operations)
        {
            if (operation.Method != "GET" || operation.PathParameters.Count != 0)
            {
                continue;
            }

            if (steps >= settings.Steps)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            steps++;
            stoppedEarly = await CallOnceAsync(http, operation, steps, random, identifiers, settings, faults, statusCounts, cancellationToken);

            if (stoppedEarly is not null)
            {
                break;
            }
        }

        while (steps < settings.Steps && stoppedEarly is null && operations.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            steps++;
            var operation = operations[random.Next(operations.Count)];
            stoppedEarly = await CallOnceAsync(http, operation, steps, random, identifiers, settings, faults, statusCounts, cancellationToken);
        }

        return new Result(steps, faults, statusCounts, operations.Count, skipped, stoppedEarly);
    }

    /// <summary>Whether a response is a fault: a 5xx, or a 4xx that is not problem details.</summary>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="contentType">The response content type, or null.</param>
    /// <returns>Whether the response is a fault.</returns>
    internal static bool IsFault(int statusCode, string? contentType)
    {
        if (statusCode >= 500)
        {
            return true;
        }

        return statusCode is >= 400 and < 500
            && contentType?.Contains(ProblemDetailsMediaType, StringComparison.OrdinalIgnoreCase) != true;
    }

    /// <summary>Reads the problem detail from a response body.</summary>
    /// <param name="body">The response body.</param>
    /// <returns>The detail string, or null when the body carries none.</returns>
    internal static string? ProblemDetail(string body)
    {
        try
        {
            if (JsonNode.Parse(body) is JsonObject problem
                && problem["detail"] is JsonValue detail
                && detail.TryGetValue<string>(out var text)
                && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }
        catch (JsonException)
        {
            // A body that is not JSON carries no problem detail.
        }

        return null;
    }

    /// <summary>Makes one call of the walk and records a fault when it finds one.</summary>
    /// <param name="http">The client calling the running API.</param>
    /// <param name="operation">The operation to call.</param>
    /// <param name="step">The step number.</param>
    /// <param name="random">The walk's random source.</param>
    /// <param name="identifiers">The values collected from earlier responses.</param>
    /// <param name="settings">The walk settings.</param>
    /// <param name="faults">Collects the faults.</param>
    /// <param name="statusCounts">Counts the calls per status code.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Why the walk must stop, or null when it continues.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The request owns its content and disposes it with the using request and response.")]
    static async Task<string?> CallOnceAsync(
        HttpClient http,
        ExploreOperation operation,
        int step,
        Random random,
        ExploreIdentifiers identifiers,
        Settings settings,
        List<ExploreFault> faults,
        Dictionary<string, int> statusCounts,
        CancellationToken cancellationToken)
    {
        var path = ExploreRequests.BuildPath(operation, identifiers, random);
        var bodyText = ExploreRequests.BuildBody(operation, random)?.ToJsonString();
        string? requestBody = bodyText is null ? null : Excerpt(bodyText, RequestExcerptLength);

        using var request = new HttpRequestMessage(new HttpMethod(operation.Method), path);

        if (bodyText is not null)
        {
            request.Content = new StringContent(bodyText, Encoding.UTF8, operation.RequestContentType ?? "application/json");
        }

        ApplyHeaders(request, settings);

        int status;
        string responseBody;
        string? contentType;

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            status = (int)response.StatusCode;
            contentType = response.Content.Headers.ContentType?.MediaType;
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Count(statusCounts, ExploreFault.Timeout);
            faults.Add(new ExploreFault(ExploreFault.Timeout, operation.Name, operation.Method, operation.PathTemplate, path, requestBody, 0, string.Empty, null, step));
            return null;
        }
        catch (HttpRequestException exception)
        {
            return exception.Message;
        }

        Count(statusCounts, status.ToString(CultureInfo.InvariantCulture));

        if (operation.Method == "GET"
            && status is >= 200 and < 300
            && LooksLikeJson(contentType, responseBody))
        {
            try
            {
                identifiers.Collect(JsonNode.Parse(responseBody));
            }
            catch (JsonException)
            {
                // A 2xx body that is not JSON carries no values.
            }
        }

        if (IsFault(status, contentType))
        {
            faults.Add(new ExploreFault(
                status >= 500 ? ExploreFault.ServerError : ExploreFault.ClientFormat,
                operation.Name,
                operation.Method,
                operation.PathTemplate,
                path,
                requestBody,
                status,
                Excerpt(responseBody, ResponseExcerptLength),
                status >= 500 ? ProblemDetail(responseBody) : null,
                step));
        }

        return null;
    }

    /// <summary>Sends the walk headers and cookies on one request.</summary>
    /// <param name="request">The request to send.</param>
    /// <param name="settings">The walk settings.</param>
    static void ApplyHeaders(HttpRequestMessage request, Settings settings)
    {
        foreach (var (name, value) in settings.Headers)
        {
            if (!request.Headers.TryAddWithoutValidation(name, value) && request.Content is not null)
            {
                request.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        if (settings.CookieHeader is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", settings.CookieHeader);
        }
    }

    /// <summary>Counts one call under one status key.</summary>
    /// <param name="counts">The calls per status code.</param>
    /// <param name="key">The status code, or timeout.</param>
    static void Count(Dictionary<string, int> counts, string key)
    {
        counts.TryGetValue(key, out var count);
        counts[key] = count + 1;
    }

    /// <summary>Whether a response body looks like JSON worth collecting values from.</summary>
    /// <param name="contentType">The response content type, or null.</param>
    /// <param name="body">The response body.</param>
    /// <returns>Whether the body looks like JSON.</returns>
    static bool LooksLikeJson(string? contentType, string body)
    {
        if (contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        foreach (var character in body)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            return character == '{' || character == '[';
        }

        return false;
    }

    /// <summary>Keeps the first characters of a text.</summary>
    /// <param name="text">The text.</param>
    /// <param name="length">How many characters to keep.</param>
    /// <returns>The excerpt.</returns>
    static string Excerpt(string text, int length) =>
        text.Length > length ? text[..length] : text;
}
