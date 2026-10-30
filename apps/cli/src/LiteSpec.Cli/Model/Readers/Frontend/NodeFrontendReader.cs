using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace LiteSpec.Model.Readers.Frontend;

/// <summary>
///     Runs the TypeScript reader through Node once per build or check, as the
///     frontend facts protocol states: one JSON request on standard input, one
///     JSON response on standard output. The bundle sits beside the tool, or at
///     <c>apps/cli/fe-reader/dist/litespec-fe-reader.mjs</c> in a source checkout.
///     Node absent, a non-zero exit, unparseable output or a protocol mismatch
///     leaves every affected frontend with empty facts and one
///     <c>reader-unavailable</c> cause. The reader is qualified when its
///     handshake matched; nothing in <c>litespec.project.json</c> disqualifies it.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
static class NodeFrontendReader
{
    /// <summary>The frontend facts protocol version this CLI speaks.</summary>
    public const int Protocol = 1;

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    /// <summary>Reads every Svelte and React frontend of the consumer, in declared order.</summary>
    public static IReadOnlyList<FrontendFacts> Read(ConsumerConfig config) =>
        Read(config, LocateBundle(), FindNode());

    /// <summary>Reads with an explicit bundle and Node, so a test can name a fake reader.</summary>
    internal static IReadOnlyList<FrontendFacts> Read(ConsumerConfig config, string? bundle, string? node)
    {
        var web = config.Frontends
            .Where(frontend => frontend.Platform is "svelte" or "react")
            .ToList();

        if (web.Count == 0)
        {
            return [];
        }

        if (node is null)
        {
            return [.. web.Select(frontend => FrontendFacts.Failed(
                frontend.Name, Finding.TypeScriptReader, "Node is absent from PATH."))];
        }

        if (bundle is null || !File.Exists(bundle))
        {
            return [.. web.Select(frontend => FrontendFacts.Failed(
                frontend.Name, Finding.TypeScriptReader,
                "the TypeScript reader bundle 'litespec-fe-reader.mjs' was not found beside the tool or in 'apps/cli/fe-reader/dist'."))];
        }

        string output;
        string error;

        try
        {
            (output, error) = RunReader(config, web, bundle, node);
        }
        catch (ReaderFailure failure)
        {
            return [.. web.Select(frontend => FrontendFacts.Failed(
                frontend.Name, Finding.TypeScriptReader, failure.Message))];
        }

        return ParseResponse(web, output);
    }

    /// <summary>The bundle beside the tool, else the source checkout's built bundle, else null.</summary>
    public static string? LocateBundle()
    {
        var beside = Path.Combine(AppContext.BaseDirectory, "litespec-fe-reader.mjs");

        if (File.Exists(beside))
        {
            return beside;
        }

        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, "apps", "cli", "fe-reader", "dist", "litespec-fe-reader.mjs");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        for (var folder = new DirectoryInfo(Directory.GetCurrentDirectory()); folder is not null; folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, "apps", "cli", "fe-reader", "dist", "litespec-fe-reader.mjs");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>The Node executable on PATH, or null when Node is absent.</summary>
    public static string? FindNode()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";

        foreach (var folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in new[] { "node.exe", "node" })
            {
                var candidate = Path.Combine(folder.Trim(), name);

                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private sealed class ReaderFailure(string message) : Exception(message);

    private static (string Output, string Error) RunReader(
        ConsumerConfig config, IReadOnlyList<FrontendConfig> web, string bundle, string node)
    {
        var start = new ProcessStartInfo(node)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = config.Root,
        };

        start.ArgumentList.Add(bundle);

        using var process = Process.Start(start)
            ?? throw new ReaderFailure("Node could not be started.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        process.StandardInput.Write(BuildRequest(config, web));
        process.StandardInput.Close();

        if (!process.WaitForExit(Timeout))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new ReaderFailure("the TypeScript reader did not finish in two minutes.");
        }

        var output = stdout.GetAwaiter().GetResult();
        var error = stderr.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
        {
            var first = error.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";

            throw new ReaderFailure(
                $"the TypeScript reader exited with code {process.ExitCode}" + (first.Length > 0 ? $": {first}" : "."));
        }

        return (output, error);
    }

    /// <summary>The versioned JSON request the protocol states, with absolute forward-slash paths.</summary>
    internal static string BuildRequest(ConsumerConfig config, IReadOnlyList<FrontendConfig> web)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("protocol", Protocol);
            writer.WriteStartArray("frontends");

