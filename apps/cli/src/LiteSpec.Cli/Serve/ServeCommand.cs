using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Serve;

/// <summary>
///     The <c>litespec serve</c> command: a stdio MCP server over the warm project model.
///     It reads one JSON-RPC 2.0 message per line, answers initialize, ping, tools/list
///     and tools/call, and rebuilds the model after a watched change settles for
///     300 ms. Fuse owns C# compiler diagnostics; this server never reports them.
/// </summary>
/// <follows>litespec/core/agent#select-task-context</follows>
public static class ServeCommand
{
    /// <summary>The usage text for the serve command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec serve [--root <consumer>]
          litespec serve --help

        Exit codes: 0 the session ended at end of input, 2 usage error or an unreadable consumer or carried release.
        """;

    /// <summary>Runs the serve command: opens the consumer and serves MCP messages on standard input and output.</summary>
    /// <param name="args">The command arguments, holding only --root.</param>
    /// <returns>The exit code: 0 the session ended at end of input, 2 usage error or unreadable input.</returns>
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var parsed = CliArgs.Parse(args, [], ["--root"]);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        ServeModel state;

        try
        {
            state = new ServeModel(config);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return CommandFailure.Fail($"Cannot build the project model: {exception.Message}", json: false, code: 2);
        }

        using (state)
        {
            return RunSession(Console.In, CliHost.Out, state);
        }
    }

    /// <summary>Runs one serve session over the readers: one JSON-RPC response per request line, none for notifications.</summary>
    /// <param name="input">The reader the JSON-RPC messages arrive on, one per line.</param>
    /// <param name="output">The writer each response is written to, one per line.</param>
    /// <param name="config">The consumer configuration the warm model is built from.</param>
    /// <returns>The exit code: 0 the session ended at end of input, 2 when the model cannot be built.</returns>
    internal static int RunSession(TextReader input, TextWriter output, ConsumerConfig config)
    {
        ServeModel state;

        try
        {
            state = new ServeModel(config);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return 2;
        }

        using (state)
        {
            return RunSession(input, output, state);
        }
    }

    /// <summary>Runs one serve session over the warm model: one JSON-RPC response per request line, none for notifications.</summary>
    /// <param name="input">The reader the JSON-RPC messages arrive on, one per line.</param>
    /// <param name="output">The writer each response is written to, one per line.</param>
    /// <param name="state">The warm model the tools answer from.</param>
    /// <returns>Exit code 0: the session ended at end of input.</returns>
    internal static int RunSession(TextReader input, TextWriter output, ServeModel state)
    {
        string? line;

        while ((line = input.ReadLine()) is not null)
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }

            if (HandleLine(line, state) is { } response)
            {
                output.WriteLine(response);
            }
        }

        output.Flush();
        return 0;
    }

    /// <summary>Handles one input line: parses the JSON-RPC message and projects the response line.</summary>
    /// <param name="line">The input line.</param>
    /// <param name="state">The warm model the tools answer from.</param>
    /// <returns>The response line, or null for a notification, which answers nothing.</returns>
    internal static string? HandleLine(string line, ServeModel state)
    {
        JsonNode? message;

        try
        {
            message = JsonNode.Parse(line);
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException)
        {
            return Compact(Error(null, -32700, "The message is not JSON."));
        }

        if (message is not JsonObject request)
        {
            return Compact(Error(null, -32600, "The message is not a JSON-RPC object."));
        }

        var method = JsonValues.AsString(request["method"]) ?? "";

        if (!string.Equals(JsonValues.AsString(request["jsonrpc"]), "2.0", StringComparison.Ordinal) || method.Length == 0)
        {
            return request["id"] is { } badId
                ? Compact(Error(badId, -32600, "The message is not a JSON-RPC 2.0 request."))
                : null;
        }

        // A notification carries no id, so it answers nothing, including initialized.
        if (request["id"] is not { } id)
        {
            Dispatch(request, method, state);
            return null;
        }

        try
        {
            var result = Dispatch(request, method, state);

            if (result is null)
            {
                return Compact(Error(id, -32601, $"Unknown method '{method}'."));
            }

            return Compact(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id.DeepClone(), ["result"] = result });
        }
        catch (InvalidOperationException exception) when (exception.Message.StartsWith("Unknown tool ", StringComparison.Ordinal))
        {
            return Compact(Error(id, -32601, exception.Message));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return Compact(Error(id, -32603, $"The tool failed: {exception.Message}"));
        }
    }

    /// <summary>Dispatches one JSON-RPC request to its method.</summary>
    /// <param name="request">The request object.</param>
    /// <param name="method">The request method.</param>
    /// <param name="state">The warm model the tools answer from.</param>
    /// <returns>The result object, or null for an unknown method or a notification method.</returns>
    static JsonObject? Dispatch(JsonObject request, string method, ServeModel state)
    {
        switch (method)
        {
            case "initialize":
                return new JsonObject
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "litespec-serve",
                        ["version"] = LiteSpecHome.Version ?? "unknown",
                    },
                };
            case "notifications/initialized":
                return new JsonObject();
            case "ping":
                return new JsonObject();
            case "tools/list":
                return new JsonObject { ["tools"] = ServeTools.Descriptors() };
            case "tools/call":
            {
                var args = request["params"] as JsonObject;
                var name = args is not null ? JsonValues.AsString(args["name"]) : null;
                var callArgs = args?["arguments"] as JsonObject;

                if (name is null)
                {
                    throw new InvalidOperationException("tools/call needs a string name.");
                }

                return ServeTools.Call(state, name, callArgs)
                    ?? throw new InvalidOperationException($"Unknown tool '{name}'.");
            }

            default:
                return null;
        }
    }

    /// <summary>One JSON-RPC error object: the version, the id, and the code with its message.</summary>
    /// <param name="id">The request id, or null when the message carried none.</param>
    /// <param name="code">The JSON-RPC error code.</param>
    /// <param name="message">The one-line error message.</param>
    /// <returns>The error object.</returns>
    static JsonObject Error(JsonNode? id, int code, string message) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
    };

    /// <summary>Writes one JSON-RPC message on one line, as MCP over standard input and output requires.</summary>
    /// <param name="message">The message to write.</param>
    /// <returns>The message as compact JSON with no line breaks.</returns>
    private static string Compact(JsonNode message) => message.ToJsonString();
}
