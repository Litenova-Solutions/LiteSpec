using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Audit;

/// <summary>The storage state the audit signs pages in with: cookies and local storage origins.</summary>
/// <follows>litespec/frontend/ui#pass-every-route-suite-check</follows>
static class AuditStorage
{
    /// <summary>Reads a storage state file: cookies with local storage origins, in Playwright storage-state JSON.</summary>
    /// <param name="file">The absolute storage state file path.</param>
    /// <returns>The cookies and origins the file states.</returns>
    internal static State Read(string file)
    {
        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Cannot read storage state file '{file}': {exception.Message}", exception);
        }

        JsonNode? root;

        try
        {
            root = JsonNode.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Storage state file '{file}' is not valid JSON: {exception.Message}", exception);
        }

        if (root is not JsonObject state)
        {
            throw new InvalidOperationException($"Storage state file '{file}' holds no storage state: write cookies and origins.");
        }

        return new State(CookiesOf(state), OriginsOf(state));
    }

    /// <summary>The cookies the storage state carries.</summary>
    /// <param name="state">The storage state object.</param>
    /// <returns>The cookies in file order.</returns>
    static List<Cookie> CookiesOf(JsonObject state)
    {
        if (state["cookies"] is not JsonArray entries)
        {
            return [];
        }

        var cookies = new List<Cookie>();

        foreach (var entry in entries)
        {
            if (entry is not JsonObject cookie
                || TextOf(cookie, "name") is not { Length: > 0 } name
                || cookie["value"] is not JsonValue valueNode
                || !valueNode.TryGetValue<string>(out var value))
            {
                continue;
            }

            cookies.Add(new Cookie(
                name,
                value,
                TextOf(cookie, "domain"),
                TextOf(cookie, "path"),
                ExpiresOf(cookie),
                FlagOf(cookie, "httpOnly"),
                FlagOf(cookie, "secure"),
                TextOf(cookie, "sameSite")));
        }

        return cookies;
    }

    /// <summary>The local storage origins the storage state carries.</summary>
    /// <param name="state">The storage state object.</param>
    /// <returns>The origins in file order.</returns>
    static List<Origin> OriginsOf(JsonObject state)
    {
        if (state["origins"] is not JsonArray entries)
        {
            return [];
        }

        var origins = new List<Origin>();

        foreach (var entry in entries)
        {
            if (entry is not JsonObject origin || TextOf(origin, "origin") is not { Length: > 0 } url)
            {
                continue;
            }

            var items = new List<LocalEntry>();

            if (origin["localStorage"] is JsonArray stored)
            {
                foreach (var item in stored)
                {
                    if (item is not JsonObject pair
                        || TextOf(pair, "name") is not { } key
                        || pair["value"] is not JsonValue valueNode
                        || !valueNode.TryGetValue<string>(out var storedValue))
                    {
                        continue;
                    }

                    items.Add(new LocalEntry(key, storedValue));
                }
            }

            origins.Add(new Origin(url, items));
        }

        return origins;
    }

    /// <summary>The text one member carries, or null when it carries none.</summary>
    /// <param name="entry">The object holding the member.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The member text, or null.</returns>
    static string? TextOf(JsonObject entry, string name) =>
        entry[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Whether one member is set: true only when the member is present and true.</summary>
    /// <param name="entry">The object holding the member.</param>
    /// <param name="name">The member name.</param>
    /// <returns>True when the member is present and true; otherwise false.</returns>
    static bool FlagOf(JsonObject entry, string name) =>
        entry[name] is JsonValue value && value.TryGetValue<bool>(out var flag) && flag;

    /// <summary>The expiry one cookie carries in seconds since the epoch, or null when it carries none.</summary>
    /// <param name="cookie">The cookie object.</param>
    /// <returns>The expiry, or null.</returns>
    static double? ExpiresOf(JsonObject cookie) =>
        cookie["expires"] is JsonValue value && value.TryGetValue<double>(out var expires) ? expires : null;

    /// <summary>The storage state: the cookies and the local storage origins.</summary>
    /// <param name="Cookies">The cookies in file order.</param>
    /// <param name="Origins">The origins in file order.</param>
    internal sealed record State(IReadOnlyList<Cookie> Cookies, IReadOnlyList<Origin> Origins);

    /// <summary>One stored cookie: the name, value, and scope the browser sets.</summary>
    /// <param name="Name">The cookie name.</param>
    /// <param name="Value">The cookie value.</param>
    /// <param name="Domain">The cookie domain, or null.</param>
    /// <param name="Path">The cookie path, or null.</param>
    /// <param name="Expires">The expiry in seconds since the epoch, or null.</param>
    /// <param name="HttpOnly">Whether JavaScript cannot read the cookie.</param>
    /// <param name="Secure">Whether the cookie needs HTTPS.</param>
    /// <param name="SameSite">The same-site mode, or null.</param>
    internal sealed record Cookie(
        string Name, string Value, string? Domain, string? Path, double? Expires, bool HttpOnly, bool Secure, string? SameSite);

    /// <summary>One stored origin: the address and its local storage entries.</summary>
    /// <param name="Url">The origin address.</param>
    /// <param name="Entries">The local storage entries.</param>
    internal sealed record Origin(string Url, IReadOnlyList<LocalEntry> Entries);

    /// <summary>One local storage entry: the key and the stored text.</summary>
    /// <param name="Name">The entry key.</param>
    /// <param name="Value">The stored text.</param>
    internal sealed record LocalEntry(string Name, string Value);
}
