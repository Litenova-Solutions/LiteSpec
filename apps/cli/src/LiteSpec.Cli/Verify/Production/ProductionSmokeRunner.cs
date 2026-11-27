using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Verify.Production;

/// <summary>Runs the production smoke: compose up, the optional migrate, the health wait, the path checks, and the stub read, always ending with compose down.</summary>
/// <follows>litespec/core/release#boot-the-production-configuration-before-release</follows>
static class ProductionSmokeRunner
{
    /// <summary>Runs the whole smoke and returns its findings, always running the down command before returning.</summary>
    /// <param name="production">The production settings from the verify block.</param>
    /// <param name="baseUrl">The production address the smoke probes.</param>
    /// <param name="processes">The runner that starts the container commands.</param>
    /// <param name="workdir">The consumer root the container commands run in.</param>
    /// <param name="healthTimeout">How long the health wait polls before reporting production-readiness-failed.</param>
    /// <param name="pollInterval">How long the health wait rests between polls.</param>
    /// <param name="requestTimeout">How long one probe request may take before it counts as no answer.</param>
    /// <returns>The findings the smoke raised, in run order.</returns>
    internal static IReadOnlyList<ProductionSmokeFinding> Run(
        VerifyProduction production,
        string baseUrl,
        IProductionProcessRunner processes,
        string workdir,
        TimeSpan healthTimeout,
        TimeSpan pollInterval,
        TimeSpan requestTimeout)
    {
        var up = processes.Run("docker", UpArgs(production), workdir);

        if (up.Status != 0)
        {
            var failed = new List<ProductionSmokeFinding>
            {
                new("production-readiness-failed", production.Compose, $"The compose up failed: {up.Error}"),
            };
            Down(processes, production, workdir);
            return failed;
        }

        var findings = new List<ProductionSmokeFinding>();

        try
        {
            if (production.Migrate is { Length: > 0 } migrate)
            {
                var (file, args) = SplitCommand(migrate);

                if (file.Length == 0)
                {
                    findings.Add(new ProductionSmokeFinding("production-readiness-failed", migrate, "The migrate command is empty."));
                    return findings;
                }

                var migrated = processes.Run(file, args, workdir);

                if (migrated.Status != 0)
                {
                    findings.Add(new ProductionSmokeFinding("production-readiness-failed", migrate, $"The migrate command failed: {migrated.Error}"));
                    return findings;
                }
            }

            var (healthy, body) = WaitForHealth(Join(baseUrl, production.Health), healthTimeout, pollInterval, requestTimeout);

            if (!healthy)
            {
                findings.Add(body is null
                    ? new ProductionSmokeFinding("production-readiness-failed", production.Health, $"The production host did not answer {production.Health} with success within {healthTimeout.TotalSeconds:0} seconds.")
                    : new ProductionSmokeFinding("production-readiness-failed", production.Health, $"The production host answered {production.Health} with no success within {healthTimeout.TotalSeconds:0} seconds."));
                return findings;
            }

            findings.AddRange(CheckPaths(baseUrl, production.Paths, requestTimeout));
            findings.AddRange(ReadStubs(body));
            return findings;
        }
        finally
        {
            Down(processes, production, workdir);
        }
    }

    /// <summary>The compose up command the smoke boots the production containers with.</summary>
    /// <param name="production">The production settings from the verify block.</param>
    /// <returns>The docker arguments that boot the containers.</returns>
    static string[] UpArgs(VerifyProduction production) =>
        ["compose", "-f", production.Compose, "--env-file", production.EnvFile, "up", "-d", "--build"];

    /// <summary>Runs the compose down command that stops the production containers.</summary>
    /// <param name="processes">The runner that starts the container commands.</param>
    /// <param name="production">The production settings from the verify block.</param>
    /// <param name="workdir">The consumer root the container commands run in.</param>
    static void Down(IProductionProcessRunner processes, VerifyProduction production, string workdir) =>
        processes.Run("docker", ["compose", "-f", production.Compose, "--env-file", production.EnvFile, "down", "-v"], workdir);

