using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     One signed-in cast person: the cookie container the sign-in requests fill
///     and the HTTP client that sends the probe with those cookies.
/// </summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal sealed class ProbeSession : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="ProbeSession"/> class.</summary>
    /// <param name="person">The cast person id, or null for anonymous.</param>
    /// <param name="client">The HTTP client sending with the session cookies.</param>
    /// <param name="cookies">The cookie container holding the session.</param>
    private ProbeSession(string? person, HttpClient client, CookieContainer cookies)
    {
        Person = person;
        Client = client;
        Cookies = cookies;
    }

    /// <summary>Gets the cast person id, or null for anonymous.</summary>
    public string? Person { get; }

    /// <summary>Gets the HTTP client sending with the session cookies.</summary>
    public HttpClient Client { get; }

    /// <summary>Gets the cookie container holding the session.</summary>
    public CookieContainer Cookies { get; }

    /// <summary>Signs one cast person in through the verify sign-in requests.</summary>
    /// <param name="person">The cast person id, or null for anonymous.</param>
    /// <param name="baseUrl">The API host address the requests run against.</param>
    /// <param name="signIn">The sign-in requests, run in order.</param>
    /// <param name="values">The fixed values a path or body names as {name}.</param>
    /// <returns>The signed-in session.</returns>
    /// <exception cref="ProbeUnreachableException">The host or the sign-in could not be reached.</exception>
    public static ProbeSession SignIn(
        string? person,
        string baseUrl,
        IReadOnlyList<VerifyRequest> signIn,
        IReadOnlyDictionary<string, string> values)
    {
        var cookies = new CookieContainer();
        var handler = new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = true };
        var client = new HttpClient(handler) { BaseAddress = new Uri(EnsureSlash(baseUrl)), Timeout = TimeSpan.FromSeconds(10) };
        var session = new ProbeSession(person, client, cookies);

        try
        {
            if (person is null || person.Length == 0)
            {
                return session;
            }

            var shortName = ShortNameOf(person);

            foreach (var request in signIn)
            {
                var path = ResolvedProbeRequest.ResolvePath(request.Path, values, shortName);
                var bodyJson = ResolvedProbeRequest.ResolveBody(request.Body, values, shortName);
                using var message = new HttpRequestMessage(new HttpMethod(request.Method), path);

                if (bodyJson is not null)
                {
                    message.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                }

                HttpResponseMessage response;

                try
                {
                    response = client.Send(message, HttpCompletionOption.ResponseHeadersRead);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
                {
                    throw new ProbeUnreachableException($"Could not sign '{person}' in: {exception.Message}.", exception);
                }

                using (response)
                {
                    if ((int)response.StatusCode is < 200 or >= 400)
                    {
                        throw new ProbeUnreachableException($"Could not sign '{person}' in: the host answered {(int)response.StatusCode}.");
                    }
                }
            }

            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    /// <summary>Sends one resolved probe request through the session.</summary>
    /// <param name="request">The resolved method, path and body.</param>
    /// <returns>The status code and the response body text.</returns>
    /// <exception cref="ProbeUnreachableException">The host could not be reached.</exception>
    public (int Status, string Body) Send(ResolvedProbeRequest request)
    {
        using var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Path);

        if (request.BodyJson is not null)
        {
            message.Content = new StringContent(request.BodyJson, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;

        try
        {
            response = Client.Send(message, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            throw new ProbeUnreachableException($"Could not reach the host at '{Client.BaseAddress}{request.Path.TrimStart('/')}': {exception.Message}.", exception);
        }

        using (response)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return ((int)response.StatusCode, body);
        }
    }

    /// <summary>Releases the HTTP client.</summary>
    public void Dispose() => Client.Dispose();

    /// <summary>The short person name without the person/ prefix.</summary>
    /// <param name="person">The cast person id, such as person/ava.</param>
    /// <returns>The name without the prefix, or the id when it carries none.</returns>
    private static string ShortNameOf(string person) =>
        person.StartsWith("person/", StringComparison.Ordinal) ? person["person/".Length..] : person;

    /// <summary>The base address with a trailing slash, so relative paths resolve under it.</summary>
    /// <param name="baseUrl">The configured base address.</param>
    /// <returns>The address with a trailing slash.</returns>
    private static string EnsureSlash(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
