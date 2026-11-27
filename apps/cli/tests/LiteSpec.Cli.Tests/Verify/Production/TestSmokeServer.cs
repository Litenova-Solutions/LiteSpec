using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LiteSpec.Cli.Tests.Verify.Production;

/// <summary>A local HTTP server for the production smoke tests: answers each request from a handler and records the forwarded scheme it saw.</summary>
sealed class TestSmokeServer : IDisposable
{
    readonly HttpListener listener;
    readonly Task worker;
    readonly Func<HttpListenerRequest, (int Status, string Body)> handler;

    /// <summary>Initializes a new instance of the <see cref="TestSmokeServer"/> class.</summary>
    /// <param name="port">The local port to listen on.</param>
    /// <param name="handler">The handler that answers one request with a status and a body.</param>
    TestSmokeServer(int port, Func<HttpListenerRequest, (int Status, string Body)> handler)
    {
        BaseUrl = $"http://127.0.0.1:{port}";
        this.handler = handler;
        listener = new HttpListener();
        listener.Prefixes.Add(BaseUrl + "/");
        listener.Start();
        worker = Task.Run(Loop);
    }

    /// <summary>Gets the base address the smoke probes.</summary>
    public string BaseUrl { get; }

    /// <summary>Gets the forwarded scheme each path was requested with, by path.</summary>
    public ConcurrentDictionary<string, string?> ForwardedProto { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the number of requests the server answered.</summary>
    public int Hits => hits;
    int hits;

    /// <summary>Starts a server that answers each request from the handler.</summary>
    /// <param name="handler">The handler that answers one request with a status and a body.</param>
    /// <returns>The running server.</returns>
    public static TestSmokeServer Start(Func<HttpListenerRequest, (int Status, string Body)> handler) =>
        new(FreePort(), handler);

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

    /// <summary>Answers one request from the handler and records its forwarded scheme.</summary>
    /// <param name="context">The request and its response.</param>
    void Handle(HttpListenerContext context)
    {
        var answer = handler(context.Request);
        ForwardedProto[context.Request.Url!.AbsolutePath] = context.Request.Headers["X-Forwarded-Proto"];
        Interlocked.Increment(ref hits);
        var bytes = Encoding.UTF8.GetBytes(answer.Body);
        context.Response.StatusCode = answer.Status;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;

        try
        {
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch (HttpListenerException)
        {
            // The smoke stopped waiting; nothing is owed back.
        }
        catch (IOException)
        {
            // The smoke stopped waiting; nothing is owed back.
        }
        catch (InvalidOperationException)
        {
            // The server stopped while the answer was still written; nothing is owed back.
        }
        finally
        {
            // The smoke may stop waiting before the answer is written; closing then throws.
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