            foreach (var frontend in web)
            {
                writer.WriteStartObject();
                writer.WriteString("name", frontend.Name);
                writer.WriteString("platform", frontend.Platform);
                writer.WriteString("root", Slash(frontend.Path));
                writer.WriteString("routes", Slash(frontend.RoutesRoot));
                writer.WriteString("modules", Slash(frontend.ModulesRoot));
                writer.WriteString("frame", Slash(frontend.FrameRoot));
                writer.WriteStartArray("testRoots");

                foreach (var root in TestRoots(config, frontend))
                {
                    writer.WriteStringValue(Slash(root));
                }

                writer.WriteEndArray();
                writer.WriteStartObject("aliases");

                foreach (var (alias, target) in Aliases(frontend))
                {
                    writer.WriteString(alias, Slash(target));
                }

                writer.WriteEndObject();
                writer.WriteString("apiClient", Slash(config.ApiClientSrc));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteString("cache", Slash(Path.Combine(config.Root, ".model", "frontend-facts")));
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    ///     The import aliases a frontend resolves: the <c>#</c> subpath imports its
    ///     <c>package.json</c> declares, or the stack's own alias when it declares none
    ///     (<c>@</c> for <c>src</c> on React, <c>#lib</c> for <c>src/lib</c> on Svelte).
    /// </summary>
    internal static IReadOnlyList<(string Alias, string Target)> Aliases(FrontendConfig frontend)
    {
        var declared = new List<(string, string)>();
        var manifest = Path.Combine(frontend.Path, "package.json");

        if (File.Exists(manifest))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(manifest));

                if (document.RootElement.TryGetProperty("imports", out var imports)
                    && imports.ValueKind == JsonValueKind.Object)
                {
                    foreach (var entry in imports.EnumerateObject())
                    {
                        if (entry.Name.EndsWith("/*", StringComparison.Ordinal)
                            && entry.Value.ValueKind == JsonValueKind.String
                            && entry.Value.GetString() is { } target
                            && target.EndsWith("/*", StringComparison.Ordinal))
                        {
                            declared.Add((entry.Name[..^2], Path.GetFullPath(Path.Combine(frontend.Path, target[..^2]))));
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // An unreadable package file declares nothing; the stack alias applies.
            }
        }

        if (declared.Count > 0)
        {
            return declared;
        }

        return frontend.Platform == "react"
            ? [("@", Path.Combine(frontend.Path, "src"))]
            : [("#lib", Path.Combine(frontend.Path, "src", "lib"))];
    }

    private static IReadOnlyList<string> TestRoots(ConsumerConfig config, FrontendConfig frontend)
    {
        var roots = new List<string> { Path.Combine(frontend.Path, "tests") };

        foreach (var root in config.TestRoots)
        {
            if (root.StartsWith(frontend.Path + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !roots.Contains(root, StringComparer.OrdinalIgnoreCase))
            {
                roots.Add(root);
            }
        }

        return roots;
    }

    /// <summary>Parses one response; a handshake mismatch fails every frontend it was asked for.</summary>
    internal static IReadOnlyList<FrontendFacts> ParseResponse(IReadOnlyList<FrontendConfig> web, string output)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(output);
        }
        catch (JsonException)
        {
            return FailAll(web, "the TypeScript reader output was not a JSON response.");
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("protocol", out var protocol)
                || protocol.ValueKind != JsonValueKind.Number
                || !protocol.TryGetInt32(out var version))
            {
                return FailAll(web, "the TypeScript reader response names no protocol version.");
            }

            if (version != Protocol)
            {
                return FailAll(web, $"the TypeScript reader handshake names protocol {version}, not protocol {Protocol}.");
            }

            if (!root.TryGetProperty("reader", out var reader)
                || reader.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("typescript", out var typescript)
                || typescript.ValueKind != JsonValueKind.String)
            {
                return FailAll(web, "the TypeScript reader handshake names no reader or TypeScript version.");
            }

            var byName = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

            if (root.TryGetProperty("frontends", out var frontends) && frontends.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in frontends.EnumerateArray())
                {
                    if (entry.TryGetProperty("name", out var name) && name.GetString() is { } named)
                    {
                        byName[named] = entry.Clone();
                    }
                }
            }

            var found = new List<FrontendFacts>();

            foreach (var frontend in web)
            {
                found.Add(byName.TryGetValue(frontend.Name, out var entry)
                    ? ParseFrontend(frontend.Name, entry)
                    : FrontendFacts.Failed(
                        frontend.Name, Finding.TypeScriptReader,
                        $"the TypeScript reader returned no facts for frontend '{frontend.Name}'."));
            }

            return found;
        }
    }

    private static IReadOnlyList<FrontendFacts> FailAll(IReadOnlyList<FrontendConfig> web, string cause) =>
        [.. web.Select(frontend => FrontendFacts.Failed(frontend.Name, Finding.TypeScriptReader, cause))];

    private static FrontendFacts ParseFrontend(string name, JsonElement entry) =>
        new(
            name,
            Finding.TypeScriptReader,
            Qualified: true,
            Failure: null,
            Modules: ArrayOf(entry, "modules", element => new ModuleFact(
                Text(element, "folder"), Text(element, "file"), Line(element), Strings(element, "marks"))),
            Routes: ArrayOf(entry, "routes", element => new RouteFact(
                Text(element, "file"), Line(element), Text(element, "address"), Strings(element, "marks"),
                ArrayOf(element, "calls", call => new CallFact(Text(call, "operation"), Text(call, "file"), Line(call))))),
            Tests: ArrayOf(entry, "tests", element => new BrowserTestFact(
                Text(element, "file"), Line(element), Text(element, "title"), Strings(element, "proves"),
                element.TryGetProperty("journey", out var journey) && journey.ValueKind == JsonValueKind.String
                    ? journey.GetString()
                    : null,
                Strings(element, "gotos"), Strings(element, "codes"), Strings(element, "texts"))),
            Diagnostics: ArrayOf(entry, "diagnostics", element => new FrontendDiagnostic(
                Text(element, "file"), Line(element), Text(element, "message"))));

    private static IReadOnlyList<T> ArrayOf<T>(JsonElement owner, string field, Func<JsonElement, T> read) =>
        owner.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray().Select(read)]
            : [];

    private static IReadOnlyList<string> Strings(JsonElement owner, string field) =>
        owner.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.String)
                .Select(entry => entry.GetString()!)]
            : [];

    private static string Text(JsonElement owner, string field) =>
        owner.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static int Line(JsonElement owner) =>
        owner.TryGetProperty("line", out var value) && value.TryGetInt32(out var line) && line > 0 ? line : 1;

    private static string Slash(string path) => path.Replace(Path.DirectorySeparatorChar, '/');
}
