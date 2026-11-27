namespace LiteSpec.Cli.Explore;

/// <summary>The cookie file <c>litespec api explore</c> reads: one name=value cookie per line.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class ExploreCookies
{
    /// <summary>Reads a cookie file into one Cookie header value.</summary>
    /// <param name="file">The absolute cookie file path.</param>
    /// <param name="display">The option value the errors name.</param>
    /// <returns>The header value, or the reason the file cannot be read.</returns>
    internal static (string? Header, string? Error) ReadFile(string file, string display)
    {
        string[] lines;

        try
        {
            lines = File.ReadAllLines(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (null, $"Cannot read cookie file '{display}': {exception.Message}");
        }

        var cookies = new List<string>();
        var number = 0;

        foreach (var line in lines)
        {
            number++;
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var equals = trimmed.IndexOf('=');

            if (equals < 0)
            {
                return (null, $"Cookie file '{display}' line {number} needs name=value.");
            }

            var name = trimmed[..equals].Trim();
            var value = trimmed[(equals + 1)..].Trim();

            if (name.Length == 0 || value.Length == 0)
            {
                return (null, $"Cookie file '{display}' line {number} needs name=value.");
            }

            cookies.Add($"{name}={value}");
        }

        if (cookies.Count == 0)
        {
            return (null, $"Cookie file '{display}' holds no cookies: write one name=value per line.");
        }

        return (string.Join("; ", cookies), null);
    }
}
