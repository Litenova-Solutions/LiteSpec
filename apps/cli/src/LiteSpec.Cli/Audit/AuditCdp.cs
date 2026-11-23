using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Audit;

/// <summary>The Chrome DevTools Protocol client: commands and events over one browser WebSocket.</summary>
/// <follows>litespec/frontend/ui#pass-every-route-suite-check</follows>
sealed class AuditCdp : IDisposable
{
    /// <summary>The receive buffer: eight kilobytes per read.</summary>
    const int BufferSize = 8192;

    /// <summary>The open WebSocket.</summary>
    readonly ClientWebSocket socket = new();

    /// <summary>The pending commands by id.</summary>
    readonly ConcurrentDictionary<int, TaskCompletionSource<JsonObject>> pending = new();

    /// <summary>Guards sends, so only one command writes at a time.</summary>
    readonly SemaphoreSlim sendGate = new(1, 1);

    /// <summary>Stops the receive loop.</summary>
    readonly CancellationTokenSource loopDone = new();

    /// <summary>Guards disposal.</summary>
    readonly object gate = new();

    /// <summary>The next command id.</summary>
    int nextId;

    /// <summary>The receive loop, started on connect.</summary>
    Task? receiveTask;

    /// <summary>Whether the client was disposed.</summary>
    bool disposed;

    /// <summary>Initializes a new instance of the <see cref="AuditCdp"/> class.</summary>
    AuditCdp()
    {
    }

    /// <summary>Occurs when the browser sends an event.</summary>
    public event Action<CdpEvent>? Received;

