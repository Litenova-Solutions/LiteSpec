using System.Globalization;
using System.Net;
using System.Net.Sockets;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Explore;

/// <summary>The parsed options of <c>litespec api explore</c>.</summary>
/// <param name="BaseUrl">The running API the walk calls, without a trailing slash.</param>
/// <param name="OpenApi">The OpenAPI document: a file, an absolute URL, or a path under the base URL.</param>
/// <param name="Steps">How many calls the walk makes.</param>
/// <param name="Seed">The seed that replays the same calls.</param>
/// <param name="Headers">The request headers from --header, in order.</param>
/// <param name="CookieFile">The cookie file, or null to send no cookies.</param>
/// <param name="Denies">The deny entries from --deny, in order.</param>
/// <param name="Out">The folder receiving the Markdown and JSON reports.</param>
/// <param name="AllowDelete">Whether the walk calls DELETE operations and paths holding delete.</param>
/// <param name="AllowRemote">Whether the walk calls a base URL that is not loopback.</param>
sealed record ExploreOptions(
    string BaseUrl,
    string OpenApi,
    int Steps,
    int Seed,
    IReadOnlyList<(string Name, string Value)> Headers,
    string? CookieFile,
    IReadOnlyList<ExploreDeny> Denies,
    string Out,
    bool AllowDelete,
    bool AllowRemote)
{
    /// <summary>The OpenAPI path the walk reads when --openapi is absent.</summary>
    internal const string DefaultOpenApi = "/openapi/v1.json";

    /// <summary>How many calls the walk makes when --steps is absent.</summary>
    internal const int DefaultSteps = 200;

    /// <summary>The seed the walk uses when --seed is absent.</summary>
    internal const int DefaultSeed = 1;

    /// <summary>The folder the walk writes into when --out is absent, under the working directory.</summary>
    internal const string DefaultOut = ".litespec/explore";

    /// <summary>The value options the repeatable split passes through with their following value.</summary>
    static readonly HashSet<string> ValueOptions = new(StringComparer.Ordinal)
    {
        "--base-url",
        "--openapi",
        "--steps",
        "--seed",
        "--cookie-file",
        "--out",
    };

    /// <summary>Splits the raw arguments into the repeatable options and the rest for the argument parser.</summary>
    /// <param name="args">The raw arguments after the subcommand.</param>
    /// <returns>The header values, the deny pairs and the rest, or the usage error.</returns>
    internal static (List<string> Headers, List<(string Method, string Pattern)> Denies, List<string> Remaining, string? Error) SplitRepeatable(string[] args)
    {
        var headers = new List<string>();
        var denies = new List<(string Method, string Pattern)>();
        var rest = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (string.Equals(arg, "--header", StringComparison.Ordinal))
            {
                if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return (headers, denies, rest, "Option --header needs a value.");
                }

                headers.Add(args[index + 1]);
                index++;
            }
            else if (arg.StartsWith("--header=", StringComparison.Ordinal))
            {
                headers.Add(arg["--header=".Length..]);
            }
            else if (string.Equals(arg, "--deny", StringComparison.Ordinal))
            {
                if (index + 2 >= args.Length
                    || args[index + 1].StartsWith("--", StringComparison.Ordinal)
                    || args[index + 2].StartsWith("--", StringComparison.Ordinal))
                {
                    return (headers, denies, rest, "Option --deny needs a method and a path pattern.");
                }

                denies.Add((args[index + 1], args[index + 2]));
                index += 2;
            }
            else if (arg.StartsWith("--deny=", StringComparison.Ordinal))
            {
                var method = arg["--deny=".Length..];

                if (method.Length == 0
                    || index + 1 >= args.Length
                    || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return (headers, denies, rest, "Option --deny needs a method and a path pattern.");
                }

                denies.Add((method, args[index + 1]));
                index++;
            }
            else if (ValueOptions.Contains(arg))
            {
                rest.Add(arg);

                if (index + 1 < args.Length)
                {
                    rest.Add(args[index + 1]);
                    index++;
                }
            }
            else
            {
                rest.Add(arg);
            }
        }

        return (headers, denies, rest, null);
    }

    /// <summary>Parses the explore options, in usage order.</summary>
    /// <param name="parsed">The parsed command arguments without the repeatable options.</param>
    /// <param name="headersRaw">The --header values in order.</param>
    /// <param name="deniesRaw">The --deny method and pattern pairs in order.</param>
    /// <returns>The options, or the usage error when an option is missing or invalid.</returns>
    internal static (ExploreOptions? Options, string? Error) Parse(CliArgs parsed, List<string> headersRaw, List<(string Method, string Pattern)> deniesRaw)
    {
        var baseUrl = parsed.Value("--base-url");

        if (string.IsNullOrEmpty(baseUrl))
        {
            return (null, "Option --base-url needs a value.");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return (null, $"Invalid base URL '{baseUrl}': use an absolute http or https URL.");
        }

        var openapi = parsed.Value("--openapi") ?? DefaultOpenApi;

        if (openapi.Length == 0)
        {
            return (null, "Option --openapi needs a value.");
        }

        var (steps, stepsError) = ParseSteps(parsed.Value("--steps"));

        if (stepsError is not null)
        {
            return (null, stepsError);
        }

        var (seed, seedError) = ParseSeed(parsed.Value("--seed"));

        if (seedError is not null)
        {
            return (null, seedError);
        }

        var (headers, headersError) = ParseHeaders(headersRaw);

        if (headers is null)
        {
            return (null, headersError);
        }

        var cookieFile = parsed.Value("--cookie-file");

        if (cookieFile is not null && cookieFile.Length == 0)
        {
            return (null, "Option --cookie-file needs a value.");
        }

        var (denies, deniesError) = ParseDenies(deniesRaw);

        if (denies is null)
        {
            return (null, deniesError);
        }

        var allowRemote = parsed.Has("--allow-remote");

        if (!IsLoopbackHost(uri.Host) && !allowRemote)
        {
            return (null, $"Base URL '{baseUrl}' is not loopback: the walk sends state-changing calls, so it runs against a local build only. Pass --allow-remote to walk a remote address.");
        }

        return (new ExploreOptions(
            baseUrl.TrimEnd('/'),
            openapi,
            steps,
            seed,
            headers,
            cookieFile,
            denies,
            parsed.Value("--out") ?? DefaultOut,
            parsed.Has("--allow-delete"),
            allowRemote), null);
    }

    /// <summary>Whether the host is loopback: localhost, 127.x.x.x or ::1.</summary>
    /// <param name="host">The base URL host.</param>
    /// <returns>Whether the walk may call it without --allow-remote.</returns>
    internal static bool IsLoopbackHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "localhost.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        return address.AddressFamily == AddressFamily.InterNetwork
            && address.GetAddressBytes()[0] == 127;
    }

    /// <summary>Parses the --steps value into a call count.</summary>
    /// <param name="raw">The option value, or null when it was not passed.</param>
    /// <returns>The call count, or the usage error when it is not a positive number.</returns>
    static (int Steps, string? Error) ParseSteps(string? raw)
    {
        if (raw is null)
        {
            return (DefaultSteps, null);
        }

        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var steps) || steps <= 0)
        {
            return (0, $"Invalid steps '{raw}': use a positive number.");
        }

        return (steps, null);
    }

    /// <summary>Parses the --seed value into a seed.</summary>
    /// <param name="raw">The option value, or null when it was not passed.</param>
    /// <returns>The seed, or the usage error when it is not a whole number.</returns>
    static (int Seed, string? Error) ParseSeed(string? raw)
    {
        if (raw is null)
        {
            return (DefaultSeed, null);
        }

        if (!int.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var seed))
        {
            return (0, $"Invalid seed '{raw}': use a whole number.");
        }

        return (seed, null);
    }

    /// <summary>Parses the --header values into names and values.</summary>
    /// <param name="raw">The --header values in order.</param>
    /// <returns>The headers in order, or the usage error when one is not Name: value.</returns>
    static (IReadOnlyList<(string Name, string Value)>? Headers, string? Error) ParseHeaders(List<string> raw)
    {
        var headers = new List<(string Name, string Value)>();

        foreach (var entry in raw)
        {
            var colon = entry.IndexOf(':');

            if (colon < 0)
            {
                return (null, $"Invalid header '{entry}': use Name: value.");
            }

            var name = entry[..colon].Trim();
            var value = entry[(colon + 1)..].Trim();

            if (name.Length == 0)
            {
                return (null, $"Invalid header '{entry}': use Name: value.");
            }

            if (string.Equals(name, "Cookie", StringComparison.OrdinalIgnoreCase))
            {
                return (null, "Header 'Cookie' comes from --cookie-file; drop it from --header.");
            }

            headers.Add((name, value));
        }

        return (headers, null);
    }

    /// <summary>Parses the --deny pairs into deny entries.</summary>
    /// <param name="raw">The --deny method and pattern pairs in order.</param>
    /// <returns>The deny entries in order, or the usage error when one is invalid.</returns>
    static (IReadOnlyList<ExploreDeny>? Denies, string? Error) ParseDenies(List<(string Method, string Pattern)> raw)
    {
        var denies = new List<ExploreDeny>();

        foreach (var (methodRaw, patternRaw) in raw)
        {
            var method = methodRaw.Trim().ToUpperInvariant();
            var pattern = patternRaw.Trim();

            if (method.Length == 0 || pattern.Length == 0)
            {
                return (null, "Option --deny needs a method and a path pattern.");
            }

            if (method != "*" && !IsMethodToken(method))
            {
                return (null, $"Invalid deny method '{methodRaw}': use an HTTP method or *.");
            }

            denies.Add(new ExploreDeny(method, pattern));
        }

        return (denies, null);
    }

    /// <summary>Whether the text holds only the letters an HTTP method holds.</summary>
    /// <param name="text">The upper-cased method.</param>
    /// <returns>Whether every character is an ASCII letter.</returns>
    static bool IsMethodToken(string text)
    {
        foreach (var character in text)
        {
            if (character is < 'A' or > 'Z')
            {
                return false;
            }
        }

        return text.Length > 0;
    }
}
