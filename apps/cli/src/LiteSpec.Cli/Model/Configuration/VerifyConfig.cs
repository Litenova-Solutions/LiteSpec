using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     The top-level <c>verify</c> block of the consumer declaration: where the API host listens,
///     how a cast person signs in, the fixed values probes name, and the persistence and production
///     settings. Every part is optional; an absent block reads as <see cref="Empty"/>.
/// </summary>
/// <param name="BaseUrl">The API host address, or null when the declaration names none.</param>
/// <param name="SignIn">The requests that sign one cast person in, in order.</param>
/// <param name="Values">The fixed values a probe path or body names as {name}.</param>
/// <param name="Persistence">The persistence harness, or null when the declaration names none.</param>
/// <param name="Production">The production smoke settings, or null when the declaration names none.</param>
sealed record VerifyConfig(
    string? BaseUrl,
    IReadOnlyList<VerifyRequest> SignIn,
    IReadOnlyDictionary<string, string> Values,
    VerifyPersistence? Persistence,
    VerifyProduction? Production)
{
    /// <summary>Gets the settings of a declaration with no <c>verify</c> block.</summary>
    public static VerifyConfig Empty { get; } = new(null, [], new Dictionary<string, string>(StringComparer.Ordinal), null, null);

    /// <summary>Reads the <c>verify</c> block, or returns <see cref="Empty"/> when the block is absent.</summary>
    /// <param name="block">The <c>verify</c> object of the declaration, or null.</param>
    /// <returns>The verify settings.</returns>
    public static VerifyConfig Read(JsonObject? block)
    {
        if (block is null)
        {
            return Empty;
        }

        var signIn = new List<VerifyRequest>();

        if (block["signIn"] is JsonArray requests)
        {
            foreach (var request in requests.OfType<JsonObject>())
            {
                if (Text(request, "method") is { } method && Text(request, "path") is { } path)
                {
                    signIn.Add(new VerifyRequest(method, path, request["body"]?.DeepClone()));
                }
            }
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        if (block["values"] is JsonObject named)
        {
            foreach (var (name, value) in named)
            {
                if (value is JsonValue scalar)
                {
                    values[name] = scalar.ToString();
                }
            }
        }

        var persistence = block["persistence"] is JsonObject harness && Text(harness, "command") is { } command
            ? new VerifyPersistence(command)
            : null;

        VerifyProduction? production = null;

        if (block["production"] is JsonObject smoke
            && Text(smoke, "compose") is { } compose
            && Text(smoke, "envFile") is { } envFile
            && Text(smoke, "baseUrl") is { } smokeUrl
            && Text(smoke, "health") is { } health)
        {
            var paths = smoke["paths"] is JsonArray listed
                ? listed.OfType<JsonValue>().Select(path => path.ToString()).Where(path => path.Length > 0).ToList()
                : [];
            production = new VerifyProduction(compose, envFile, smokeUrl, Text(smoke, "migrate"), health, paths);
        }

        return new VerifyConfig(Text(block, "baseUrl"), signIn, values, persistence, production);
    }

    /// <summary>The non-empty string a property holds, or null.</summary>
    /// <param name="node">The object to read.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string, or null when the property is absent, not a string, or empty.</returns>
    private static string? Text(JsonObject node, string name) =>
        node[name] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > 0 ? text : null;
}
