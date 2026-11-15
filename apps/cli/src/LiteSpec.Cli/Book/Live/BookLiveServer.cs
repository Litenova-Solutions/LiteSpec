using System.Net;
using System.Text;

namespace LiteSpec.Cli.Book.Live;

/// <summary>
///     The live book's HTTP server: the built-in <see cref="HttpListener" /> on localhost. It answers
///     only the names the current build holds, exactly as named, with no directory listing and no
///     path read from disk, and it streams the change notifications to the open page as server-sent
///     events. It refuses any host name other than the loopback names, so a page on another site
///     cannot read the book through a rebound name.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookLiveServer
{
    /// <summary>The host names a request may carry: the loopback names only.</summary>
    static readonly HashSet<string> LocalHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "localhost", "127.0.0.1", "[::1]" };

    /// <summary>The path of the change notification stream, relative to the book.</summary>
    const string EventsPath = "events";

    /// <summary>Serves the live book on localhost until the token is cancelled, rebuilding it in the background.</summary>
    /// <param name="port">The local port to listen on.</param>
    /// <param name="live">The live book the server answers from.</param>
    /// <param name="output">The console the listening line is written to.</param>
    /// <param name="token">The token that stops the server and the rebuild loop.</param>
    /// <returns>A task that completes when the token is cancelled.</returns>
    /// <exception cref="HttpListenerException">The port cannot be listened on, for example because another process holds it.</exception>
    public static async Task ServeAsync(int port, BookLive live, TextWriter output, CancellationToken token)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        using var stop = token.Register(() => listener.Stop());
        output.WriteLine($"Serving the book at http://localhost:{port}/. Press Ctrl+C to stop.");

        var rebuilds = live.RunAsync(token);
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                throw;
            }

            _ = HandleAsync(context, live, token);
        }

        await rebuilds;
    }

    /// <summary>Answers one request: the event stream, a book file by its exact name, or a refusal.</summary>
    /// <param name="context">The request and its response.</param>
    /// <param name="live">The live book the answer comes from.</param>
    /// <param name="token">The token that stops the server.</param>
    /// <returns>A task that completes when the response is closed.</returns>
    static async Task HandleAsync(HttpListenerContext context, BookLive live, CancellationToken token)
    {
        var response = context.Response;
        try
        {
            var request = context.Request;
            if (!LocalHosts.Contains(request.Url?.Host ?? ""))
            {
                await WriteTextAsync(response, 403, "Forbidden.", token);
                return;
            }

            if (request.HttpMethod is not ("GET" or "HEAD"))
            {
                response.Headers["Allow"] = "GET, HEAD";
                await WriteTextAsync(response, 405, "Method not allowed.", token);
                return;
            }

            var name = Uri.UnescapeDataString(request.Url?.AbsolutePath ?? "/").TrimStart('/');
            if (name == EventsPath)
            {
                await StreamEventsAsync(response, live, token);
                return;
            }

            if (name.Length == 0)
            {
                name = "index.html";
            }

            if (!live.Current.Site.TryGet(name, out var file))
            {
                await WriteTextAsync(response, 404, "Not found.", token);
                return;
            }

            response.StatusCode = 200;
            response.ContentType = file.ContentType;
            response.Headers["Cache-Control"] = "no-store";
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.ContentLength64 = file.Bytes.Length;
            if (request.HttpMethod == "GET")
            {
                await response.OutputStream.WriteAsync(file.Bytes.AsMemory(), token);
            }
        }
        catch (Exception exception) when (exception is HttpListenerException or IOException or ObjectDisposedException or OperationCanceledException)
        {
            // The reader went away or the server is stopping; nothing is owed back.
        }
        finally
        {
            response.Close();
        }
    }

    /// <summary>Streams the book's change notifications to one page until the page leaves or the server stops.</summary>
    /// <param name="response">The response the events are written to.</param>
    /// <param name="live">The live book whose notifications are streamed.</param>
    /// <param name="token">The token that stops the stream.</param>
    /// <returns>A task that completes when the stream ends.</returns>
    static async Task StreamEventsAsync(HttpListenerResponse response, BookLive live, CancellationToken token)
    {
        response.StatusCode = 200;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-store";
        response.SendChunked = true;

        var channel = live.Subscribe();
        try
        {
            await WriteChunkAsync(response, ": connected\n\n", token);
            await foreach (var evt in channel.Reader.ReadAllAsync(token))
            {
                await WriteChunkAsync(response, $"event: {evt.Name}\ndata: {evt.Data}\n\n", token);
            }
        }
        finally
        {
            live.Unsubscribe(channel);
        }
    }

    /// <summary>Writes one event chunk and flushes it, so the page hears it at once.</summary>
    /// <param name="response">The response the chunk is written to.</param>
    /// <param name="text">The event text.</param>
    /// <param name="token">The token that stops the write.</param>
    /// <returns>A task that completes when the chunk is flushed.</returns>
    static async Task WriteChunkAsync(HttpListenerResponse response, string text, CancellationToken token)
    {
        await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(text).AsMemory(), token);
        await response.OutputStream.FlushAsync(token);
    }

    /// <summary>Writes a short plain-text answer with its status.</summary>
    /// <param name="response">The response the answer is written to.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <param name="text">The answer text.</param>
    /// <param name="token">The token that stops the write.</param>
    /// <returns>A task that completes when the answer is written.</returns>
    static async Task WriteTextAsync(HttpListenerResponse response, int status, string text, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "\n");
        response.StatusCode = status;
        response.ContentType = "text/plain; charset=utf-8";
        response.Headers["Cache-Control"] = "no-store";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes.AsMemory(), token);
    }
}