    /// <summary>Polls the readiness path until it answers 2xx or the timeout passes.</summary>
    /// <param name="address">The readiness address.</param>
    /// <param name="timeout">How long the wait polls before giving up.</param>
    /// <param name="poll">How long the wait rests between polls.</param>
    /// <param name="requestTimeout">How long one poll request may take.</param>
    /// <returns>Whether the host answered 2xx, with the last answer body if one arrived.</returns>
    static (bool Healthy, string? Body) WaitForHealth(string address, TimeSpan timeout, TimeSpan poll, TimeSpan requestTimeout)
    {
        using var http = CreateClient(requestTimeout);
        var started = DateTime.UtcNow;
        string? body = null;

        while (true)
        {
            try
            {
                using var response = http.GetAsync(address).GetAwaiter().GetResult();
                body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if ((int)response.StatusCode is >= 200 and < 300)
                {
                    return (true, body);
                }
            }
            catch (HttpRequestException)
            {
                // No answer on this poll; the wait retries until the timeout passes.
            }
            catch (TaskCanceledException)
            {
                // The poll request timed out; the wait retries until the timeout passes.
            }

            var remaining = timeout - (DateTime.UtcNow - started);

            if (remaining <= TimeSpan.Zero)
            {
                return (false, body);
            }

            Thread.Sleep(remaining < poll ? remaining : poll);
        }
    }

    /// <summary>Requests each smoke path with the forwarded https scheme and reports a 5xx or no answer.</summary>
    /// <param name="baseUrl">The production address the smoke probes.</param>
    /// <param name="paths">The smoke paths from the verify block.</param>
    /// <param name="requestTimeout">How long one probe request may take before it counts as no answer.</param>
    /// <returns>One path-failed finding per path that answered 5xx or gave no answer.</returns>
    static List<ProductionSmokeFinding> CheckPaths(string baseUrl, IReadOnlyList<string> paths, TimeSpan requestTimeout)
    {
        using var http = CreateClient(requestTimeout);
        var findings = new List<ProductionSmokeFinding>();

        foreach (var path in paths)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, Join(baseUrl, path));
                request.Headers.Add("X-Forwarded-Proto", "https");
                using var response = http.SendAsync(request).GetAwaiter().GetResult();

                if ((int)response.StatusCode >= 500)
                {
                    findings.Add(new ProductionSmokeFinding("path-failed", path, $"The production host answered {path} with {(int)response.StatusCode}."));
                }
            }
            catch (HttpRequestException)
            {
                findings.Add(new ProductionSmokeFinding("path-failed", path, $"The production host gave no answer on {path}."));
            }
            catch (TaskCanceledException)
            {
                findings.Add(new ProductionSmokeFinding("path-failed", path, $"The production host gave no answer on {path}."));
            }
        }

        return findings;
    }

    /// <summary>Reads the readiness body and reports one stub-adapter-shown finding per listed stub adapter.</summary>
    /// <param name="body">The readiness answer body.</param>
    /// <returns>One stub-adapter-shown finding per entry of the top-level stubAdapters array.</returns>
    static List<ProductionSmokeFinding> ReadStubs(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return [];
        }

        JsonNode? node;

        try
        {
            node = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return [];
        }

        if (node is not JsonObject report || report["stubAdapters"] is not JsonArray adapters)
        {
            return [];
        }

        var findings = new List<ProductionSmokeFinding>();

        foreach (var entry in adapters)
        {
            if (entry is JsonValue value && value.TryGetValue<string>(out var adapter) && adapter.Length > 0)
            {
                findings.Add(new ProductionSmokeFinding("stub-adapter-shown", adapter, $"The production readiness lists stub adapter '{adapter}', so a stand-in serves production traffic."));
            }
        }

        return findings;
    }

    /// <summary>Splits a migrate command on ASCII spaces, keeping double-quoted parts together.</summary>
    /// <param name="command">The migrate command from the verify block.</param>
    /// <returns>The program and its arguments.</returns>
    static (string File, string[] Args) SplitCommand(string command)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        foreach (var character in command)
        {
            if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == ' ' && !quoted)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(character);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts.Count == 0 ? ("", []) : (parts[0], [.. parts.Skip(1)]);
    }

    /// <summary>Joins the base address and a path with exactly one slash.</summary>
    /// <param name="baseUrl">The production address, with or without a trailing slash.</param>
    /// <param name="path">The path, with or without a leading slash.</param>
    /// <returns>The full address.</returns>
    static string Join(string baseUrl, string path) =>
        baseUrl.TrimEnd('/') + (path.StartsWith('/') ? path : "/" + path);

    /// <summary>Creates the client that probes the production host: no redirects, no cookie jar, one timeout.</summary>
    /// <param name="requestTimeout">How long one probe request may take.</param>
    /// <returns>The client.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller owns the client and disposes it with using.")]
    static HttpClient CreateClient(TimeSpan requestTimeout)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
        };
        return new HttpClient(handler) { Timeout = requestTimeout };
    }
}