    /// <summary>Closes the debugging connection and stops the receive loop.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
        }

        loopDone.Cancel();

        try
        {
            socket.Abort();
        }
        catch (Exception exception) when (exception is WebSocketException or InvalidOperationException or ObjectDisposedException)
        {
            // The socket is already gone; disposal still frees the rest.
        }

        socket.Dispose();
        loopDone.Dispose();
        sendGate.Dispose();

        foreach (var entry in pending)
        {
            entry.Value.TrySetException(new InvalidOperationException("The browser closed the debugging connection."));
        }

        pending.Clear();

        if (receiveTask is not null)
        {
            try
            {
                receiveTask.GetAwaiter().GetResult();
            }
            catch (Exception exception) when (exception is OperationCanceledException or WebSocketException or IOException or InvalidOperationException or ObjectDisposedException or ArgumentException)
            {
                // The loop ends with the socket; its own failure needs no report.
            }
        }
    }

    /// <summary>Connects to the browser debugging address and starts the receive loop.</summary>
    /// <param name="webSocketUrl">The browser debugging WebSocket address.</param>
    /// <param name="token">The token that stops the connect.</param>
    /// <returns>The connected client.</returns>
    internal static async Task<AuditCdp> ConnectAsync(string webSocketUrl, CancellationToken token)
    {
        Uri endpoint;

        try
        {
            endpoint = new Uri(webSocketUrl, UriKind.Absolute);
        }
        catch (Exception exception) when (exception is UriFormatException or ArgumentException)
        {
            throw new InvalidOperationException("The browser answered no debugging address.", exception);
        }

        var client = new AuditCdp();

        try
        {
            using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            connectTimeout.CancelAfter(TimeSpan.FromSeconds(15));
            await client.socket.ConnectAsync(endpoint, connectTimeout.Token);
        }
        catch (OperationCanceledException exception) when (!token.IsCancellationRequested)
        {
            client.Dispose();
            throw new InvalidOperationException("The browser did not accept the debugging connection in time.", exception);
        }
        catch (Exception exception) when (exception is WebSocketException or InvalidOperationException or ArgumentException)
        {
            client.Dispose();
            throw new InvalidOperationException($"Cannot reach the browser debugging address: {FirstLine(exception.Message)}", exception);
        }

        client.receiveTask = Task.Run(() => client.ReceiveLoopAsync(client.loopDone.Token), CancellationToken.None);
        return client;
    }

    /// <summary>Sends one command and reads its result back.</summary>
    /// <param name="method">The protocol method, such as Page.navigate.</param>
    /// <param name="parameters">The method parameters, or null for none.</param>
    /// <param name="sessionId">The target session, or null for a browser command.</param>
    /// <param name="timeout">How long the browser may take to answer.</param>
    /// <param name="token">The token that stops the send.</param>
    /// <returns>The result object, or an empty object when the method returns none.</returns>
    internal async Task<JsonObject> SendAsync(string method, JsonObject? parameters, string? sessionId, TimeSpan timeout, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var id = Interlocked.Increment(ref nextId);
        var message = new JsonObject
        {
            ["id"] = id,
            ["method"] = method,
        };

        if (parameters is not null)
        {
            message["params"] = parameters;
        }

        if (sessionId is not null)
        {
            message["sessionId"] = sessionId;
        }

        var bytes = Encoding.UTF8.GetBytes(message.ToJsonString());
        var answer = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = answer;

        try
        {
            await sendGate.WaitAsync(token);

            try
            {
                await socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, endOfMessage: true, token);
            }
            finally
            {
                sendGate.Release();
            }

            using var answerTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            answerTimeout.CancelAfter(timeout);
            JsonObject response;

            try
            {
                response = await answer.Task.WaitAsync(answerTimeout.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new TimeoutException($"The browser did not answer '{method}' in time.");
            }

            if (response["error"] is JsonObject error)
            {
                var detail = error["message"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : "the browser reported an error";
                throw new InvalidOperationException(detail);
            }

            return response["result"] as JsonObject ?? new JsonObject();
        }
        finally
        {
            pending.TryRemove(id, out _);
        }
    }

    /// <summary>Reads every message: answers complete their command, events raise <see cref="Received"/>.</summary>
    /// <param name="token">The token that stops the loop.</param>
    /// <returns>A task that completes when the loop stops.</returns>
    async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[BufferSize];
        using var message = new MemoryStream();

        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            message.SetLength(0);
            ValueWebSocketReceiveResult received;

            do
            {
                try
                {
                    received = await socket.ReceiveAsync(buffer.AsMemory(), token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception) when (exception is WebSocketException or InvalidOperationException or ObjectDisposedException)
                {
                    FailPending("The browser closed the debugging connection.");
                    return;
                }

                if (received.MessageType == WebSocketMessageType.Close)
                {
                    FailPending("The browser closed the debugging connection.");
                    return;
                }

                message.Write(buffer, 0, received.Count);
            }
            while (!received.EndOfMessage);

            JsonNode? node;

            try
            {
                node = JsonNode.Parse(Encoding.UTF8.GetString(message.ToArray()));
            }
            catch (JsonException)
            {
                continue;
            }

            if (node is not JsonObject envelope)
            {
                continue;
            }

            if (envelope["id"] is JsonValue idValue && idValue.TryGetValue<int>(out var id)
                && pending.TryRemove(id, out var answer))
            {
                answer.TrySetResult(envelope);
            }
            else if (envelope["method"] is JsonValue methodValue && methodValue.TryGetValue<string>(out var method))
            {
                var session = envelope["sessionId"] is JsonValue sessionValue && sessionValue.TryGetValue<string>(out var sessionId)
                    ? sessionId
                    : null;
                Received?.Invoke(new CdpEvent(method, session, envelope["params"] as JsonObject));
            }
        }

        FailPending("The browser closed the debugging connection.");
    }

    /// <summary>Fails every pending command the closed connection leaves behind.</summary>
    /// <param name="message">Why the commands fail.</param>
    void FailPending(string message)
    {
        foreach (var entry in pending)
        {
            entry.Value.TrySetException(new InvalidOperationException(message));
        }

        pending.Clear();
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

    /// <summary>One browser event: the method, the target session, and the event parameters.</summary>
    /// <param name="Method">The protocol method, such as Page.loadEventFired.</param>
    /// <param name="SessionId">The target session, or null for a browser event.</param>
    /// <param name="Params">The event parameters, or null for none.</param>
    internal sealed record CdpEvent(string Method, string? SessionId, JsonObject? Params);
}
