using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LiteSpec.Cli.Tests.Verify.Probes;

/// <summary>A local HTTP server for the probe tests: answers each request from a handler and records what it saw.</summary>
sealed class TestProbeServer : IDisposable
{
    readonly HttpListener listener;
    readonly Task worker;
    readonly Func<HttpListenerRequest, string?, (int Status, string Body)> handler;

    /// <summary>Initializes a new instance of the <see cref="TestProbeServer"/> class.</summary>
    /// <param name="port">The local port to listen on.</param>
    /// <param name="handler">The handler that answers one request with a status and a body.</param>
    TestProbeServer(int port, Func<HttpListenerRequest, string?, (int Status, string Body)> handler)
    {
        BaseUrl = $"http://127.0.0.1:{port}";
        this.handler = handler;
        listener = new HttpListener();
        listener.Prefixes.Add(BaseUrl + "/");
        listener.Start();
        worker = Task.Run(Loop);
    }

    /// <summary>Gets the base address the probes run against.</summary>
    public string BaseUrl { get; }

    /// <summary>Gets the request paths the server answered, in order.</summary>
    public ConcurrentQueue<string> Paths { get; } = new();

    /// <summary>Gets the request bodies the server answered, in order.</summary>
    public ConcurrentQueue<string> Bodies { get; } = new();

    /// <summary>Gets the headers of every request, in arrival order.</summary>
    public ConcurrentQueue<System.Collections.Specialized.NameValueCollection> Headers { get; } = new();

    /// <summary>Starts a server that answers each request from the handler.</summary>
    /// <param name="handler">The handler that answers one request with a status and a body.</param>
    /// <returns>The running server.</returns>
    public static TestProbeServer Start(Func<HttpListenerRequest, string?, (int Status, string Body)> handler) =>
        new(FreePort(), handler);

    /// <summary>Gets or sets the Set-Cookie header every sign-in answer under /auth/ carries, or null for none.</summary>
    public string? SignInCookie { get; set; }

    /// <summary>Stops the server and waits for its loop to end.</summary>
    public void Dispose()
    {
        listener.Stop();
        listener.Close();
        worker.Wait(TimeSpan.FromSeconds(10));
    }

    /// <summary>Answers requests until the listener stops.</summary>
    void Loop()
    {
        while (listener.IsListening)
        {
            HttpListenerContext context;

            try
            {
                context = listener.GetContext();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            Handle(context);
        }
    }

    /// <summary>Answers one request from the handler and records its path and body.</summary>
    /// <param name="context">The request and its response.</param>
    void Handle(HttpListenerContext context)
    {
        string? body = null;

        if (context.Request.HasEntityBody)
        {
            using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            body = reader.ReadToEnd();
        }

        Paths.Enqueue(context.Request.Url!.AbsolutePath);
        Bodies.Enqueue(body ?? string.Empty);
        Headers.Enqueue(new System.Collections.Specialized.NameValueCollection(context.Request.Headers));

        var answer = handler(context.Request, body);
        var bytes = Encoding.UTF8.GetBytes(answer.Body);

        try
        {
            context.Response.StatusCode = answer.Status;

            if (SignInCookie is not null && context.Request.Url!.AbsolutePath.StartsWith("/auth/", StringComparison.Ordinal))
            {
                context.Response.AppendHeader("Set-Cookie", SignInCookie);
            }

            // A redirect answer carries its target in the body, which becomes the Location header.
            if (answer.Status is >= 300 and < 400)
            {
                context.Response.RedirectLocation = answer.Body;
            }

            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch (HttpListenerException)
        {
        }
        catch (IOException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch (HttpListenerException)
            {
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    /// <summary>Finds a free local port.</summary>
    /// <returns>The free port number.</returns>
    static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }
}
