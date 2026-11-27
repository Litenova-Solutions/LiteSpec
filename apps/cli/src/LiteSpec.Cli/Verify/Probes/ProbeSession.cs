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
    /// <param name="cookieUri">The address the session keeps its cookies under.</param>
    private ProbeSession(string? person, HttpClient client, CookieContainer cookies, Uri cookieUri)
    {
        Person = person;
        Client = client;
        Cookies = cookies;
        CookieUri = cookieUri;
    }

    /// <summary>Gets the cast person id, or null for anonymous.</summary>
    public string? Person { get; }

    /// <summary>Gets the HTTP client sending with the session cookies.</summary>
    public HttpClient Client { get; }

    /// <summary>Gets the cookie container holding the session.</summary>
    public CookieContainer Cookies { get; }

    /// <summary>
    ///     Gets the address the session keeps its cookies under: the base address, with the https scheme for a
    ///     loopback host, because a browser treats localhost as a secure context and sends it a secure cookie.
    /// </summary>
    public Uri CookieUri { get; }

    /// <summary>Signs one cast person in through the verify sign-in requests.</summary>
    /// <param name="person">The cast person id, or null for anonymous.</param>
    /// <param name="baseUrl">The API host address the requests run against.</param>
    /// <param name="signIn">The sign-in requests, run in order.</param>
    /// <param name="values">The fixed values a path or body names as {name}.</param>
    /// <param name="headers">The headers sent on every request of the session.</param>
    /// <returns>The signed-in session.</returns>
    /// <exception cref="ProbeUnreachableException">The host or the sign-in could not be reached.</exception>
    public static ProbeSession SignIn(
        string? person,
        string baseUrl,
        IReadOnlyList<VerifyRequest> signIn,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyDictionary<string, string> headers)
    {
        var cookies = new CookieContainer();

        // The session never follows a redirect: a sign-in that redirects has set its cookie, and a probe sees the
        // answer the host gave, not the page a redirect leads to.
        var handler = new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false };
        var baseUri = new Uri(EnsureSlash(baseUrl));
        var client = new HttpClient(handler) { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(10) };

        foreach (var (name, value) in headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        }

        var session = new ProbeSession(person, client, cookies, CookieUriOf(baseUri));

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
                    response = session.Exchange(message);
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
            response = Exchange(message);
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

    /// <summary>Sends one request with the session cookies and keeps the cookies the answer sets.</summary>
    /// <param name="message">The request.</param>
    /// <returns>The answer.</returns>
    private HttpResponseMessage Exchange(HttpRequestMessage message)
    {
        var header = Cookies.GetCookieHeader(CookieUri);

        if (header.Length > 0)
        {
            message.Headers.TryAddWithoutValidation("Cookie", header);
        }

        var response = Client.Send(message, HttpCompletionOption.ResponseHeadersRead);

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                try
                {
                    Cookies.SetCookies(CookieUri, setCookie);
                }
                catch (CookieException)
                {
                    // A cookie the container cannot read is not this session's; the probe answer shows its effect.
                }
            }
        }

        return response;
    }

    /// <summary>The address the session keeps its cookies under, with the https scheme for a loopback host.</summary>
    /// <param name="baseUri">The base address of the host.</param>
    /// <returns>The cookie address.</returns>
    private static Uri CookieUriOf(Uri baseUri) =>
        baseUri.IsLoopback && baseUri.Scheme == Uri.UriSchemeHttp
            ? new UriBuilder(baseUri) { Scheme = Uri.UriSchemeHttps, Port = baseUri.Port }.Uri
            : baseUri;

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
